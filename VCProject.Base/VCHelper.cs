using EnvDTE;
using Microsoft.VisualStudio.VCProjectEngine;
using VCProjectUtils.Base;

#if VC14
namespace VCProjectUtils.VS14
#elif VC15
namespace VCProjectUtils.VS15
#elif VC16
namespace VCProjectUtils.VS16
#endif
{
    public class VCHelper : IVCHelper
    {
        public bool IsVCProject(Project project)
        {
            return project?.Object is VCProject;
        }

        private static VCFileConfiguration GetVCFileConfigForCompilation(Document document, out string reasonForFailure)
        {
            if (document == null)
            {
                reasonForFailure = "No document.";
                return null;
            }

            var vcProject = document.ProjectItem.ContainingProject?.Object as VCProject;
            if (vcProject == null)
            {
                reasonForFailure = "The given document does not belong to a VC++ Project.";
                return null;
            }

            VCFile vcFile = document.ProjectItem?.Object as VCFile;
            if (vcFile == null)
            {
                reasonForFailure = "The given document is not a VC++ file.";
                return null;
            }

            if (vcFile.FileType != eFileType.eFileTypeCppCode)
            {
                reasonForFailure = "The given document is not a compileable VC++ file.";
                return null;
            }

            IVCCollection fileConfigCollection = vcFile.FileConfigurations as IVCCollection;
            VCFileConfiguration fileConfig = fileConfigCollection?.Item(vcProject.ActiveConfiguration.Name) as VCFileConfiguration;
            if (fileConfig == null)
            {
                reasonForFailure = "Failed to retrieve file config from document.";
                return null;
            }

            reasonForFailure = "";
            return fileConfig;
        }

        private static VCTool GetToolFromActiveConfiguration<VCTool>(Project project, out string reasonForFailure) where VCTool: class
        {
            VCProject vcProject = project?.Object as VCProject;
            if (vcProject == null)
            {
                reasonForFailure = $"Failed to retrieve VCCLCompilerTool since project \"{project.Name}\" is not a VCProject.";
                return null;
            }
            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            VCTool compilerTool = null;
            foreach (var tool in activeConfiguration.Tools)
            {
                compilerTool = tool as VCTool;
                if (compilerTool != null)
                    break;
            }

            if (compilerTool == null)
            {
                reasonForFailure = $"Couldn't find a {typeof(VCTool).Name} in active configuration of VC++ Project \"{vcProject.Name}\"";
                return null;
            }

            reasonForFailure = "";
            return compilerTool;
        }

        public static VCLinkerTool GetLinkerTool(Project project, out string reasonForFailure)
        {
            VCProject vcProject = project?.Object as VCProject;
            if (vcProject == null)
            {
                reasonForFailure = "Failed to retrieve VCLinkerTool since project is not a VCProject.";
                return null;
            }
            VCConfiguration activeConfiguration = vcProject.ActiveConfiguration;
            var tools = activeConfiguration.Tools;
            VCLinkerTool linkerTool = null;
            foreach (var tool in activeConfiguration.Tools)
            {
                linkerTool = tool as VCLinkerTool;
                if (linkerTool != null)
                    break;
            }

            if (linkerTool == null)
            {
                reasonForFailure = "Couldn't file a VCLinkerTool in VC++ Project.";
                return null;
            }

            reasonForFailure = "";
            return linkerTool;
        }

        public bool IsCompilableFile(Document document, out string reasonForFailure)
        {
            return GetVCFileConfigForCompilation(document, out reasonForFailure) != null;
        }

        public void CompileSingleFile(Document document)
        {
            string reasonForFailure;
            var fileConfig = GetVCFileConfigForCompilation(document, out reasonForFailure);
            if(fileConfig != null)
            {
                fileConfig.Compile(true, false); // WaitOnBuild==true always fails.
            }
        }

        public string GetCompilerSetting_Includes(Project project, out string reasonForFailure)
        {
            VCCLCompilerTool compilerTool = GetToolFromActiveConfiguration<VCCLCompilerTool>(project, out reasonForFailure);
            if (compilerTool != null)
                return compilerTool.FullIncludePath;

            // If querying the NMake tool fails, keep old reason for failure, since this is what we usually expect. Using NMake is seen as mere fallback.
            VCNMakeTool nmakeTool = GetToolFromActiveConfiguration<VCNMakeTool>(project, out var _);
            if (nmakeTool == null) return null;
            reasonForFailure = "";
            return nmakeTool.IncludeSearchPath;
        }

        public void SetCompilerSetting_ShowIncludes(Project project, bool show, out string reasonForFailure)
        {
            VCCLCompilerTool compilerTool = GetToolFromActiveConfiguration<VCCLCompilerTool>(project, out reasonForFailure);
            if(compilerTool != null)
                compilerTool.ShowIncludes = show;
        }

        public bool? GetCompilerSetting_ShowIncludes(Project project, out string reasonForFailure)
        {
            VCCLCompilerTool compilerTool = GetToolFromActiveConfiguration<VCCLCompilerTool>(project, out reasonForFailure);
            return compilerTool?.ShowIncludes;
        }

        public string GetCompilerSetting_PreprocessorDefinitions(Project project, out string reasonForFailure)
        {
            VCCLCompilerTool compilerTool = GetToolFromActiveConfiguration<VCCLCompilerTool>(project, out reasonForFailure);
            if (compilerTool != null)
                return compilerTool?.PreprocessorDefinitions;

            // If querying the NMake tool fails, keep old reason for failure, since this is what we usually expect. Using NMake is seen as mere fallback.
            VCNMakeTool nmakeTool = GetToolFromActiveConfiguration<VCNMakeTool>(project, out var _);
            if (nmakeTool == null) return null;
            reasonForFailure = "";
            return nmakeTool.IncludeSearchPath;
        }

        public TargetMachineType? GetLinkerSetting_TargetMachine(EnvDTE.Project project, out string reasonForFailure)
        {
            var linkerTool = GetLinkerTool(project, out reasonForFailure);
            if (linkerTool == null)
                return null;
            else
                return (TargetMachineType)linkerTool.TargetMachine;
        }
    }
}
