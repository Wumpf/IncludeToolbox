using System;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class FormatIncludes : CommandBase<FormatIncludes>
    {
        public override CommandID CommandID => new CommandID(CommandSetGuids.MenuGroup, 0x0100);

        public FormatIncludes()
        {
        }

        protected override void SetupMenuCommand()
        {
            base.SetupMenuCommand();
            menuCommand.BeforeQueryStatus += UpdateVisibility;
        }
        
        private void UpdateVisibility(object sender, EventArgs e)
        {
            // Check whether any includes are selected.
            var viewHost = VSUtils.GetCurrentTextViewHost();
            var selectionSpan = GetSelectionSpan(viewHost);
            var lines = Formatter.IncludeLineInfo.ParseIncludes(selectionSpan.GetText(), Formatter.ParseOptions.RemoveEmptyLines);

            menuCommand.Visible = lines.Any(x => x.ContainsActiveInclude);
        }

        /// <summary>
        /// Returns process selection range - whole lines!
        /// </summary>
        SnapshotSpan GetSelectionSpan(IWpfTextViewHost viewHost)
        {
            var sel = viewHost.TextView.Selection.StreamSelectionSpan;
            var start = new SnapshotPoint(viewHost.TextView.TextSnapshot, sel.Start.Position).GetContainingLine().Start;
            var end = new SnapshotPoint(viewHost.TextView.TextSnapshot, sel.End.Position).GetContainingLine().End;

            return new SnapshotSpan(start, end);
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

            var settings = (FormatterOptionsPage)Package.GetDialogPage(typeof(FormatterOptionsPage));

            // Try to find absolute paths
            var document = VSUtils.GetDTE().ActiveDocument;
            var project = document.ProjectItem?.ContainingProject;
            if (project == null)
            {
                Output.Instance.WriteLine("The document '{0}' is not part of a project.", document.Name);
                return;
            }
            var includeDirectories = VSUtils.GetProjectIncludeDirectories(project);

            // Read.
            var viewHost = VSUtils.GetCurrentTextViewHost();
            var selectionSpan = GetSelectionSpan(viewHost);

            // Format
            string formatedText = Formatter.IncludeFormatter.FormatIncludes(selectionSpan.GetText(), document.FullName, includeDirectories, settings);

            // Overwrite.
            using (var edit = viewHost.TextView.TextBuffer.CreateEdit())
            {
                edit.Replace(selectionSpan, formatedText);
                edit.Apply();
            }
        }
    }
}
