using Community.VisualStudio.Toolkit;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    public class IncludeGraphViewModel : PropertyNotify
    {
        public IncludeGraphViewModel() { }


        private IncludeGraph model = new();
        private HierarchyIncludeTree view = new();
        private string root_file = "";
        private int incs = 0;

        public HierarchyIncludeTree HierarchyIncludeTreeModel { get => view; private set => SetProperty(ref view, value); }


        public string RootFilename { get => root_file; set { SetProperty(ref root_file, value); } }
        public int NumIncludes
        {
            get => incs;
            set => SetProperty(ref incs, value);
        }

        public async Task RecalculateForAsync(DocumentView document)
        {
            var root = await model.InitializeAsync(document);
            HierarchyIncludeTreeModel = new(root.Includes, model, root.AbsoluteFilename);
            RootFilename = root.FormattedName;
            NumIncludes = root.Includes.Length;
        }
    }
}