using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Graph
{
    public static class CompilationBasedGraphParser
    {
        public delegate Task OnCompleteCallback(IncludeGraph graph, Document document, bool success);

        // There can always be only one compilation operation and it takes a while.
        // This makes the whole mechanism effectively a singletonish thing.
        private static bool CompilationOngoing { get { return documentBeingCompiled != null; } }
        private static bool showIncludeSettingBefore = false;
        private static OnCompleteCallback onCompleted;
        private static Document documentBeingCompiled;
        private static IncludeGraph graphBeingExtended;


        public static async Task<BoolWithReason> CanPerformShowIncludeCompilation(Document document)
        {
            if (CompilationOngoing)
            {
                return new BoolWithReason
                { 
                    Result = false,
                    Reason = "Can't compile while another file is being compiled.",
                };
            }

            var dte = VSUtils.GetDTE();
            if (dte == null)
            {
                return new BoolWithReason
                {
                    Result = false,
                    Reason = "Failed to acquire dte object.",
                };
            }

            var result = await VSUtils.VCUtils.IsCompilableFile(document);
            if (result.Result == false)
            {
                result.Reason = $"Can't extract include graph since current file '{document?.FullName ?? "<no file>"}' can't be compiled: {result.Reason}.";
                return result;
            }

            return new BoolWithReason { Result = true, Reason = "" };
         }

        /// <summary>
        /// Parses a given source file using cl.exe with the /showIncludes option and adds the output to the original graph.
        /// </summary>
        /// <remarks>
        /// If this is the first file, the graph is necessarily a tree after this operation.
        /// </remarks>
        /// <returns>true if successful, false otherwise.</returns>
        public static async Task<bool> AddIncludesRecursively_ShowIncludesCompilation(this IncludeGraph graph, Document document, OnCompleteCallback onCompleted)
        {
            var canPerformShowIncludeCompilation = await CanPerformShowIncludeCompilation(document);
            if (!canPerformShowIncludeCompilation.Result)
            {
                await Output.Instance.ErrorMsg(canPerformShowIncludeCompilation.Reason);
                return false;
            }

            try
            {
                var dte = VSUtils.GetDTE();
                if (dte == null)
                {
                    await Output.Instance.ErrorMsg("Failed to acquire dte object.");
                    return false;
                }

                try
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    showIncludeSettingBefore = VSUtils.VCUtils.GetCompilerSetting_ShowIncludes(document.ProjectItem?.ContainingProject);
                    VSUtils.VCUtils.SetCompilerSetting_ShowIncludes(document.ProjectItem?.ContainingProject, true);
                }
                catch (VCQueryFailure queryFailure)
                {
                    await Output.Instance.ErrorMsg("Can't compile with show includes: {0}.", queryFailure.Message);
                    return false;
                }

                // Only after we're through all early out error cases, set static compilation infos.
                dte.Events.BuildEvents.OnBuildDone += OnBuildConfigFinished;
                CompilationBasedGraphParser.onCompleted = onCompleted;
                CompilationBasedGraphParser.documentBeingCompiled = document;
                CompilationBasedGraphParser.graphBeingExtended = graph;

                // Even with having the config changed and having compile force==true, we still need to make a dummy change in order to enforce recompilation of this file.
                {
                    document.Activate();
                    var documentTextView = VSUtils.GetCurrentTextViewHost();
                    var textBuffer = documentTextView.TextView.TextBuffer;
                    using (var edit = textBuffer.CreateEdit())
                    {
                        edit.Insert(0, " ");
                        edit.Apply();
                    }
                    using (var edit = textBuffer.CreateEdit())
                    {
                        edit.Replace(new Microsoft.VisualStudio.Text.Span(0, 1), "");
                        edit.Apply();
                    }
                }

                await VSUtils.VCUtils.CompileSingleFile(document);
            }
            catch(Exception e)
            {
                await ResetPendingCompilationInfo();
                await Output.Instance.ErrorMsg("Compilation of file '{0}' with /showIncludes failed: {1}.", document.FullName, e);
                return false;
            }

            return true;
        }

        private static async Task ResetPendingCompilationInfo()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                VSUtils.VCUtils.SetCompilerSetting_ShowIncludes(documentBeingCompiled.ProjectItem?.ContainingProject, showIncludeSettingBefore);
            }
            catch (VCQueryFailure) { }

            onCompleted = null;
            documentBeingCompiled = null;
            graphBeingExtended = null;

            VSUtils.GetDTE().Events.BuildEvents.OnBuildDone -= OnBuildConfigFinished;
        }

        private static void OnBuildConfigFinished(vsBuildScope Scope, vsBuildAction Action)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Sometimes we get this message several times.
            if (!CompilationOngoing)
                return;

            // Parsing maybe successful for an unsuccessful build!
            bool successfulParsing = true;
            try
            {
                string outputText = VSUtils.GetOutputText();
                if (string.IsNullOrEmpty(outputText))
                {
                    successfulParsing = false;
                    return;
                }

                // What we're building right now is a tree.
                // However, combined with the existing data it might be a wide graph.
                var includeTreeItemStack = new Stack<IncludeGraph.GraphItem>();
                includeTreeItemStack.Push(graphBeingExtended.CreateOrGetItem(documentBeingCompiled.FullName, out _));

                var includeDirectories = VSUtils.GetProjectIncludeDirectories(documentBeingCompiled.ProjectItem.ContainingProject);
                includeDirectories.Insert(0, PathUtil.Normalize(documentBeingCompiled.Path) + Path.DirectorySeparatorChar);

                const string includeNoteString = "Note: including file: ";
                string[] outputLines = System.Text.RegularExpressions.Regex.Split(outputText, "\r\n|\r|\n"); // yes there are actually \r\n in there in some VS versions.
                foreach (string line in outputLines)
                {
                    int startIndex = line.IndexOf(includeNoteString);
                    if (startIndex < 0)
                        continue;
                    startIndex += includeNoteString.Length;

                    int includeStartIndex = startIndex;
                    while (includeStartIndex < line.Length && line[includeStartIndex] == ' ')
                        ++includeStartIndex;
                    int depth = includeStartIndex - startIndex;

                    if (depth >= includeTreeItemStack.Count)
                    {
                        includeTreeItemStack.Push(includeTreeItemStack.Peek().Includes.Last().IncludedFile);
                    }
                    while (depth < includeTreeItemStack.Count - 1)
                        includeTreeItemStack.Pop();

                    string fullIncludePath = line.Substring(includeStartIndex);
                    IncludeGraph.GraphItem includedItem = graphBeingExtended.CreateOrGetItem(fullIncludePath, out _);
                    includeTreeItemStack.Peek().Includes.Add(new IncludeGraph.Include() { IncludedFile = includedItem });
                }
            }

            catch (Exception e)
            {
                _ = Output.Instance.ErrorMsg("Failed to parse output from /showInclude compilation of file '{0}': {1}", documentBeingCompiled.FullName, e);
                successfulParsing = false;
                return;
            }
            finally
            {
                _ = onCompleted(graphBeingExtended, documentBeingCompiled, successfulParsing);
                _ = ResetPendingCompilationInfo();
            }
        }
    }
}
