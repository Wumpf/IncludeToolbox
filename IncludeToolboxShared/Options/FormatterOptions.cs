using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class FormatterOptionsPage : BaseOptionPage<FormatOptions> { }
    }

    public class FormatOptions : BaseOptionModel<FormatOptions>
    {
        [Category("Path")]
        [DisplayName("Mode")]
        [Description("Changes the path mode to the given pattern.")]
        public PathMode PathFormat { get; set; } = PathMode.Shortest_AvoidUpSteps;

        [Category("Path")]
        [DisplayName("Ignore File Relative")]
        [Description("If true, include directives will not take the path of the file into account.")]
        public bool IgnoreFileRelative { get; set; } = false;



        [Category("Formatting")]
        [DisplayName("Delimiter Mode")]
        [Description("Optionally changes all delimiters to either angle brackets <...> or quotes \"...\".")]
        public DelimiterMode DelimiterFormatting { get; set; } = DelimiterMode.Unchanged;


        [Category("Formatting")]
        [DisplayName("Slash Mode")]
        [Description("Changes all slashes to the given type.")]
        public SlashMode SlashFormatting { get; set; } = SlashMode.ForwardSlash;

        [Category("Formatting")]
        [DisplayName("Remove Empty Lines")]
        [Description("If true, all empty lines of a include selection will be removed.")]
        public bool RemoveEmptyLines { get; set; } = true;



        [Category("Sorting")]
        [DisplayName("Include delimiters in precedence regexes")]
        [Description("If true, precedence regexes will consider delimiters (angle brackets or quotes.)")]
        public bool RegexIncludeDelimiter { get; set; } = false;

        [Category("Sorting")]
        [DisplayName("Insert blank line between precedence regex match groups")]
        [Description("If true, a blank line will be inserted after each group matching one of the precedence regexes.")]
        public bool BlankAfterRegexGroupMatch { get; set; } = false;

        [Category("Sorting")]
        [DisplayName("Precedence Regexes")]
        [Description("Earlier match means higher sorting priority.\n\"" + RegexUtils.CurrentFileNameKey + "\" will be replaced with the current file name without extension.")]
        public string[] PrecedenceRegexes {
            get { return precedenceRegexes; }
            set { precedenceRegexes = value.Where(x => x.Length > 0).ToArray(); } // Remove empty lines.
        }
        private string[] precedenceRegexes = new string[] { $"(?i){RegexUtils.CurrentFileNameKey}\\.(h|hpp|hxx|inl|c|cpp|cxx)(?-i)" };


        [Category("Sorting")]
        [DisplayName("Sort by Include Type")]
        [Description("Optionally put either includes with angle brackets <...> or quotes \"...\" first.")]
        public TypeSorting SortByType { get; set; } = TypeSorting.QuotedFirst;

        [Category("Sorting")]
        [DisplayName("Remove duplicates")]
        [Description("If true, duplicate includes will be removed.")]
        public bool RemoveDuplicates { get; set; } = true;
    }


    public enum PathMode
    {
        Unchanged,
        Shortest,
        Shortest_AvoidUpSteps,
        Absolute,
    }
    public enum DelimiterMode
    {
        Unchanged,
        AngleBrackets,
        Quotes,
    }
    public enum SlashMode
    {
        Unchanged,
        ForwardSlash,
        BackSlash,
    }
    public enum TypeSorting
    {
        None,
        AngleBracketsFirst,
        QuotedFirst,
    }
}
