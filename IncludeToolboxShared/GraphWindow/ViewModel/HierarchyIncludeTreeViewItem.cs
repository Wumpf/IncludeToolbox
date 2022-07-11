using IncludeToolbox.Graph;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.GraphWindow
{
    public class HierarchyIncludeTreeViewItem : IncludeTreeViewItem
    {
        private IncludeGraph.Include include;

        /// <summary>
        /// File that caused this inlude - the parent!
        /// </summary>
        private string includingFileAbsoluteFilename = null;

        public override IReadOnlyList<IncludeTreeViewItem> Children
        {
            get
            {
                if (cachedItems == null)
                    GenerateChildItems();
                return cachedItems;
            }
        }
        protected IReadOnlyList<IncludeTreeViewItem> cachedItems;

        public HierarchyIncludeTreeViewItem(IncludeGraph.Include include, string includingFileAbsoluteFilename)
        {
            Reset(include, includingFileAbsoluteFilename);
        }

        private void GenerateChildItems()
        {
            if (include.IncludedFile?.Includes != null)
            {
                var cachedItemsList = new List<IncludeTreeViewItem>();
                foreach (Graph.IncludeGraph.Include include in include.IncludedFile?.Includes)
                {
                    cachedItemsList.Add(new HierarchyIncludeTreeViewItem(include, this.AbsoluteFilename));
                }
                cachedItems = cachedItemsList;
            }
            else
            {
                cachedItems = emptyList;
            }
        }

        public void Reset(IncludeGraph.Include include, string includingFileAbsoluteFilename)
        {
            this.include = include;
            cachedItems = null;
            Name = include.IncludedFile?.FormattedName ?? "";
            AbsoluteFilename = include.IncludedFile?.AbsoluteFilename;
            this.includingFileAbsoluteFilename = includingFileAbsoluteFilename;

            NotifyAllPropertiesChanged();
        }

        public override async Task NavigateToInclude()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Want to navigate to origin of this include, not target if possible
            if (includingFileAbsoluteFilename != null && Path.IsPathRooted(includingFileAbsoluteFilename))
            {
                var fileWindow = VSUtils.OpenFileAndShowDocument(includingFileAbsoluteFilename);

                // Try to move to carret if possible.
                if (include.IncludeLine != null)
                {
                    var textDocument = fileWindow.Document.Object() as EnvDTE.TextDocument;

                    if (textDocument != null)
                    {
                        var includeLinePoint = textDocument.StartPoint.CreateEditPoint();
                        includeLinePoint.MoveToLineAndOffset(include.IncludeLine.LineNumber+1, 1);
                        includeLinePoint.TryToShow();

                        textDocument.Selection.MoveToPoint(includeLinePoint);
                    }
                }
            }
        }
    }
}
