using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    [Guid("69CFD797-2E2B-497E-9231-334BCDC41407")]
    public class IncludeWhatYouUseOptionsPage : OptionsPage
    {
        public const string SubCategory = "Include-What-You-Use";
        private const string collectionName = "IncludeFormatter";

        #region iwyu source

        [Category("iwyu general")]
        [DisplayName("Executable Path")]
        [Description("File path of include-what-you-use.exe. If automatic download is active, this folder will be used.")]
        public string ExecutablePath { get; set; } = "";

        [Category("iwyu general")]
        [DisplayName("Automatic Updates")]
        [Description("If true, automatic check for updates will be done on first use each session. Will download from https://github.com/Wumpf/iwyu_for_vs_includetoolbox. " +
                     "Set this to false if you want to use your own include-what-you-use version.")]
        public bool AutomaticCheckForUpdates { get; set; } = true;

        static public string GetDefaultExecutablePath()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "iwyu", "include-what-you-use.exe");
        }

        #endregion

        #region iwyu options

        [Category("iwyu options")]
        [DisplayName("Log Verbosity")]
        [Description("The higher the level, the more output. A level lower 1 might disable automatic include replacing (--verbose)")]
        public int LogVerbosity { get; set; } = 2;

        [Category("iwyu options")]
        [DisplayName("Mapping File")]
        [Description("Gives iwyu a mapping file. (--mapping_file)")]
        public string[] MappingFiles { get; set; } = new string[0];

        /// <summary>
        /// Adds a list of mapping files and eliminates duplicates.
        /// </summary>
        public void AddMappingFiles(IEnumerable<string> mappingFilesPaths)
        {
            var resolvedNewFiles = mappingFilesPaths.Select(x => new KeyValuePair<string, string>(Path.GetFullPath(x), x));
            var resolvedOldFiles = MappingFiles.Select(x => new KeyValuePair<string, string>(Path.GetFullPath(x), x));
            MappingFiles = resolvedNewFiles.Union(resolvedOldFiles).Select(x => x.Value).ToArray();
        }

        [Category("iwyu options")]
        [DisplayName("No Default Mappings")]
        [Description("Do not add iwyu's default mappings. (--no_default_mappings)")]
        public bool NoDefaultMappings { get; set; } = false; 
        
        [Category("iwyu options")]
        [DisplayName("Commentaries")]
        [Description("Change the output mode of the commentaries from IWYU.")]
        [TypeConverter(typeof(EnumConverter))]
        public Commentaries Commentary { get; set; } = Commentaries.Default;

        [Category("iwyu options")]
        [DisplayName("PCH in Code")]
        [Description("Mark the first include in a translation unit as a precompiled header. Use to prevent IWYU from removing necessary PCH includes. Though Clang forces PCHs to be listed as prefix headers, the PCH in code pattern can be used with GCC and is standard practice on MSVC (e.g.stdafx.h). (--pch_in_code)")]
        public bool PCHInCode { get; set; } = true;

        public enum PrefixHeaderMode
        {
            Add,
            Keep,
            Remove
        }
        [Category("iwyu options")]
        [DisplayName("Prefix Header Include Mode")]
        [Description("Tells iwyu what to do with in-source includes and forward declarations involving prefix headers. Prefix header is a file included via command-line option -include. If prefix header makes include or forward declaration obsolete, presence of such include can be controlled with the following values:\nAdd: new lines are added\nKeep: new lines aren't added, existing are kept intact\nRemove: new lines aren't added, existing are removed. (--prefix_header_includes)")]
        public PrefixHeaderMode PrefixHeaderIncludes { get; set; } = PrefixHeaderMode.Add;

        [Category("iwyu options")]
        [DisplayName("Transitive Includes Only")]
        [Description("Do not suggest that a file add foo.h unless foo.h is already visible in the file's transitive includes. (--transitive_includes_only)")]
        public bool TransitiveIncludesOnly { get; set; } = false;

        [Category("iwyu options")]
        [DisplayName("Additional Parameters")]
        [Description("This string is inserted after all other parameters and before the filename of the file iwyu is running on.")]
        public string AdditionalParameters { get; set; } = "";


        [Category("iwyu options")]
        [DisplayName("Header file prefix")]
        [Description("Specify Header file prefix folder. Used to find pair header eg. a.cpp has an include/proj/a.h path to it, so the value should be ./include/proj. Works only relative to file.")]
        public string HeaderPrefix { get; set; } = "";

        #endregion

        #region postprocessing

        [Category("Post Processing")]
        [DisplayName("Apply Proposal")]
        [Description("Applies iwyu's proposal directly to all files. Requires at least Log Verbosity of 1.")]
        public bool ApplyProposal { get; set; } = true;

        [Category("Post Processing")]
        [DisplayName("Run Include Formatter on Changes")]
        [Description("Runs the Include Formatter on all changed lines.")]
        public bool RunIncludeFormatter { get; set; } = true;


        #endregion

        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = GetSettingsStore();

            if (!settingsStore.CollectionExists(collectionName))
                settingsStore.CreateCollection(collectionName);

            settingsStore.SetString(collectionName, nameof(ExecutablePath), ExecutablePath);
            settingsStore.SetBoolean(collectionName, nameof(AutomaticCheckForUpdates), AutomaticCheckForUpdates);

            settingsStore.SetInt32(collectionName, nameof(LogVerbosity), LogVerbosity);

            var value = string.Join("\n", MappingFiles);
            settingsStore.SetString(collectionName, nameof(MappingFiles), value);

            settingsStore.SetBoolean(collectionName, nameof(NoDefaultMappings), NoDefaultMappings);
            settingsStore.SetBoolean(collectionName, nameof(PCHInCode), PCHInCode);
            settingsStore.SetInt32(collectionName, nameof(PrefixHeaderIncludes), (int)PrefixHeaderIncludes);
            settingsStore.SetBoolean(collectionName, nameof(TransitiveIncludesOnly), TransitiveIncludesOnly);
            settingsStore.SetString(collectionName, nameof(AdditionalParameters), AdditionalParameters);
            settingsStore.SetString(collectionName, nameof(HeaderPrefix), HeaderPrefix);

            settingsStore.SetBoolean(collectionName, nameof(ApplyProposal), ApplyProposal);
            settingsStore.SetBoolean(collectionName, nameof(RunIncludeFormatter), RunIncludeFormatter);
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = GetSettingsStore();

            if (settingsStore.PropertyExists(collectionName, nameof(ExecutablePath)))
                ExecutablePath = settingsStore.GetString(collectionName, nameof(ExecutablePath));
            else
                ExecutablePath = GetDefaultExecutablePath();
            if (settingsStore.PropertyExists(collectionName, nameof(AutomaticCheckForUpdates)))
                AutomaticCheckForUpdates = settingsStore.GetBoolean(collectionName, nameof(AutomaticCheckForUpdates));

            if (settingsStore.PropertyExists(collectionName, nameof(LogVerbosity)))
                LogVerbosity = settingsStore.GetInt32(collectionName, nameof(LogVerbosity));
            if (settingsStore.PropertyExists(collectionName, nameof(MappingFiles)))
            {
                var value = settingsStore.GetString(collectionName, nameof(MappingFiles));
                MappingFiles = value.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            if (settingsStore.PropertyExists(collectionName, nameof(NoDefaultMappings)))
                NoDefaultMappings = settingsStore.GetBoolean(collectionName, nameof(NoDefaultMappings));
            if (settingsStore.PropertyExists(collectionName, nameof(PCHInCode)))
                PCHInCode = settingsStore.GetBoolean(collectionName, nameof(PCHInCode));
            if (settingsStore.PropertyExists(collectionName, nameof(PrefixHeaderIncludes)))
                PrefixHeaderIncludes = (PrefixHeaderMode)settingsStore.GetInt32(collectionName, nameof(PrefixHeaderIncludes));
            if (settingsStore.PropertyExists(collectionName, nameof(TransitiveIncludesOnly)))
                TransitiveIncludesOnly = settingsStore.GetBoolean(collectionName, nameof(TransitiveIncludesOnly));
            if (settingsStore.PropertyExists(collectionName, nameof(AdditionalParameters)))
                AdditionalParameters = settingsStore.GetString(collectionName, nameof(AdditionalParameters));
            if (settingsStore.PropertyExists(collectionName, nameof(HeaderPrefix)))
                HeaderPrefix = settingsStore.GetString(collectionName, nameof(HeaderPrefix));

            if (settingsStore.PropertyExists(collectionName, nameof(ApplyProposal)))
                ApplyProposal = settingsStore.GetBoolean(collectionName, nameof(ApplyProposal));
            if (settingsStore.PropertyExists(collectionName, nameof(RunIncludeFormatter)))
                RunIncludeFormatter = settingsStore.GetBoolean(collectionName, nameof(RunIncludeFormatter));
        }
    }

    public enum Commentaries
    {
        Default,
        Always,
        Never
    }

}
