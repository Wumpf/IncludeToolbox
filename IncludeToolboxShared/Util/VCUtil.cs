using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.VisualStudio.VSConstants;
using Project = Community.VisualStudio.Toolkit.Project;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    public static class ProjectExtension
    {
        public static async Task<VCProject> ToVCProjectAsync(this Project project)
        {
            project.GetItemInfo(out var hierarchy, out _, out _);
            return await VCUtil.GetVCProjectAsync(hierarchy);
        }
    }

    

    public class VCUtil
    {
        static VCProject cached_project;
        static string command_line = "";
        static Standard std = Standard.cpp23;

        public static Standard Std { get => std; }

        public static EnvDTE80.DTE2 GetDTE() //DEPRECATED
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if (dte == null)
            {
                throw new System.Exception("Failed to retrieve DTE2!");
            }
            return dte;
        }

        public static async Task<VCProject> GetVCProjectAsync(IVsHierarchy hierarchy)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _ = hierarchy.GetProperty(VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out var objProj);
            return (objProj as EnvDTE.Project)?.Object as VCProject;
        }

        

        public static async Task<IEnumerable<string>> GetIncludeDirsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var proj = await (await VS.Solutions.GetActiveProjectAsync()).ToVCProjectAsync();
            if (proj == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "The project is not a Visual Studio C/C++ type.").FireAndForget(); return null; }

            var cfg = proj.ActiveConfiguration;
            var cl = cfg?.Rules;
            if (cl == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "Failed to gather Compiler info.").FireAndForget(); return null; }
            var com = (IVCRulePropertyStorage2)cl.Item("CL");
            return com.GetEvaluatedPropertyValue("AdditionalIncludeDirectories").Replace('\\','/')
                .Split(';').Where(s => !string.IsNullOrWhiteSpace(s));
        }

        public static async Task<string> GetCommandLineAsync(bool rebuild)
        {
            return await GetCommandLineAsync(rebuild, await VS.Solutions.GetActiveProjectAsync());
        }

        public static async Task<string> GetCommandLineAsync(bool rebuild, Project xproj)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var proj = await xproj.ToVCProjectAsync();
            
            if (cached_project == proj && !rebuild)
                return command_line;

            cached_project = proj;

            if (proj == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "The project is not a Visual Studio C/C++ type.").FireAndForget(); return null; }

            var cfg = proj.ActiveConfiguration;
            var cl = cfg?.Rules;
            if (cl == null) { VS.MessageBox.ShowErrorAsync("IWYU Error:", "Failed to gather Compiler info.").FireAndForget(); return null; }

            var com = (IVCRulePropertyStorage2)cl.Item("CL");
            var xstandard = com.GetEvaluatedPropertyValue("LanguageStandard");
            var includes = com.GetEvaluatedPropertyValue("AdditionalIncludeDirectories").Replace('\\', '/')
                .Split(';').Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(x => "-I\"" + x + '\"');
            var defs = com.GetEvaluatedPropertyValue("PreprocessorDefinitions")
                .Split(';').Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(x => "-D" + x);

            std = ExtensionMethods.FromMSVCFlag(xstandard);
            string standard = std.ToStdFlag();

            var inc_string = string.Join(" ", includes);
            var def_string = string.Join(" ", defs);


            return command_line = inc_string + ' ' + def_string + ' ' + standard;
        }
    }
}
