using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.IncludeWhatYouUse
{
    /// <summary>
    /// Command handler for include what you use.
    /// </summary>
    static public class IWYU
    {
        private static readonly Regex RegexRemoveLine = new Regex(@"^-\s+.+\s+\/\/ lines (\d+)-(\d+)$");

        private class FormatTask
        {
            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append("Remove:\n");
                foreach (int i in linesToRemove)
                    stringBuilder.AppendFormat("{0},", i);
                stringBuilder.Append("\nAdd:\n");
                foreach (string s in linesToAdd)
                    stringBuilder.AppendFormat("{0}\n", s);
                return stringBuilder.ToString();
            }

            public readonly HashSet<int> linesToRemove = new HashSet<int>();
            public readonly List<string> linesToAdd = new List<string>();
        };


        static private Dictionary<string, FormatTask> ParseOutput(string iwyuOutput)
        {
            Dictionary<string, FormatTask> fileTasks = new Dictionary<string, FormatTask>();
            FormatTask currentTask = null;
            bool removeCommands = true;

            //- #include <Core/Basics.h>  // lines 3-3

            // Parse what to do.
            var lines = Regex.Split(iwyuOutput, "\r\n|\r|\n");
            bool lastLineWasEmpty = false;
            foreach (string line in lines)
            {
                if (line.Length == 0)
                {
                    if (lastLineWasEmpty)
                    {
                        currentTask = null;
                    }

                    lastLineWasEmpty = true;
                    continue;
                }

                int i = line.IndexOf(" should add these lines:");
                if (i < 0)
                {
                    i = line.IndexOf(" should remove these lines:");
                    if (i >= 0)
                        removeCommands = true;
                }
                else
                {
                    removeCommands = false;
                }

                if (i >= 0)
                {
                    string file = line.Substring(0, i);

                    if (!fileTasks.TryGetValue(file, out currentTask))
                    {
                        currentTask = new FormatTask();
                        fileTasks.Add(file, currentTask);
                    }
                }
                else if (currentTask != null)
                {
                    if (removeCommands)
                    {
                        var match = RegexRemoveLine.Match(line);
                        if (match.Success)
                        {
                            int removeStart, removeEnd;
                            if (int.TryParse(match.Groups[1].Value, out removeStart) &&
                                int.TryParse(match.Groups[2].Value, out removeEnd))
                            {
                                for (int lineIdx = removeStart; lineIdx <= removeEnd; ++lineIdx)
                                    currentTask.linesToRemove.Add(lineIdx - 1);
                            }
                        }
                        else if (lastLineWasEmpty)
                        {
                            currentTask = null;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            currentTask.linesToAdd.Add(line);
                        }
                        else if (lastLineWasEmpty)
                        {
                            currentTask = null;
                        }
                    }
                }

                lastLineWasEmpty = false;
            }

            return fileTasks;
        }

        static private async Task ApplyTasks(Dictionary<string, FormatTask> tasks, bool applyFormatting, FormatterOptionsPage formatSettings)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = VSUtils.GetDTE();

            foreach (KeyValuePair<string, FormatTask> entry in tasks)
            {
                string filename = entry.Key.Replace('/', '\\'); // Classy. But Necessary.
                EnvDTE.Window fileWindow = dte.ItemOperations.OpenFile(filename);
                if (fileWindow == null)
                {
                    await Output.Instance.ErrorMsg("Failed to open File {0}", filename);
                    continue;
                }
                fileWindow.Activate();

                var viewHost = VSUtils.GetCurrentTextViewHost();
                using (var edit = viewHost.TextView.TextBuffer.CreateEdit())
                {
                    var originalLines = edit.Snapshot.Lines.ToArray();

                    // Determine which line ending to use by majority.
                    string lineEndingToBeUsed = Utils.GetDominantNewLineSeparator(edit.Snapshot.GetText());

                    // Add lines.
                    {
                        // Find last include.
                        // Will find even if commented out, but we don't care.
                        int lastIncludeLine = -1;
                        for (int line = originalLines.Length - 1; line >= 0; --line)
                        {
                            if (originalLines[line].GetText().Contains("#include"))
                            {
                                lastIncludeLine = line;
                                break;
                            }
                        }

                        // Build replacement string
                        StringBuilder stringToInsertBuilder = new StringBuilder();
                        foreach (string lineToAdd in entry.Value.linesToAdd)
                        {
                            stringToInsertBuilder.Append(lineToAdd);
                            stringToInsertBuilder.Append(lineEndingToBeUsed);
                        }
                        string stringToInsert = stringToInsertBuilder.ToString();


                        // optional, format before adding.
                        if (applyFormatting)
                        {
                            var includeDirectories = VSUtils.GetProjectIncludeDirectories(fileWindow.Document.ProjectItem?.ContainingProject);
                            stringToInsert = Formatter.IncludeFormatter.FormatIncludes(stringToInsert, fileWindow.Document.FullName, includeDirectories, formatSettings);

                            // Add a newline if we removed it.
                            if (formatSettings.RemoveEmptyLines)
                                stringToInsert += lineEndingToBeUsed;
                        }

                        // Insert.
                        int insertPosition = 0;
                        if (lastIncludeLine >= 0 && lastIncludeLine < originalLines.Length)
                        {
                            insertPosition = originalLines[lastIncludeLine].EndIncludingLineBreak;
                        }
                        edit.Insert(insertPosition, stringToInsert.ToString());
                    }

                    // Remove lines.
                    // It should safe to do that last since we added includes at the bottom, this way there is no confusion with the text snapshot.
                    {
                        foreach (int lineToRemove in entry.Value.linesToRemove.Reverse())
                        {
                            if (!Formatter.IncludeLineInfo.ContainsPreserveFlag(originalLines[lineToRemove].GetText()))
                                edit.Delete(originalLines[lineToRemove].ExtentIncludingLineBreak);
                        }
                    }

                    edit.Apply();
                }

                // For Debugging:
                //Output.Instance.WriteLine("");
                //Output.Instance.WriteLine(entry.Key);
                //Output.Instance.WriteLine(entry.Value.ToString());
            }
        }

        static public async Task Apply(string iwyuOutput, bool applyFormatter, FormatterOptionsPage formatOptions)
        {
            var tasks = ParseOutput(iwyuOutput);
            await ApplyTasks(tasks, applyFormatter, formatOptions);
        }

        /// <summary>
        /// Runs iwyu. Blocks until finished.
        /// </summary>
        static public async Task<string> RunIncludeWhatYouUse(string fullFileName, EnvDTE.Project project, IncludeWhatYouUseOptionsPage settings)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            string preprocessorDefintions;
            try
            {
                preprocessorDefintions = VSUtils.VCUtils.GetCompilerSetting_PreprocessorDefinitions(project);
            }
            catch (VCQueryFailure e)
            {
                await Output.Instance.ErrorMsg("Can't run IWYU: {0}", e.Message);
                return null;
            }

            string output = "";
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.FileName = settings.ExecutablePath;

                // Clang options
                var clangOptionList = new List<string>();
                // Disable all diagnostics
                clangOptionList.Add("-w");
                // ... despite of that "invalid token paste" comes through a lot. Disable it.
                clangOptionList.Add("-Wno-invalid-token-paste");
                // MSVC specific. See https://clang.llvm.org/docs/UsersManual.html#microsoft-extensions
                clangOptionList.Add("-fms-compatibility -fms-extensions -fdelayed-template-parsing");

                // icwyu options
                var iwyuOptionList = new List<string>();
                iwyuOptionList.Add("--verbose=" + settings.LogVerbosity);
                for (int i = 0; i < settings.MappingFiles.Length; ++i)
                    iwyuOptionList.Add("--mapping_file=\"" + settings.MappingFiles[i] + "\"");
                if (settings.NoDefaultMappings)
                    iwyuOptionList.Add("--no_default_mappings");
                if (settings.PCHInCode)
                    iwyuOptionList.Add("--pch_in_code");
                switch (settings.PrefixHeaderIncludes)
                {
                    case IncludeWhatYouUseOptionsPage.PrefixHeaderMode.Add:
                        iwyuOptionList.Add("--prefix_header_includes=add");
                        break;
                    case IncludeWhatYouUseOptionsPage.PrefixHeaderMode.Remove:
                        iwyuOptionList.Add("--prefix_header_includes=remove");
                        break;
                    case IncludeWhatYouUseOptionsPage.PrefixHeaderMode.Keep:
                        iwyuOptionList.Add("--prefix_header_includes=keep");
                        break;
                }
                if (settings.TransitiveIncludesOnly)
                    iwyuOptionList.Add("--transitive_includes_only");

                

                // Set max line length so something large so we don't loose comment information.
                // Documentation:
                // --max_line_length: maximum line length for includes. Note that this only affects comments and alignment thereof,
                // the maximum line length can still be exceeded with long file names(default: 80).
                iwyuOptionList.Add("--max_line_length=1024");

                /// write support file with includes, defines and the targetgile. Long argument lists lead to an error. Support files are the solution here.
                /// https://github.com/Wumpf/IncludeToolbox/issues/36                
                // Include-paths and Preprocessor.
                var includes = string.Join(" ", VSUtils.GetProjectIncludeDirectories(project, false).Select(x => "-I \"" + x.Replace("\\", "\\\\") + "\""));
                var defines = preprocessorDefintions.Length == 0 ? "" : string.Join(" ", preprocessorDefintions.Split(';').Select(x => "-D" + x));
                var filename = "\"" + fullFileName.Replace("\\", "\\\\") + "\"";

                var iwyuOptions = string.Join(" ", iwyuOptionList.Select(x => " -Xiwyu " + x));

                var ext = Path.GetExtension(fullFileName);

                string co = "";
                switch (settings.Commentary)
                {
                    case Commentaries.Always: co = (" -Xiwyu --update_comments"); break;
                    case Commentaries.Never: co = (" -Xiwyu --no_comments"); break;
                    case Commentaries.Default: break;
                }

                iwyuOptions += co;

                if (ext == ".h" || ext == ".hpp")
                {
                    var tmp_cpp = Path.GetTempFileName();
                    tmp_cpp = Path.ChangeExtension(tmp_cpp, ".cpp");
                    File.WriteAllText(tmp_cpp, "#include \"" + fullFileName + "\"");
                    iwyuOptions += " -Xiwyu --check_also=" + filename;
                    filename = "\"" + tmp_cpp.Replace("\\", "\\\\") + "\"";
                }
                else if (settings.HeaderPrefix != "" && Directory.Exists(Path.GetDirectoryName(fullFileName) + settings.HeaderPrefix))
                {
                    var correspond_h = settings.HeaderPrefix + '\\' + Path.GetFileNameWithoutExtension(fullFileName) + ".h";
                    var correspond_hpp = Path.ChangeExtension(correspond_h, ".hpp");
                    if (!File.Exists(correspond_h))
                        iwyuOptions += " -Xiwyu --check_also=" + "\"" + correspond_h.Replace("\\", "\\\\") + "\"";
                    else if (!File.Exists(correspond_hpp))
                        iwyuOptions += " -Xiwyu --check_also=" + "\"" + correspond_hpp.Replace("\\", "\\\\") + "\"";
                }

                var supportFilePath = Path.GetTempFileName();
                File.WriteAllText(supportFilePath, includes + " " + defines + " " + filename);

                var clangOptions = string.Join(" ", clangOptionList);
                // each include-what-you-use parameter has an -Xiwyu prefix
                process.StartInfo.Arguments = $"{clangOptions} {iwyuOptions} {settings.AdditionalParameters} \"@{supportFilePath}\"";

                Output.Instance.Write("Running command '{0}' with following arguments:\n{1}\n\n", process.StartInfo.FileName, process.StartInfo.Arguments);

                // Start the child process.
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (s, args) =>
                {
                    Output.Instance.WriteLine(args.Data);
                    output += args.Data + "\n";
                };
                process.ErrorDataReceived += (s, args) =>
                {
                    Output.Instance.WriteLine(args.Data);
                    output += args.Data + "\n";
                };
                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.CancelOutputRead();
                process.CancelErrorRead();
            }

            return output;
        }
    }
}
