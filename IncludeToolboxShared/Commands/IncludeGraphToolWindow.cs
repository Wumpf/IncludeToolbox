using System;
using System.ComponentModel.Design;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    [Command(PackageIds.IncludeGraphId)]
    internal sealed class IncludeGraphToolWindow : BaseCommand<IncludeGraphToolWindow>
    {
        protected override Task InitializeCompletedAsync()
        {
            return base.InitializeCompletedAsync();
        }

        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Has to be synchronous")]
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Visible = true;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await base.ExecuteAsync(e);
            //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            //// Get the instance number 0 of this tool window. This window is single instance so this instance
            //// is actually the only one.
            //// The last flag is set to true so that if the tool window does not exists it will be created.
            //ToolWindowPane window = Package.FindToolWindow(typeof(GraphWindow.IncludeGraphToolWindow), 0, true);
            //if (window?.Frame == null)
            //{
            //    await Output.Instance.ErrorMsg("Failed to open Include Graph window!");
            //}
            //else
            //{
            //    IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            //    windowFrame.SetProperty((int)__VSFPROPID.VSFPROPID_CmdUIGuid, GraphWindow.IncludeGraphToolWindow.GUIDString);
            //    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
            //}
        }
    }
}
