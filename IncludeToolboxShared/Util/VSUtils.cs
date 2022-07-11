using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace IncludeToolbox
{
    public static class VSUtils
    {
        public static EnvDTE80.DTE2 GetDTE()
        {
            var dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
            if (dte == null)
            {
                throw new System.Exception("Failed to retrieve DTE2!");
            }
            return dte;
        }

        // Historically, the GUIDs of the COM interfaces in VCProject/VCProjectEngine would change from version to version.
        // To work around this we had several builds of VCHelpers that we could choose from, each with a different dependency.
        // With VS2019, the older versions are no longer available and we're stuck with a single version for better or worse.

        public static VCHelper VCUtils = new VCHelper();

        /// <summary>
        /// Returns what the C++ macro _MSC_VER should resolve to.
        /// </summary>
        /// <returns></returns>
        public static string GetMSCVerString()
        {
            // See http://stackoverflow.com/questions/70013/how-to-detect-if-im-compiling-code-with-visual-studio-2008
            var dte = GetDTE();
            var dteVersion = dte.Version;
            if (dte.Version.StartsWith("14."))
                return "1900";
            else if (dte.Version.StartsWith("15."))
                return "1915";
            else if (dte.Version.StartsWith("16."))
                return "1920";
            else
                throw new NotImplementedException("Unknown MSVC version!");
        }

        /// <summary>
        /// Tries to retrieve include directories from a project.
        /// For each encountered path it will try to resolve the paths to absolute paths.
        /// </summary>
        /// <returns>Empty list if include directory retrieval failed.</returns>
        public static List<string> GetProjectIncludeDirectories(EnvDTE.Project project, bool endWithSeparator = true)
        {
            List<string> pathStrings = new List<string>();
            if (project == null)
                return pathStrings;

            string projectIncludeDirectories;
            try
            {
                projectIncludeDirectories = VCUtils.GetCompilerSetting_Includes(project);
            }
            catch (VCQueryFailure e)
            {
                Output.Instance.WriteLine(e.Message); 
                return pathStrings;
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            string projectPath = Path.GetDirectoryName(Path.GetFullPath(project.FileName));

            // According to documentation FullIncludePath has resolved macros.
            
            pathStrings.AddRange(projectIncludeDirectories.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries));

            for (int i = pathStrings.Count - 1; i >= 0; --i)
            {
                try
                {
                    pathStrings[i] = pathStrings[i].Trim();
                    if (!Path.IsPathRooted(pathStrings[i]))
                    {
                        pathStrings[i] = Path.Combine(projectPath, pathStrings[i]);
                    }
                    pathStrings[i] = Utils.GetExactPathName(Path.GetFullPath(pathStrings[i]));

                    if (endWithSeparator)
                        pathStrings[i] += Path.DirectorySeparatorChar;
                }
                catch
                {
                    pathStrings.RemoveAt(i);
                }
            }
            return pathStrings;
        }

        public static IWpfTextViewHost GetCurrentTextViewHost()
        {
            IVsTextManager textManager = Package.GetGlobalService(typeof (SVsTextManager)) as IVsTextManager;

            IVsTextView textView = null;
            textManager.GetActiveView(1, null, out textView);

            var userData = textView as IVsUserData;
            if (userData == null)
            {
                return null;
            }
            else
            {
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                object holder;
                userData.GetData(ref guidViewHost, out holder);
                var viewHost = (IWpfTextViewHost) holder;

                return viewHost;
            }
        }

        public static EnvDTE.Window OpenFileAndShowDocument(string filePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (filePath == null)
                return null;

            var dte = VSUtils.GetDTE();
            EnvDTE.Window fileWindow = dte.ItemOperations.OpenFile(filePath);
            if (fileWindow == null)
            {
                Output.Instance.WriteLine("Failed to open File {0}", filePath);
                return null;
            }
            fileWindow.Activate();
            fileWindow.Visible = true;

            return fileWindow;
        }

        public static string GetOutputText()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = GetDTE();
            if (dte == null)
                return "";


            OutputWindowPane buildOutputPane = null;
            foreach (OutputWindowPane pane in dte.ToolWindows.OutputWindow.OutputWindowPanes)
            {
                if (pane.Guid == VSConstants.OutputWindowPaneGuid.BuildOutputPane_string)
                {
                    buildOutputPane = pane;
                    break;
                }
            }
            if (buildOutputPane == null)
            {
                _ = Output.Instance.ErrorMsg("Failed to query for build output pane!");
                return null;
            }
            TextSelection sel = buildOutputPane.TextDocument.Selection;

            sel.StartOfDocument(false);
            sel.EndOfDocument(true);

            return sel.Text;
        }
    }
}
