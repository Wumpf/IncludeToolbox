using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    [Guid("DBC8A65D-8B86-4296-9F1F-E785B182B550")]
    public class TrialAndErrorRemovalOptionsPage : OptionsPage
    {
        public const string SubCategory = "Trial and Error Include Removal";
        private const string collectionName = "TryAndErrorRemoval"; // All "try and error" were updated to "trial and error", but need to keep old string here to preserve existing settings files.

        public enum IncludeRemovalOrder
        {
            BottomToTop,
            TopToBottom,
        }
        [Category(SubCategory)]
        [DisplayName("Removal Order")]
        [Description("Gives the order which #includes are removed.")]
        public IncludeRemovalOrder RemovalOrder { get; set; } = IncludeRemovalOrder.BottomToTop;

        [Category(SubCategory)]
        [DisplayName("Ignore First Include")]
        [Description("If true, the first include of a file will never be removed (useful for ignoring PCH).")]
        public bool IgnoreFirstInclude { get; set; } = true;

        [Category(SubCategory)]
        [DisplayName("Ignore List")]
        [Description("List of regexes. If the content of a #include directive match with any of these, it will be ignored." +
                       "\n\"" + RegexUtils.CurrentFileNameKey + "\" will be replaced with the current file name without extension.")]
        public string[] IgnoreList { get; set; } = new string[] { $"(\\/|\\\\|^){RegexUtils.CurrentFileNameKey}\\.(h|hpp|hxx|inl|c|cpp|cxx)$", ".inl", "_inl.h" };

        [Category(SubCategory)]
        [DisplayName("Keep Line Breaks")]
        [Description("If true, removed includes will leave an empty line.")]
        public bool KeepLineBreaks { get; set; } = false;


        public override void SaveSettingsToStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = GetSettingsStore();

            if (!settingsStore.CollectionExists(collectionName))
                settingsStore.CreateCollection(collectionName);

            settingsStore.SetInt32(collectionName, nameof(RemovalOrder), (int)RemovalOrder);
            settingsStore.SetBoolean(collectionName, nameof(IgnoreFirstInclude), IgnoreFirstInclude);

            var value = string.Join("\n", IgnoreList);
            settingsStore.SetString(collectionName, nameof(IgnoreList), value);

            settingsStore.SetBoolean(collectionName, nameof(KeepLineBreaks), KeepLineBreaks);
        }

        public override void LoadSettingsFromStorage()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var settingsStore = GetSettingsStore();

            if (settingsStore.PropertyExists(collectionName, nameof(RemovalOrder)))
                RemovalOrder = (IncludeRemovalOrder)settingsStore.GetInt32(collectionName, nameof(RemovalOrder));
            if (settingsStore.PropertyExists(collectionName, nameof(IgnoreFirstInclude)))
                IgnoreFirstInclude = settingsStore.GetBoolean(collectionName, nameof(IgnoreFirstInclude));

            if (settingsStore.PropertyExists(collectionName, nameof(IgnoreList)))
            {
                var value = settingsStore.GetString(collectionName, nameof(IgnoreList));
                IgnoreList = value.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (settingsStore.PropertyExists(collectionName, nameof(KeepLineBreaks)))
                KeepLineBreaks = settingsStore.GetBoolean(collectionName, nameof(KeepLineBreaks));
        }
    }
}
