using IncludeToolbox.IncludeWhatYouUse;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class IncludeWhatYouUse : CommandBase<IncludeWhatYouUse>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.MenuGroup, 0x0103);

        /// <summary>
        /// Whether we already checked for updates.
        /// </summary>
        private bool checkedForUpdatesThisSession = false;

        public IncludeWhatYouUse()
        {
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();
            menuCommand.BeforeQueryStatus += UpdateVisibility;
        }

        private void UpdateVisibility(object sender, EventArgs e)
        {
            // Needs to be part of a VCProject to be applicable.
            var document = VSUtils.GetDTE()?.ActiveDocument;
            menuCommand.Visible = VSUtils.VCUtils.IsVCProject(document?.ProjectItem?.ContainingProject);
        }

        private async Task<bool> DownloadIWYUWithProgressBar(string executablePath, IVsThreadedWaitDialogFactory dialogFactory)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsThreadedWaitDialog2 progressDialog;
            dialogFactory.CreateInstance(out progressDialog);
            if (progressDialog == null)
            {
                Output.Instance.WriteLine("Failed to get create wait dialog.");
                return false;
            }

            progressDialog.StartWaitDialogWithPercentageProgress(
                szWaitCaption: "Include Toolbox - Downloading include-what-you-use",
                szWaitMessage: "", // comes in later.
                szProgressText: null,
                varStatusBmpAnim: null,
                szStatusBarText: "Downloading include-what-you-use",
                fIsCancelable: true,
                iDelayToShowDialog: 0,
                iTotalSteps: 100,
                iCurrentStep: 0);

            var cancellationToken = new System.Threading.CancellationTokenSource();

            try
            {
                await IWYUDownload.DownloadIWYU(executablePath, delegate (string section, string status, float percentage)
                {
                    ThreadHelper.ThrowIfNotOnUIThread();

                    bool canceled;
                    progressDialog.UpdateProgress(
                        szUpdatedWaitMessage: section,
                        szProgressText: status,
                        szStatusBarText: $"Downloading include-what-you-use - {section} - {status}",
                        iCurrentStep: (int)(percentage * 100),
                        iTotalSteps: 100,
                        fDisableCancel: true,
                        pfCanceled: out canceled);
                    if (canceled)
                    {
                        cancellationToken.Cancel();
                    }
                }, cancellationToken.Token);
            }
            catch (Exception e)
            {
                await Output.Instance.ErrorMsg("Failed to download include-what-you-use: {0}", e);
                return false;
            }
            finally
            {
                progressDialog.EndWaitDialog();
            }

            return true;
        }

        private async Task OptionalDownloadOrUpdate(IncludeWhatYouUseOptionsPage settings, IVsThreadedWaitDialogFactory dialogFactory)
        {
            // Check existence, offer to download if it's not there.
            bool downloadedNewIwyu = false;
            if (!File.Exists(settings.ExecutablePath))
            {
                if (await Output.Instance.YesNoMsg($"Can't find include-what-you-use in '{settings.ExecutablePath}'. Do you want to download it from '{IWYUDownload.DisplayRepositorURL}'?") != Output.MessageResult.Yes)
                {
                    return;
                }

                downloadedNewIwyu = await DownloadIWYUWithProgressBar(settings.ExecutablePath, dialogFactory);
                if (!downloadedNewIwyu)
                    return;
            }
            else if (settings.AutomaticCheckForUpdates && !checkedForUpdatesThisSession)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                IVsThreadedWaitDialog2 dialog = null;
                dialogFactory.CreateInstance(out dialog);
                dialog?.StartWaitDialog("Include Toolbox", "Running Include-What-You-Use", null, null, "Checking for Updates for include-what-you-use", 0, false, true);
                bool newVersionAvailable = await IWYUDownload.IsNewerVersionAvailableOnline(settings.ExecutablePath);
                dialog?.EndWaitDialog();

                if (newVersionAvailable)
                {
                    checkedForUpdatesThisSession = true;
                    if (await Output.Instance.YesNoMsg($"There is a new version of include-what-you-use available. Do you want to download it from '{IWYUDownload.DisplayRepositorURL}'?") == Output.MessageResult.Yes)
                    {
                        downloadedNewIwyu = await DownloadIWYUWithProgressBar(settings.ExecutablePath, dialogFactory);
                    }
                }
            }
            if (downloadedNewIwyu)
                settings.AddMappingFiles(IWYUDownload.GetMappingFilesNextToIwyuPath(settings.ExecutablePath));
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        protected override async Task MenuItemCallback(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var settingsIwyu = (IncludeWhatYouUseOptionsPage)Package.GetDialogPage(typeof(IncludeWhatYouUseOptionsPage));
            Output.Instance.Clear();

            var document = VSUtils.GetDTE().ActiveDocument;
            if (document == null)
            {
                Output.Instance.WriteLine("No active document!");
                return;
            }
            var project = document.ProjectItem?.ContainingProject;
            if (project == null)
            {
                Output.Instance.WriteLine("The document {0} is not part of a project.", document.Name);
                return;
            }

            var dialogFactory = ServiceProvider.GetService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
            if (dialogFactory == null)
            {
                Output.Instance.WriteLine("Failed to get IVsThreadedWaitDialogFactory service.");
                return;
            }

            await OptionalDownloadOrUpdate(settingsIwyu, dialogFactory);

            // We should really have it now, but just in case our update or download method screwed up.
            if (!File.Exists(settingsIwyu.ExecutablePath))
            {
                await Output.Instance.ErrorMsg("Unexpected error: Can't find include-what-you-use.exe after download/update.");
                return;
            }
            checkedForUpdatesThisSession = true;

            // Save all documents.
            try
            {
                document.DTE.Documents.SaveAll();
            }
            catch(Exception saveException)
            {
                Output.Instance.WriteLine("Failed to get save all documents: {0}", saveException);
            }

            // Start wait dialog.
            {
                IVsThreadedWaitDialog2 dialog = null;
                dialogFactory.CreateInstance(out dialog);
                dialog?.StartWaitDialog("Include Toolbox", "Running include-what-you-use", null, null, "Running include-what-you-use", 0, false, true);

                string output = await IWYU.RunIncludeWhatYouUse(document.FullName, project, settingsIwyu);
                if (settingsIwyu.ApplyProposal && output != null)
                {
                    var settingsFormatting = (FormatterOptionsPage)Package.GetDialogPage(typeof(FormatterOptionsPage));
                    await IWYU.Apply(output, settingsIwyu.RunIncludeFormatter, settingsFormatting);
                }

                dialog?.EndWaitDialog();
            }
        }
    }
}
