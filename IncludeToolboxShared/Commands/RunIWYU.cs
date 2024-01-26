﻿using Community.VisualStudio.Toolkit;
using IncludeToolbox.IncludeWhatYouUse;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using File = System.IO.File;
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
        IWYU proc = new();
        CancelCallback cancelCallback;


        protected override async Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            cancelCallback = new(delegate { proc.CancelAsync().FireAndForget(); });
            var settings = await IWYUOptions.GetLiveInstanceAsync();
            settings.OnChange += proc.BuildCommandLine;
            proc.BuildCommandLine(settings);
        }


        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var settings = await IWYUOptions.GetLiveInstanceAsync();
            var dlg = (IVsThreadedWaitDialogFactory)await VS.Services.GetThreadedWaitDialogAsync();

            if ((settings.Executable == "" || !File.Exists(settings.Executable) || await settings.DownloadRequiredAsync())
                && !await IWYUDownload.DownloadAsync(dlg, settings))
            {
                VS.MessageBox.ShowErrorAsync("IWYU Error", "No executable found, operation cannot be completed").FireAndForget();
                return;
            }

            await VS.Commands.ExecuteAsync(KnownCommands.File_SaveSelectedItems);

            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            if (doc == null) return;
            if (settings.IgnoreHeader) IWYU.MoveHeader(doc);
            var buf = doc.TextBuffer;
            var str = buf.CurrentSnapshot.GetText();


            var x = Parser.ParseAsync(str);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            dlg.CreateInstance(out IVsThreadedWaitDialog2 xdialog);
            IVsThreadedWaitDialog4 dialog = xdialog as IVsThreadedWaitDialog4;

            dialog.StartWaitDialogWithCallback("Include Toolbox", "Running include-what-you-use", null, null, "Running include-what-you-use", true, 0, true, 0, 0, cancelCallback);

            bool result = false;
            try
            {
                result = await proc.StartAsync(doc.FilePath, settings.AlwaysRebuid);
            }
            catch (Exception ex)
            {
                _ = Output.WriteLineAsync("IWYU Failed with error:" + ex.Message);
            }

            if (dialog.EndWaitDialog() || result == false) return;

            if (settings.Sub == Substitution.Precise)
                await IWYUApply.ApplyPreciseAsync(settings, await x, proc.ProcOutput, VCUtil.Std);
            else
                await IWYUApply.ApplyAsync(settings, proc.ProcOutput);


            if (settings.RemoveENS)
                IWYUApply.ClearNamespaces(doc);
            if (settings.Format)
                await IWYUApply.FormatAsync(doc);
            if (settings.FormatDoc)
                await VS.Commands.ExecuteAsync(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID.FORMATDOCUMENT);
        }
    }
}
