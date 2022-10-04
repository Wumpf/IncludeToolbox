using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class TransitiveOptionsPage : BaseOptionPage<TransitiveOptions> { }
    }

    public class TransitiveOptions : BaseOptionModel<TransitiveOptions>
    {
        [Category("Parsing")]
        [DisplayName("Stable Directories")]
        [Description("List of absolute directories, that are treated as stable and unchanged for a VS session. The files from those directories will be only parsed once.")]
        public string[] StablePaths
        {
            get;
            set;
        } = new string[0];
    }
}
