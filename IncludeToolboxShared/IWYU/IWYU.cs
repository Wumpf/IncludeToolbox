using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.IncludeWhatYouUse
{
    internal class IWYU
    {
        Process process = new();
        string output = "";
        string command_line = "";
        string support_path = "";
        string support_cpp_path = "";

        static readonly string match = "The full include-list for ";


        public IWYU()
        {
            process.EnableRaisingEvents = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += (s, args) =>
            {
                output += args.Data + "\n";
            };
            process.ErrorDataReceived += (s, args) =>
            {
                output += args.Data + "\n";
            };
        }

        public void BuildCommandLine(IWYUOptions settings)
        {
            process.StartInfo.FileName = settings.Executable;

            List<string> args = new();
            args.Add(string.Format("--verbose={0}", settings.Verbosity));

            if (settings.Precompiled || settings.IgnoreHeader)
                args.Add("--pch_in_code");
            if (settings.Transitives)
                args.Add("--transitive_includes_only");
            if (settings.NoDefault)
                args.Add("--no_default_mappings");
            if (settings.MappingFile != "")
                args.Add(string.Format("--mapping_file=\"{0}\"", settings.MappingFile));
            args.Add("--max_line_length=256"); // output line for commentaries

            command_line =
                string.Join(" ", args.Select(x => " -Xiwyu " + x));

            if (!settings.Warnings)
                command_line += " -w";

            command_line += " -Wno-invalid-token-paste -fms-compatibility -fms-extensions -fdelayed-template-parsing";
            if (settings.ClangOptions != null && settings.ClangOptions?.Count() != 0)
                command_line += " " + string.Join(" ", settings.ClangOptions);
            if (settings.Options != null && settings.Options.Count() != 0)
                command_line += " " + string.Join(" ", settings.Options.Select(x => " -Xiwyu " + x));
            settings.ClearFlag();
        }

        public string CommandLine { get => command_line; set => command_line = value; }

        async Task FormatAsync(DocumentView doc)
        {
            using var xedit = doc.TextBuffer.CreateEdit();
            var text = xedit.Snapshot.GetText();
            var span = IWYUApply.GetIncludeSpan(text);
            var result = await IWYUApply.PreformatAsync(new SnapshotSpan(xedit.Snapshot, span).GetText(), doc.FilePath);
            xedit.Replace(span, result);
            xedit.Apply();
        }
        public async Task ApplyAsync(IWYUOptions settings)
        {
            if (output == "") return;

            while (true)
            {
                int pos = output.IndexOf(match);
                if (pos == -1) return;

                pos += match.Length;
                string part = output.Substring(pos);

                int endp = part.IndexOf("---");
                string path = part.Substring(0, part.IndexOf(':', 3));
                var doc = await VS.Documents.OpenAsync(path);
                using var edit = doc.TextBuffer.CreateEdit();


                if (settings.Sub == Substitution.Cheap)
                {
                    int endl = part.IndexOf("\n");
                    string result = part.Substring(endl, endp - endl);
                    IWYUApply.ApplyCheap(edit, 
                        result, 
                        settings.Comms != Comment.No);
                }
                else
                {
                    var tasks = output.Substring(0, pos)
                    .Split('\n').Select(l => l.Trim());

                    IWYUApply.ParseTasks(tasks, settings.Comms != Comment.No).Apply(edit);
                }
                edit.Apply();

                if (settings.Format) await FormatAsync(doc);
                output = part.Substring(endp);
            }
        }

        public async Task<bool> StartAsync(string file, Project proj, bool rebuild, string additional = "")
        {
            var cmd = await VCUtil.GetCommandLineAsync(rebuild, proj);
            if (!string.IsNullOrEmpty(support_path))
                cmd += ' ' + additional;
            return StartImpl(file, rebuild, cmd);
        }

        public async Task<bool> StartAsync(string file, bool rebuild, string additional = "")
        {
            var cmd = await VCUtil.GetCommandLineAsync(rebuild);
            if (!string.IsNullOrEmpty(support_path))
                cmd += ' ' + additional;
            return StartImpl(file, rebuild, cmd);
        }

        bool StartImpl(string file, bool rebuild, string cmd)
        {
            output = "";
            if (cmd == null) return false;
            if (cmd != "")
            {
                // cache file for reuse
                support_path = string.IsNullOrEmpty(support_path) ? Path.GetTempFileName() : support_path;
                File.WriteAllText(support_path, cmd);
            }
            var ext = Path.GetExtension(file);
            if (ext == ".h" || ext == ".hpp")
            {
                if (support_cpp_path == "") { support_cpp_path = Path.ChangeExtension(Path.GetTempFileName(), ".cpp"); }
                File.WriteAllText(support_cpp_path, "#include \"" + file + "\"");
                file = " -Xiwyu --check_also=" + file;
                file += " \"" + support_cpp_path.Replace("\\", "\\\\") + "\"";
            }
            process.StartInfo.Arguments = $"{command_line} \"@{support_path}\" {file}";

            Output.WriteLineAsync(string.Format("Running command '{0}' with following arguments:\n{1}\n\n", process.StartInfo.FileName, process.StartInfo.Arguments)).FireAndForget();

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            process.CancelErrorRead();

            Output.WriteLineAsync(output).FireAndForget();
            return true;
        }
        public async Task CancelAsync()
        {
            await Task.Run(delegate { process.Kill(); });
        }
    }
}
