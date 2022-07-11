using IncludeToolbox.Graph;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox.GraphWindow
{
    public class FolderIncludeTreeViewItem_Root : IncludeTreeViewItem
    {
        IReadOnlyCollection<IncludeGraph.GraphItem> graphItems;
        IncludeGraph.GraphItem includingFile;

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


        public FolderIncludeTreeViewItem_Root(IReadOnlyCollection<IncludeGraph.GraphItem> graphItems, IncludeGraph.GraphItem includingFile)
        {
            this.graphItems = graphItems;
            this.includingFile = includingFile;
            this.Name = "Root";
            this.AbsoluteFilename = "Root";
        }

        public void Reset(IReadOnlyCollection<IncludeGraph.GraphItem> graphItems, IncludeGraph.GraphItem includingFile)
        {
            this.graphItems = graphItems;
            this.includingFile = includingFile;
            this.cachedItems = null;
            NotifyAllPropertiesChanged();
        }

        private void GenerateChildItems()
        {
            if (graphItems == null)
            {
                cachedItems = emptyList;
                return;
            }

            var rootChildren = new List<IncludeTreeViewItem>();
            cachedItems = rootChildren;

            // Create first layer of folder and leaf items
            var leafItems = new List<FolderIncludeTreeViewItem_Leaf>();
            foreach (IncludeGraph.GraphItem item in graphItems)
            {
                if (item == includingFile)
                    continue;

                leafItems.Add(new FolderIncludeTreeViewItem_Leaf(item));
            }

            // Group by folder.
            if (leafItems.Count > 0)
            {
                leafItems.Sort((x, y) => x.ParentFolder.CompareTo(y.ParentFolder));

                var root = new FolderIncludeTreeViewItem_Folder("", 0);
                GroupIncludeRecursively(root, leafItems, 0, leafItems.Count, 0);
                rootChildren.AddRange(root.ChildrenList);
            }
        }

        private string GetNextPathPrefix(string path, int begin)
        {
            int nextSlash = begin;
            while (path.Length > nextSlash && path[nextSlash] != Path.DirectorySeparatorChar)
                ++nextSlash;
            return path.Substring(0, nextSlash);
        }

        private string LargestCommonFolderPrefixInRange(List<FolderIncludeTreeViewItem_Leaf> allLeafItems, int begin, int end, string commonPrefix)
        {
            string folderBegin = allLeafItems[begin].ParentFolder;
            string folderEnd = allLeafItems[end-1].ParentFolder;

            string prefixCandidate = commonPrefix;
            string previousPrefix = null;
            do
            {
                if (folderBegin.Length == prefixCandidate.Length)
                    return prefixCandidate;

                previousPrefix = prefixCandidate;
                prefixCandidate = GetNextPathPrefix(folderBegin, previousPrefix.Length + 1);

            } while (folderEnd.StartsWith(prefixCandidate));

            return previousPrefix;
        }

        private void GroupIncludeRecursively(FolderIncludeTreeViewItem_Folder parentFolder, List<FolderIncludeTreeViewItem_Leaf> allLeafItems, int begin, int end, int commonPrefixLength)
        {
            System.Diagnostics.Debug.Assert(begin < end);
            System.Diagnostics.Debug.Assert(allLeafItems.Count >= end);

            // Look through the sorted subsection of folders and find ranges where the prefix changes.
            while(begin < end)
            {
                // New subgroup to look at!
                string currentPrefix = GetNextPathPrefix(allLeafItems[begin].ParentFolder, commonPrefixLength + 1);

                // Find end of the rest of the group and expand recurively.
                for (int i = begin; i <= end; ++i)
                {
                    if (i == end || !allLeafItems[i].ParentFolder.StartsWith(currentPrefix))
                    {
                        // Find maximal prefix of this group.
                        string largestPrefix = LargestCommonFolderPrefixInRange(allLeafItems, begin, i, currentPrefix);
                        var newGroup = new FolderIncludeTreeViewItem_Folder(largestPrefix, commonPrefixLength);
                        parentFolder.ChildrenList.Add(newGroup);

                        // If there are any direct children, they will be first due to sorting. Add them to the new group and ignore this part of the range.
                        while (allLeafItems[begin].ParentFolder.Length == largestPrefix.Length)
                        {
                            newGroup.ChildrenList.Add(allLeafItems[begin]);
                            ++begin;
                            if (begin == i)
                                break;
                        }

                        // What's left is non-direct children (== folders!) that we need to handle recursively.
                        int numFoldersInGroup = i - begin;
                        if (numFoldersInGroup > 0)
                        {
                            GroupIncludeRecursively(newGroup, allLeafItems, begin, i, largestPrefix.Length);
                        }

                        // Next group starts at this element.
                        begin = i;
                        break;
                    }
                }
            }
        }

        public override Task NavigateToInclude()
        {
            return Task.CompletedTask;
        }
    }

    public class FolderIncludeTreeViewItem_Folder : IncludeTreeViewItem
    {
        public override IReadOnlyList<IncludeTreeViewItem> Children => ChildrenList;
        public List<IncludeTreeViewItem> ChildrenList { get; private set; } = new List<IncludeTreeViewItem>();

        public const string UnresolvedFolderName = "<unresolved>";

        public FolderIncludeTreeViewItem_Folder(string largestPrefix, int commonPrefixLength)
        {
            AbsoluteFilename = largestPrefix;

            largestPrefix.Substring(commonPrefixLength);

            if (largestPrefix != UnresolvedFolderName)
            {
                var stringBuilder = new StringBuilder(largestPrefix);
                stringBuilder.Remove(0, commonPrefixLength);
                stringBuilder.Append(Path.DirectorySeparatorChar);
                if (stringBuilder[0] == Path.DirectorySeparatorChar)
                    stringBuilder.Remove(0, 1);

                Name = stringBuilder.ToString();
            }
            else
            {
                Name = largestPrefix;
            }
        }

        public override Task NavigateToInclude()
        {
            // todo?
            return Task.CompletedTask;
        }
    }

    public class FolderIncludeTreeViewItem_Leaf : IncludeTreeViewItem
    {
        public override IReadOnlyList<IncludeTreeViewItem> Children => emptyList;

        /// <summary>
        /// Parent folder of this leaf. Needed during build-up.
        /// </summary>
        public string ParentFolder { get; private set; }

        public FolderIncludeTreeViewItem_Leaf(IncludeGraph.GraphItem item)
        {
            try
            {
                Name = Path.GetFileName(item.AbsoluteFilename);
            }
            catch
            {
                Name = item?.FormattedName;
            }
            AbsoluteFilename = item?.AbsoluteFilename;

            if (string.IsNullOrWhiteSpace(AbsoluteFilename) || !Path.IsPathRooted(AbsoluteFilename))
                ParentFolder = FolderIncludeTreeViewItem_Folder.UnresolvedFolderName;
            else
                ParentFolder = Path.GetDirectoryName(AbsoluteFilename);
        }

        public override async Task NavigateToInclude()
        {
            if (AbsoluteFilename != null && Path.IsPathRooted(AbsoluteFilename))
            {
                await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var fileWindow = VSUtils.OpenFileAndShowDocument(AbsoluteFilename);
            }
        }
    }
}
