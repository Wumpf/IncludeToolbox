using Community.VisualStudio.Toolkit;
using IncludeToolbox.IncludeWhatYouUse;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    internal class CancelCallback : IVsThreadedWaitDialogCallback
    {
        public delegate void Cancel();
        public Cancel cancel;
        public CancelCallback(Cancel cancel)
        {
            this.cancel = cancel;
        }

        void IVsThreadedWaitDialogCallback.OnCanceled()
        {
            cancel();
        }
    }


    [Command(PackageIds.IncludeWhatYouUseId)]
    internal sealed class RunIWYU : BaseCommand<RunIWYU>
    {
        bool download_required = false;
        IWYU proc = new();
        CancelCallback cancelCallback;

        public RunIWYU()
        {
            cancelCallback = new(delegate { proc.CancelAsync().FireAndForget(); });
        }

        protected override async Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            var settings = await IWYUOptions.GetLiveInstanceAsync();
            if(settings.Executable == IWYUDownload.GetDefaultExecutablePath())
                download_required = await IWYUDownload.IsNewerVersionAvailableOnlineAsync();
        }

        private async Task<bool> DownloadAsync(IVsThreadedWaitDialogFactory dialogFactory)
        {
            if (!await VS.MessageBox.ShowConfirmAsync($"Can't locate include-what-you-use. Do you want to download it from '{IWYUDownload.DisplayRepositorURL}'?"))
                return false;

            var downloader = new IWYUDownload();

            dialogFactory.CreateInstance(out IVsThreadedWaitDialog2 xdialog);
            IVsThreadedWaitDialog4 dialog = xdialog as IVsThreadedWaitDialog4;

            downloader.OnProgress += (string section, string status, float percentage) =>
                {
                    bool canceled;
                    dialog.UpdateProgress(
                    szUpdatedWaitMessage: section,
                    szProgressText: status,
                    szStatusBarText: $"Downloading include-what-you-use - {section} - {status}",
                    iCurrentStep: (int)(percentage * 100),
                    iTotalSteps: 100,
                    fDisableCancel: true,
                    pfCanceled: out canceled);
                };

            dialog.StartWaitDialogWithCallback(
                szWaitCaption: "Include Toolbox - Downloading include-what-you-use",
                szWaitMessage: "", // comes in later.
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: "Downloading include-what-you-use",
                fIsCancelable: true,
                iDelayToShowDialog: 0,
                fShowProgress: true,
                iTotalSteps: 100,
                iCurrentStep: 0,
                new CancelCallback(() => { downloader.Cancel(); }));

            await downloader.DownloadIWYUAsync();

            if (dialog.EndWaitDialog()) return false;

            var settings = await IWYUOptions.GetLiveInstanceAsync();
            settings.Executable = IWYUDownload.GetDefaultExecutablePath();


            return true;
        }

        async Task SaveAllDocumentsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                VCUtil.GetDTE().Documents.SaveAll();
            }
            catch (Exception e)
            {
                Output.WriteLineAsync($"Failed to get save all documents: {e.Message}").FireAndForget();
            }
        }

        public void MoveHeader(DocumentView view)
        {
            var buf = view.TextBuffer;
            var str = buf.CurrentSnapshot.GetText();
            int begin = str.IndexOf("#include ");
            int end = str.LastIndexOf("#include ");
            end = str.IndexOf('\n', end);


            Regex regex = new($"#include\\s[<\"]([\\w\\\\\\/\\.]+{Path.GetFileNameWithoutExtension(view.FilePath)}.h(?:pp|xx)?)[>\"]");
            var match = regex.Match(str, begin, end - begin);
            if (!match.Success) return;
            var edit = buf.CreateEdit();
            _ = edit.Delete(new(match.Index, match.Length));
            /// TODO: remove, replace with format
            edit.Insert(begin, $"#include <{match.Groups[1].Value}>\n");
            edit.Apply();
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var settings = await IWYUOptions.GetLiveInstanceAsync();
            var dlg = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();

            if ((settings.Executable == "" || !File.Exists(settings.Executable) || download_required)
                && !await DownloadAsync(dlg))
            {
                VS.MessageBox.ShowErrorAsync("IWYU Error", "No executable found, operation cannot be completed").FireAndForget();
                return;
            }


            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            if (doc == null) return;


            if (settings.Dirty) proc.BuildCommandLine(settings);
            if (settings.IgnoreHeader) MoveHeader(doc);

            await SaveAllDocumentsAsync();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            dlg.CreateInstance(out IVsThreadedWaitDialog2 xdialog);
            IVsThreadedWaitDialog4 dialog = xdialog as IVsThreadedWaitDialog4;

            dialog.StartWaitDialogWithCallback("Include Minimizer", "Running include-what-you-use", null, null, "Running include-what-you-use", true, 0, true, 0, 0, cancelCallback);

            var result = await proc.StartAsync(doc.FilePath, settings.AlwaysRebuid);

            if (dialog.EndWaitDialog() || result == false) return;

            await proc.ApplyAsync();
        }
    }
}
