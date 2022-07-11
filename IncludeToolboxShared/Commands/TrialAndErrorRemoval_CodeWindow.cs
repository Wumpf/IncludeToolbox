

using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TrialAndErrorRemoval_CodeWindow : CommandBase<TrialAndErrorRemoval_CodeWindow>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.MenuGroup, 0x0104);

        private TrialAndErrorRemoval impl;

        public TrialAndErrorRemoval_CodeWindow()
        {
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();

            impl = new TrialAndErrorRemoval();
            menuCommand.BeforeQueryStatus += UpdateVisibility;
        }

        private async void UpdateVisibility(object sender, EventArgs e)
        {
            menuCommand.Visible = (await VSUtils.VCUtils.IsCompilableFile(VSUtils.GetDTE().ActiveDocument)).Result;
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
            var document = VSUtils.GetDTE().ActiveDocument;
            if (document != null)
                await impl.PerformTrialAndErrorIncludeRemoval(document, (TrialAndErrorRemovalOptionsPage)Package.GetDialogPage(typeof(TrialAndErrorRemovalOptionsPage)));
        }
    }
}