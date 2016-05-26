using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.VCProjectEngine;

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
        protected override void MenuItemCallback(object sender, EventArgs e)
        {
            try
            {
                var settings = (FormatterOptionsPage) Package.GetDialogPage(typeof (FormatterOptionsPage));

                // Try to find absolute paths
                var document = VSUtils.GetDTE().ActiveDocument;
                var project = document.ProjectItem?.ContainingProject;
                if (project == null)
                {
                    Output.Instance.WriteLine("The document {0} is not part of a project.", document.Name);
                }
                var includeDirectories = VSUtils.GetProjectIncludeDirectories(project);
                includeDirectories.Insert(0, PathUtil.Normalize(document.Path) + Path.DirectorySeparatorChar);

                // Read.
                var viewHost = VSUtils.GetCurrentTextViewHost();
                var selectionSpan = GetSelectionSpan(viewHost);
                var lines = IncludeFormatter.IncludeLineInfo.ParseIncludes(selectionSpan.GetText(), settings.RemoveEmptyLines, includeDirectories);

                // Format.
                IEnumerable<string> formatingDirs = includeDirectories;
                if (settings.IgnoreFileRelative)
                {
                    formatingDirs = formatingDirs.Skip(1);
                }
                IncludeFormatter.IncludeFormatter.FormatPaths(lines, settings.PathFormat, formatingDirs);
                IncludeFormatter.IncludeFormatter.FormatDelimiters(lines, settings.DelimiterFormatting);
                IncludeFormatter.IncludeFormatter.FormatSlashes(lines, settings.SlashFormatting);

                // Apply changes so far.
                foreach (var line in lines)
                    line.UpdateTextWithIncludeContent();

                // Sorting. Ignores non-include lines.
                IncludeFormatter.IncludeFormatter.SortIncludes(lines, settings, document.Name);

                // Overwrite.
                string replaceText = string.Join(Environment.NewLine, lines.Select(x => x.Text));
                using (var edit = viewHost.TextView.TextBuffer.CreateEdit())
                {
                    edit.Replace(selectionSpan, replaceText);
                    edit.Apply();
                }
            }
            catch (Exception exception)
            {
                Output.Instance.ErrorMsg("Unexpected Error: {0}", exception.ToString());
            }
        }
    }
}
