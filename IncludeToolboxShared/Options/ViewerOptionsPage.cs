using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class ViewerOptionsPage : BaseOptionPage<ViewerOptions> { }
    }

    public class ViewerOptions : BaseOptionModel<ViewerOptions>
    {
        [Category("Include Graph Parsing")]
        [DisplayName("Graph Endpoint Directories")]
        [Description("List of absolute directory paths. For any include below these paths, the graph parsing will stop.")]
        public string[] NoParsePaths
        {
            get { return noParsePaths; }
            set
            {
                // It is critical that the paths are "exact" since we want to use them as with string comparison.
                noParsePaths = value;
                for (int i = 0; i < noParsePaths.Length; ++i)
                    noParsePaths[i] = Utils.GetExactPathName(noParsePaths[i]);
            }
        }
        private string[] noParsePaths = new string[0];

        [Category("Include Graph DGML")]
        [DisplayName("Create Group Nodes by Folders")]
        [Description("Creates folders like in the folder hierarchy view of Include Graph.")]
        public bool CreateGroupNodesForFolders { get; set; } = true;

        [Category("Include Graph DGML")]
        [DisplayName("Expand Folder Group Nodes")]
        [Description("If true all folder nodes start out expanded, otherwise they are collapsed.")]
        public bool ExpandFolderGroupNodes { get; set; } = false;

        [Category("Include Graph DGML")]
        [DisplayName("Colorize by Number of Includes")]
        [Description("If true each node gets color coded according to its number of unique transitive includes.")]
        public bool ColorCodeNumTransitiveIncludes { get; set; } = true;

        [Category("Include Graph DGML")]
        [DisplayName("No Children Color")]
        [Description("See \"Colorize by Number of Includes\". Color for no children at all.")]
        public System.Drawing.Color NoChildrenColor { get; set; } = System.Drawing.Color.White;

        [Category("Include Graph DGML")]
        [DisplayName("Max Children Color")]
        [Description("See \"Colorize by Number of Includes\". Color for highest number of children.")]
        public System.Drawing.Color MaxChildrenColor { get; set; } = System.Drawing.Color.Red;
    }
}
