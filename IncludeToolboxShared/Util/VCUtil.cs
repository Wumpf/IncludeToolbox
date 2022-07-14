using Community.VisualStudio.Toolkit;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    public class VCUtil
    {
        static EnvDTE.Project cached_project;
        public static EnvDTE80.DTE2 GetDTE()
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if (dte == null)
            {
                throw new System.Exception("Failed to retrieve DTE2!");
            }
            return dte;
        }
        public static async Task<EnvDTE.Project> GetProjectAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var doc = GetDTE().ActiveDocument;
            return doc.ProjectItem?.ContainingProject;
        }

        public static async Task<IEnumerable<string>> GetIncludeDirsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var prj = await GetProjectAsync();
            var proj = prj.Object as VCProject;
            if (proj == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "The project is not a Visual Studio C/C++ type.").FireAndForget(); return null; }

            var cfg = proj.ActiveConfiguration;
            var cl = cfg?.Rules;
            if (cl == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "Failed to gather Compiler info.").FireAndForget(); return null; }
            var com = (IVCRulePropertyStorage2)cl.Item("CL");
            return com.GetEvaluatedPropertyValue("AdditionalIncludeDirectories")
                .Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
        }

        public static async Task<string> GetCommandLineAsync(bool rebuild)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var prj = await GetProjectAsync();

            if (cached_project == prj && !rebuild)
                return "";

            cached_project = prj;

            var proj = prj.Object as VCProject;
            if (proj == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "The project is not a Visual Studio C/C++ type.").FireAndForget(); return null; }

            var cfg = proj.ActiveConfiguration;
            var cl = cfg?.Rules;
            if (cl == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "Failed to gather Compiler info.").FireAndForget(); return null; }

            var com = (IVCRulePropertyStorage2)cl.Item("CL");
            var xstandard = com.GetEvaluatedPropertyValue("LanguageStandard");
            var includes = com.GetEvaluatedPropertyValue("AdditionalIncludeDirectories")
                .Split(';').Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(x => "-I\"" + x + '\"');
            var defs = com.GetEvaluatedPropertyValue("PreprocessorDefinitions")
                .Split(';').Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(x => "-D" + x);

            string standard;
            switch (xstandard)
            {
                default:
                case "stdcpp20":
                    standard = "-std=c++20";
                    break;
                case "stdcpp17":
                    standard = "-std=c++17";
                    break;
                case "stdcpp14":
                case "Default":
                    standard = "-std=c++14";
                    break;
            }

            var inc_string = string.Join(" ", includes);
            var def_string = string.Join(" ", defs);


            return inc_string + ' ' + def_string + ' ' + standard;
        }
    }
}
