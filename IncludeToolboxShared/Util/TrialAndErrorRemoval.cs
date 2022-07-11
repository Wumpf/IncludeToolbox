using System;
using System.Linq;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    /// <summary>
    /// Command handler for trial and error include removal
    /// </summary>
    internal sealed class TrialAndErrorRemoval
    {
        public delegate void FinishedEvent(int numRemovedIncludes, bool canceled);
        public event FinishedEvent OnFileFinished;

        public static bool WorkInProgress { get; private set; }

        private volatile bool lastBuildSuccessful;
        private AutoResetEvent outputWaitEvent = new AutoResetEvent(false);
        private const int timeoutMS = 600000; // 600 seconds, 10 minutes per file

        /// <summary>
        /// Need to keep build events object around as long as it is used, otherwise the events may not be fired!
        /// </summary>
        private BuildEvents buildEvents;

        public async Task<bool> PerformTrialAndErrorIncludeRemoval(EnvDTE.Document document, TrialAndErrorRemovalOptionsPage settings)
        {
            if (document == null)
                return false;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var canCompile = await VSUtils.VCUtils.IsCompilableFile(document);
            if (canCompile.Result == false)
            {
                Output.Instance.WriteLine($"Can't compile file '{canCompile.Reason}': {document.Name}");
                return false;
            }

            if (WorkInProgress)
            {
                _ = Output.Instance.ErrorMsg("Trial and error include removal already in progress!");
                return false;
            }
            WorkInProgress = true;

            // Start wait dialog.
            IVsThreadedWaitDialog2 progressDialog = await StartProgressDialog(document.Name);
            if (progressDialog == null)
                return false;

            // Extract all includes.
            ITextBuffer textBuffer;
            Formatter.IncludeLineInfo[] includeLines;
            try
            {
                ExtractSelectionAndIncludes(document, settings, out textBuffer, out includeLines);
            }
            catch (Exception ex)
            {
                Output.Instance.WriteLine("Unexpected error while extracting include selection: {0}", ex);
                progressDialog.EndWaitDialog();
                return false;
            }

            // Hook into build events.
            SubscribeBuildEvents();

            // The rest runs in a separate thread since the compile function is non blocking and we want to use BuildEvents
            // We are not using Task, since we want to make use of WaitHandles - using this together with Task is a bit more complicated to get right.
            outputWaitEvent.Reset();
            var removalThread = new System.Threading.Thread(() => TrialAndErrorRemovalThreadFunc(document, settings, includeLines, progressDialog, textBuffer));
            removalThread.Start();
            return true;
        }

        private async Task<IVsThreadedWaitDialog2> StartProgressDialog(string documentName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dialogFactory = Package.GetGlobalService(typeof(SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
            if (dialogFactory == null)
            {
                Output.Instance.WriteLine("Failed to get Dialog Factory for wait dialog.");
                return null;
            }

            IVsThreadedWaitDialog2 progressDialog;
            dialogFactory.CreateInstance(out progressDialog);
            if (progressDialog == null)
            {
                Output.Instance.WriteLine("Failed to get create wait dialog.");
                return null;
            }
            string waitMessage = $"Parsing '{documentName}' ... ";
            progressDialog.StartWaitDialogWithPercentageProgress(
                szWaitCaption: "Include Toolbox - Running Trial & Error Include Removal",
                                szWaitMessage: waitMessage,
                                szProgressText: null,
                                varStatusBmpAnim: null,
                                szStatusBarText: "Running Trial & Error Removal - " + waitMessage,
                                fIsCancelable: true,
                                iDelayToShowDialog: 0,
                                iTotalSteps: 20,    // Will be replaced.
                                iCurrentStep: 0);

            return progressDialog;
        }

        private void ExtractSelectionAndIncludes(EnvDTE.Document document, TrialAndErrorRemovalOptionsPage settings,
                                                out ITextBuffer textBuffer, out Formatter.IncludeLineInfo[] includeLinesArray)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Parsing.
            document.Activate();
            var documentTextView = VSUtils.GetCurrentTextViewHost();
            textBuffer = documentTextView.TextView.TextBuffer;
            string documentText = documentTextView.TextView.TextSnapshot.GetText();
            IEnumerable<Formatter.IncludeLineInfo> includeLines = Formatter.IncludeLineInfo.ParseIncludes(documentText, Formatter.ParseOptions.IgnoreIncludesInPreprocessorConditionals | Formatter.ParseOptions.KeepOnlyValidIncludes);

            // Optionally skip top most include.
            if (settings.IgnoreFirstInclude)
                includeLines = includeLines.Skip(1);

            // Skip everything with preserve flag.
            includeLines = includeLines.Where(x => !x.ShouldBePreserved);

            // Apply filter ignore regex.
            {
                string documentName = Path.GetFileNameWithoutExtension(document.FullName);
                string[] ignoreRegexList = RegexUtils.FixupRegexes(settings.IgnoreList, documentName);
                includeLines = includeLines.Where(line => !ignoreRegexList.Any(regexPattern =>
                                                                     new System.Text.RegularExpressions.Regex(regexPattern).Match(line.IncludeContent).Success));
            }
            // Reverse order if necessary.
            if (settings.RemovalOrder == TrialAndErrorRemovalOptionsPage.IncludeRemovalOrder.BottomToTop)
                includeLines = includeLines.Reverse();

            includeLinesArray = includeLines.ToArray();
        }

        private void TrialAndErrorRemovalThreadFunc(EnvDTE.Document document, TrialAndErrorRemovalOptionsPage settings,
                                                    Formatter.IncludeLineInfo[] includeLines, IVsThreadedWaitDialog2 progressDialog, ITextBuffer textBuffer)
        {
            int numRemovedIncludes = 0;
            bool canceled = false;

            try
            {
                int currentProgressStep = 0;

                // For ever include line..
                foreach (Formatter.IncludeLineInfo line in includeLines)
                {
                    // If we are working from top to bottom, the line number may have changed!
                    int currentLine = line.LineNumber;
                    if (settings.RemovalOrder == TrialAndErrorRemovalOptionsPage.IncludeRemovalOrder.TopToBottom)
                        currentLine -= numRemovedIncludes;

                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        // Update progress.
                        string waitMessage = $"Removing #includes from '{document.Name}'";
                        string progressText = $"Trying to remove '{line.IncludeContent}' ...";
                        progressDialog.UpdateProgress(
                            szUpdatedWaitMessage: waitMessage,
                            szProgressText: progressText,
                            szStatusBarText: "Running Trial & Error Removal - " + waitMessage + " - " + progressText,
                            iCurrentStep: currentProgressStep + 1,
                            iTotalSteps: includeLines.Length + 1,
                            fDisableCancel: false,
                            pfCanceled: out canceled);
                        if (!canceled)
                        { 
                            ++currentProgressStep;

                            // Remove include
                            using (var edit = textBuffer.CreateEdit())
                            {
                                if (settings.KeepLineBreaks)
                                    edit.Delete(edit.Snapshot.Lines.ElementAt(currentLine).Extent);
                                else
                                    edit.Delete(edit.Snapshot.Lines.ElementAt(currentLine).ExtentIncludingLineBreak);
                                edit.Apply();
                            }
                        }
                        outputWaitEvent.Set();
                    });
                    outputWaitEvent.WaitOne();

                    if (canceled)
                        break;

                    // Compile - In rare cases VS tells us that we are still building which should not be possible because we have received OnBuildFinished
                    // As a workaround we just try again a few times.
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        const int maxNumCompileAttempts = 3;
                        for (int numCompileFails = 0; numCompileFails < maxNumCompileAttempts; ++numCompileFails)
                        {
                            // TODO: This happens on the main thread. Making the whole thread thing a bit pointless!!!
                            try
                            {
                                await VSUtils.VCUtils.CompileSingleFile(document);
                            }
                            catch (Exception e)
                            {
                                Output.Instance.WriteLine("Compile Failed:\n{0}", e);

                                if (numCompileFails == maxNumCompileAttempts - 1)
                                {
                                    document.Undo();
                                    throw e;
                                }
                                else
                                {
                                    // Try again.
                                    await System.Threading.Tasks.Task.Delay(100);
                                    continue;
                                }
                            }
                            break;
                        }
                    });

                    // Wait till woken.
                    bool noTimeout = outputWaitEvent.WaitOne(timeoutMS);

                    // Undo removal if compilation failed.
                    if (!noTimeout || !lastBuildSuccessful)
                    {
                        Output.Instance.WriteLine("Could not remove #include: '{0}'", line.IncludeContent);
                        ThreadHelper.JoinableTaskFactory.Run(async () =>
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            document.Undo();
                            if (!noTimeout)
                                await Output.Instance.ErrorMsg("Compilation of {0} timeouted!", document.Name);
                        });

                        if (!noTimeout)
                            break;
                    }
                    else
                    {
                        Output.Instance.WriteLine("Successfully removed #include: '{0}'", line.IncludeContent);
                        ++numRemovedIncludes;
                    }
                }
            }
            catch (Exception ex)
            {
                Output.Instance.WriteLine("Unexpected error: {0}", ex);
            }
            finally
            {
                _ = OnTrialAndErrorRemovalDone(progressDialog, document, numRemovedIncludes, canceled);
            }
        }

        private async Task OnTrialAndErrorRemovalDone(IVsThreadedWaitDialog2 progressDialog, EnvDTE.Document document, int numRemovedIncludes, bool canceled)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Close Progress bar.
            progressDialog.EndWaitDialog();

            // Remove build hook again.
            UnsubscribeBuildEvents();

            // Message.
            Output.Instance.WriteLine("Removed {0} #include directives from '{1}'", numRemovedIncludes, document.Name);
            Output.Instance.OutputToForeground();

            // Notify that we are done.
            WorkInProgress = false;
            OnFileFinished?.Invoke(numRemovedIncludes, canceled);
        }

        private void SubscribeBuildEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            buildEvents = VSUtils.GetDTE().Events.BuildEvents;
            buildEvents.OnBuildDone += OnBuildFinished;
            buildEvents.OnBuildProjConfigDone += OnBuildConfigFinished;
        }

        private void UnsubscribeBuildEvents()
        {
            buildEvents.OnBuildDone -= OnBuildFinished;
            buildEvents.OnBuildProjConfigDone -= OnBuildConfigFinished;
            buildEvents = null;
        }

        private void OnBuildFinished(vsBuildScope scope, vsBuildAction action)
        {
            //Output.Instance.WriteLine("OnBuildFinished. scope: {0}, action: {1}", scope, action);
            outputWaitEvent.Set();
        }

        private void OnBuildConfigFinished(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            //Output.Instance.WriteLine("OnBuildConfigFinished. project {0}, projectConfig {1}, platform {2}, solutionConfig {3}, success {4}", project, projectConfig, platform, solutionConfig, success);
            lastBuildSuccessful = success;
        }
    }
}