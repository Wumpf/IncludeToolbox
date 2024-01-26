using System.Collections.Generic;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    public abstract class IncludeTreeViewItem : PropertyNotify
    {
        static protected IReadOnlyList<IncludeTreeViewItem> emptyList = new IncludeTreeViewItem[0];

        string name = "";
        string abs_name = "";
        public string Name { get => name; protected set => SetProperty(ref name, value); }
        public string AbsoluteFilename { get => abs_name; protected set => SetProperty(ref abs_name, value); }
        public abstract IReadOnlyList<IncludeTreeViewItem> Children { get; }

        abstract public Task NavigateToIncludeAsync();
    }
}
