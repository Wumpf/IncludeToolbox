using Community.VisualStudio.Toolkit;
using System.Collections.Generic;
using Path = System.IO.Path;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    public class HierarchyIncludeTree
    {
        protected IReadOnlyList<IncludeTreeViewItem> cachedItems;
        public HierarchyIncludeTree()
        {
            cachedItems = empty;
        }
        public HierarchyIncludeTree(Include[] root_inc, IGraphModel model_ref, string root_file)
        {
            var cachedItemsList = new List<IncludeTreeViewItem>();

            foreach (var include in root_inc)
                cachedItemsList.Add(new HierarchyIncludeTreeViewItem(include, root_file, model_ref));
            cachedItems = cachedItemsList;
        }

        public IReadOnlyList<IncludeTreeViewItem> Children => cachedItems;
        private static readonly List<IncludeTreeViewItem> empty = new();
    }


    public class HierarchyIncludeTreeViewItem : IncludeTreeViewItem
    {
        private string ParentFilename = null;
        private IGraphModel model_ref;
        private int line = 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Synchronous property")]
        public override IReadOnlyList<IncludeTreeViewItem> Children
        {
            get
            {
                if (cachedItems == null)
                    GenerateChildItemsAsync().Wait();
                return cachedItems;
            }
        }
        protected IReadOnlyList<IncludeTreeViewItem> cachedItems;

        public HierarchyIncludeTreeViewItem(Include include, string ParentFilename, IGraphModel model_ref)
        {
            Reset(include, ParentFilename);
            this.model_ref = model_ref;
        }
        private async Task GenerateChildItemsAsync()
        {
            if (AbsoluteFilename == Name) 
                return;

            var item = await model_ref.TryEmplaceAsync(AbsoluteFilename, Name);
            var cachedItemsList = new List<IncludeTreeViewItem>();
            foreach (var include in item.Includes)
                cachedItemsList.Add(new HierarchyIncludeTreeViewItem(include, AbsoluteFilename, model_ref));
            cachedItems = cachedItemsList;
        }

        public void Reset(Include include, string includingFileAbsoluteFilename)
        {
            line = include.Line;
            cachedItems = null;
            Name = include.File;
            AbsoluteFilename = include.AbsolutePath;
            ParentFilename = includingFileAbsoluteFilename;
        }

        public override async Task NavigateToIncludeAsync()
        {
            // Want to navigate to origin of this include, not target if possible
            if (ParentFilename == null && !Path.IsPathRooted(ParentFilename)) return;
            var doc = await VS.Documents.OpenAsync(ParentFilename);
            if (doc == null) return;

            var lines = doc.TextView.TextViewLines;
            if(line >= lines.Count) return;

            var sel_line = lines[line];
            doc.TextView.ViewScroller.EnsureSpanVisible(sel_line.Extent);
            doc.TextView.Caret.MoveTo(sel_line);


            //{

            //    var fileWindow = VSUtils.OpenFileAndShowDocument(ParentFilename);

            //    // Try to move to carret if possible.
            //    if (include.IncludeLine != null)
            //    {
            //        var textDocument = fileWindow.Document.Object() as EnvDTE.TextDocument;

            //        if (textDocument != null)
            //        {
            //            var includeLinePoint = textDocument.StartPoint.CreateEditPoint();
            //            includeLinePoint.MoveToLineAndOffset(include.IncludeLine.LineNumber+1, 1);
            //            includeLinePoint.TryToShow();

            //            textDocument.Selection.MoveToPoint(includeLinePoint);
            //        }
            //    }
            //}
        }
    }
}
