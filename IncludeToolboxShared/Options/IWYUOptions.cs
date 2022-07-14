using Community.VisualStudio.Toolkit;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IncludeToolbox
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class XIWYUOptions : BaseOptionPage<IWYUOptions> { }
    }

    public class IWYUOptions : BaseOptionModel<IWYUOptions>
    {
        string exe = "";
        Comment comm = Comment.Default;
        uint verbose = 2;
        bool pch = false;
        bool nodefault = false;
        bool transitives = false;
        bool warn = false;
        bool always = false;
        bool header = false;
        string mapping = "";
        string[] clang_options = new string[] { };
        string[] iwyu_options = new string[] { };

        [Browsable(false)]
        public bool Dirty { get; private set; }

        [Category("General")]
        [DisplayName("IWYU Executable")]
        [Description("A path to IWYU executable.")]
        [DefaultValue("")]
        public string Executable
        {
            get { return exe; }
            set
            {
                Dirty = true;
                exe = value;
            }
        }

        [Category("General")]
        [DisplayName("Print Commentaries")]
        [Description("Tells IWYU to show or hide individual commentaries to headers.")]
        [DefaultValue(Comment.Default)]
        [TypeConverter(typeof(EnumConverter))]
        public Comment Comms { get { return comm; } set { Dirty = true; comm = value; } }
        
        [Category("General")]
        [DisplayName("Output Verbosity")]
        [Description("Determines how much output needs to be printed. May help in case of error. Max value is 7.")]
        [DefaultValue(2)]
        public uint Verbosity{ get { return verbose; } set { Dirty = true; verbose = Math.Min(value, 7); } }

        [Category("General")]
        [DisplayName("Precompiled Header")]
        [Description("Sets if first file in .cpp is precompiled header. Blocks first file from being parsed.")]
        [DefaultValue(false)]
        public bool Precompiled { get { return pch; } set { Dirty = true; pch = value; } }
        
        [Category("General")]
        [DisplayName("No Default Maps")]
        [Description("If true, turns default gcc iwyu STL bindings off. Useful for STL map implementation.")]
        [DefaultValue(false)]
        public bool NoDefault { get { return nodefault; } set { Dirty = true; nodefault = value; } }
        
        [Category("General")]
        [DisplayName("Only Transitive")]
        [Description("Do not suggest that a file add foo.h unless foo.h is already visible in the file's transitive includes.")]
        [DefaultValue(false)]
        public bool Transitives { get { return transitives; } set { Dirty = true; transitives = value; } }        
        
        [Category("General")]
        [DisplayName("Show Warnings")]
        [Description("Shows warnings from IWYU compiler.")]
        [DefaultValue(false)]
        public bool Warnings { get { return warn; } set { Dirty = true; warn = value; } }        
        
        [Category("General")]
        [DisplayName("Always Rebuild")]
        [Description("Rebuild the project command line on each call. Good for dynamic projects, that may change their options.")]
        [DefaultValue(false)]
        public bool AlwaysRebuid { get { return always; } set { Dirty = true; always = value; } }

        [Category("Options")]
        [DisplayName("IWYU options")]
        [Description("IWYU launch options, that determine the flow of include-what-you-use.")]
        public string[] Options { get { return iwyu_options; } set { iwyu_options = value; Dirty = true; } }

        [Category("Options")]
        [DisplayName("Clang options")]
        [Description("Clang launch options, that determine compilation stage flow.")]
        public string[] ClangOptions { get { return clang_options; } set { clang_options = value; Dirty = true; } }
        
        [Category("Options")]
        [DisplayName("Mapping File")]
        [Description("Specifies the mapping file to use by iwyu.")]
        [DefaultValue("")]
        public string MappingFile { get { return mapping; } set { mapping = value; Dirty = true; } }

        [Category("Options")]
        [DisplayName("Ignore Header")]
        [Description("Ignores header of specified .cpp. Tries to find same-named .h in the includes. If it succeeds, the header is moved to the beginning and it is treated as precompiled. Useful when .h file is already refactored.")]
        [DefaultValue(false)]
        public bool IgnoreHeader { get { return header; } set { header = value; Dirty = true; } }

        public void ClearFlag()
        {
            Dirty = false;
        }
    }

    public enum Comment
    {
        Default,
        Always,
        Never
    }
}
