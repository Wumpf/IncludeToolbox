using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    public class VCQueryFailure : System.Exception
    {
        public VCQueryFailure(string message) : base(message)
        {
        }
    }

    public class VCHelper
    {
        public bool IsVCProject(Project project)
        {
            return project?.Object is VCProject;
        }

        private static async Task<VCFileConfiguration> GetVCFileConfigForCompilation(Document document)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (document == null)
                throw new VCQueryFailure("No document.");

            var vcProject = document.ProjectItem?.ContainingProject?.Object as VCProject;
            if (vcProject == null)
                throw new VCQueryFailure("The given document does not belong to a VC++ Project.");

            VCFile vcFile = document.ProjectItem?.Object as VCFile;
            if (vcFile == null)
                throw new VCQueryFailure("The given document is not a VC++ file.");

            if (vcFile.FileType != eFileType.eFileTypeCppCode)
                throw new VCQueryFailure("The given document is not a compileable VC++ file.");

            IVCCollection fileConfigCollection = vcFile.FileConfigurations as IVCCollection;
            VCFileConfiguration fileConfig = fileConfigCollection?.Item(vcProject.ActiveConfiguration.Name) as VCFileConfiguration;
            if (fileConfig == null)
                throw new VCQueryFailure("Failed to retrieve file config from document.");

            return fileConfig;
        }

        private static VCTool GetToolFromActiveConfiguration<VCTool>(Project project) where VCTool: class
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            VCProject vcProject = project?.Object as VCProject;
            if (vcProject == null)
                throw new VCQueryFailure($"Failed to retrieve VCCLCompilerTool since project \"{project.Name}\" is not a VCProject.");

            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            VCTool compilerTool = null;
            foreach (var tool in (IVCCollection)activeConfiguration.Tools)
            {
                compilerTool = tool as VCTool;
                if (compilerTool != null)
                    break;
            }

            if (compilerTool == null)
                throw new VCQueryFailure($"Couldn't find a {typeof(VCTool).Name} in active configuration of VC++ Project \"{vcProject.Name}\"");

            return compilerTool;
        }

        public static VCLinkerTool GetLinkerTool(Project project)
        {
            VCProject vcProject = project?.Object as VCProject;
            if (vcProject == null)
                throw new VCQueryFailure("Failed to retrieve VCLinkerTool since project is not a VCProject.");

            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            var tools = activeConfiguration.Tools;
            VCLinkerTool linkerTool = null;
            foreach (var tool in (IVCCollection)activeConfiguration.Tools)
            {
                linkerTool = tool as VCLinkerTool;
                if (linkerTool != null)
                    break;
            }

            if (linkerTool == null)
                throw new VCQueryFailure("Couldn't file a VCLinkerTool in VC++ Project.");

            return linkerTool;
        }

        public async Task<BoolWithReason> IsCompilableFile(Document document)
        {
            try
            {
                await GetVCFileConfigForCompilation(document);
            }
            catch (VCQueryFailure queryFailure)
            {
                return new BoolWithReason()
                {
                    Result = false,
                    Reason = queryFailure.Message,
                };
            }

            return new BoolWithReason()
            {
                Result = true,
                Reason = "",
            };
        }

        public async Task CompileSingleFile(Document document)
        {
            var fileConfig = await GetVCFileConfigForCompilation(document);
            if (fileConfig != null)
                fileConfig.Compile(true, false); // WaitOnBuild==true always fails.
        }

        public string GetCompilerSetting_Includes(Project project)
        {
            VCQueryFailure queryFailure;
            try
            {
                VCCLCompilerTool compilerTool = GetToolFromActiveConfiguration<VCCLCompilerTool>(project);
                if (compilerTool != null)
                    return compilerTool.FullIncludePath;
                else
                    queryFailure = new VCQueryFailure("Unhandled error");
            }
            catch (VCQueryFailure e)
            {
                queryFailure = e;
            }

            // If querying the NMake tool fails, keep old reason for failure, since this is what we usually expect. Using NMake is seen as mere fallback.
            try
            {
                VCNMakeTool nmakeTool = GetToolFromActiveConfiguration<VCNMakeTool>(project);
                if (nmakeTool == null)
                    throw queryFailure;

                return nmakeTool.IncludeSearchPath;
            }
            catch
            {
                throw queryFailure;
            }
        }

        public void SetCompilerSetting_ShowIncludes(Project project, bool show)
        {
            GetToolFromActiveConfiguration<VCCLCompilerTool>(project).ShowIncludes = show;
        }

        public bool GetCompilerSetting_ShowIncludes(Project project)
        {
            return GetToolFromActiveConfiguration<VCCLCompilerTool>(project).ShowIncludes;
        }

        public string GetCompilerSetting_PreprocessorDefinitions(Project project)
        {
            VCQueryFailure queryFailure;
            try
            {
                VCCLCompilerTool compilerTool = GetToolFromActiveConfiguration<VCCLCompilerTool>(project);
                if (compilerTool != null)
                    return compilerTool.PreprocessorDefinitions.Replace("\\\"$(INHERIT)\\\";", "");
                else
                    queryFailure = new VCQueryFailure("Unhandled error");
            }
            catch (VCQueryFailure e)
            {
                queryFailure = e;
            }

            // If querying the NMake tool fails, keep old reason for failure, since this is what we usually expect. Using NMake is seen as mere fallback.
            try
            {
                VCNMakeTool nmakeTool = GetToolFromActiveConfiguration<VCNMakeTool>(project);
                if (nmakeTool == null)
                    throw queryFailure;

                return nmakeTool.PreprocessorDefinitions;
            }
            catch
            {
                throw queryFailure;
            }
        }

        // https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.vcprojectengine.machinetypeoption.aspx
        public enum TargetMachineType
        {
            NotSet = 0,
            X86 = 1,
            AM33 = 2,
            ARM = 3,
            EBC = 4,
            IA64 = 5,
            M32R = 6,
            MIPS = 7,
            MIPS16 = 8,
            MIPSFPU = 9,
            MIPSFPU16 = 10,
            MIPSR41XX = 11,
            SH3 = 12,
            SH3DSP = 13,
            SH4 = 14,
            SH5 = 15,
            THUMB = 16,
            AMD64 = 17
        }

        public TargetMachineType GetLinkerSetting_TargetMachine(EnvDTE.Project project)
        {
            return (TargetMachineType)GetLinkerTool(project).TargetMachine;
        }
    }
}
