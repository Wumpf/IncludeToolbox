using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    [Guid("769AFCC2-25E2-459A-B2A3-89D7308800BD")]
    public class ViewerOptionsPage : OptionsPage
    {
        public const string SubCategory = "Include Viewer & Graph";
        private const string collectionName = "IncludeViewer";

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
        private string[] noParsePaths;

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

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = GetSettingsStore();

            if (!settingsStore.CollectionExists(collectionName))
                settingsStore.CreateCollection(collectionName);

            var value = string.Join("\n", NoParsePaths);
            settingsStore.SetString(collectionName, nameof(NoParsePaths), value);


            settingsStore.SetBoolean(collectionName, nameof(CreateGroupNodesForFolders), CreateGroupNodesForFolders);
            settingsStore.SetBoolean(collectionName, nameof(ExpandFolderGroupNodes), ExpandFolderGroupNodes);
            settingsStore.SetBoolean(collectionName, nameof(ColorCodeNumTransitiveIncludes), ColorCodeNumTransitiveIncludes);
            settingsStore.SetInt32(collectionName, nameof(NoChildrenColor), NoChildrenColor.ToArgb());
            settingsStore.SetInt32(collectionName, nameof(MaxChildrenColor), MaxChildrenColor.ToArgb());
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = GetSettingsStore();

            if (settingsStore.PropertyExists(collectionName, nameof(NoParsePaths)))
            {
                var value = settingsStore.GetString(collectionName, nameof(NoParsePaths));
                NoParsePaths = value.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                // It is surprisingly hard to get to the standard library paths.
                // Even finding the VS install path is not easy and - as it turns out - not necessarily where the standard library files reside.
                // So in lack of a better idea, we just put everything under the program files folder in here.
                string programFiles = Environment.ExpandEnvironmentVariables("%ProgramW6432%");
                string programFilesX86 = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%");
                if(programFiles == programFilesX86) // If somebody uses still x86 system.
                    NoParsePaths = new string[] { programFiles  };
                else
                    NoParsePaths = new string[] { programFiles, programFilesX86 };
            }


            if (settingsStore.PropertyExists(collectionName, nameof(CreateGroupNodesForFolders)))
                CreateGroupNodesForFolders = settingsStore.GetBoolean(collectionName, nameof(CreateGroupNodesForFolders));
            if (settingsStore.PropertyExists(collectionName, nameof(ExpandFolderGroupNodes)))
                ExpandFolderGroupNodes = settingsStore.GetBoolean(collectionName, nameof(ExpandFolderGroupNodes));

            if (settingsStore.PropertyExists(collectionName, nameof(ColorCodeNumTransitiveIncludes)))
                ColorCodeNumTransitiveIncludes = settingsStore.GetBoolean(collectionName, nameof(ColorCodeNumTransitiveIncludes));
            if (settingsStore.PropertyExists(collectionName, nameof(NoChildrenColor)))
                NoChildrenColor = System.Drawing.Color.FromArgb(settingsStore.GetInt32(collectionName, nameof(NoChildrenColor)));
            if (settingsStore.PropertyExists(collectionName, nameof(MaxChildrenColor)))
                MaxChildrenColor = System.Drawing.Color.FromArgb(settingsStore.GetInt32(collectionName, nameof(MaxChildrenColor)));
        }
    }
}
