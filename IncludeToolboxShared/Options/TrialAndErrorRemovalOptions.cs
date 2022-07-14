using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class TrialAndErrorRemovalOptions : BaseOptionPage<IncludeToolbox.TrialAndErrorRemovalOptions> { }
    }

    public class TrialAndErrorRemovalOptions : BaseOptionModel<TrialAndErrorRemovalOptions>
    {
        [Category("General")]
        [DisplayName("Removal Order")]
        [Description("Gives the order which #includes are removed.")]
        public IncludeRemovalOrder RemovalOrder { get; set; } = IncludeRemovalOrder.BottomToTop;

        [Category("General")]
        [DisplayName("Ignore First Include")]
        [Description("If true, the first include of a file will never be removed (useful for ignoring PCH).")]
        public bool IgnoreFirstInclude { get; set; } = true;

        [Category("General")]
        [DisplayName("Ignore List")]
        [Description("List of regexes. If the content of a #include directive match with any of these, it will be ignored." +
                       "\n\"" + RegexUtils.CurrentFileNameKey + "\" will be replaced with the current file name without extension.")]
        public string[] IgnoreList { get; set; } = new string[] { $"(\\/|\\\\|^){RegexUtils.CurrentFileNameKey}\\.(h|hpp|hxx|inl|c|cpp|cxx)$", ".inl", "_inl.h" };

        [Category("General")]
        [DisplayName("Keep Line Breaks")]
        [Description("If true, removed includes will leave an empty line.")]
        public bool KeepLineBreaks { get; set; } = false;
    }
    public enum IncludeRemovalOrder
    {
        BottomToTop,
        TopToBottom,
    }
}
