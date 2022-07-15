using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.IncludeWhatYouUse
{
    internal class IWYU
    {
        Process process = new Process();
        string output = "";
        string command_line = "";
        string support_path = "";
        string support_cpp_path = "";

        readonly string match = "The full include-list for ";
        readonly Regex include = new("#include ([<\"].*[>\"])");
        readonly Regex include_from_file = new("#include(?:(?:\\/\\*.*\\*\\/)|[^\\S\\r\\n])*([<\\\"].*[>\\\"])");
        readonly Regex commentary = new("(?:\\/\\*(?:.|\\r|\\n)*?\\*\\/)|(?:\\/\\/.*?\\n)");


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

            switch (settings.Comms)
            {
                case Comment.Always: args.Add("--update_comments"); break;
                case Comment.Never: args.Add("--no_comments"); break;
                case Comment.Default: break;
            }
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

        /// <summary>
        /// Heavy function for include detection. 
        /// Inlcude may be inside the commentary or between multiline comms, 
        /// so the detection should be correctly defined.
        /// Still Does not count for prepro
        /// </summary>
        /// <param name="snap"></param>
        /// <returns>Dictionary of valid includes</returns>
        Dictionary<string, List<Span>> ParseIncludes(ITextSnapshot snap, out int first_pos)
        {
            first_pos = 0;
            var text = snap.GetText();
            int begin = text.IndexOf("#include");
            int end = text.LastIndexOf("#include");
            end = text.IndexOf('\n', end);
            text = text.Substring(begin, end - begin); //optimized

            // Get all commentary spans
            var comms = commentary.Matches(text).Cast<Match>().Select(s =>
            { var a = s.Captures[0]; return new Span(a.Index, a.Length); });

            // gather all the includes, that are not commented!
            var includes = include_from_file.Matches(text).Cast<Match>()
                .Where(s => !comms.Any(sp => sp.Contains(s.Captures[0].Index)))
                .Select(s =>
                {
                    var a = s.Groups[0];
                    return new KeyValuePair<string, Span>(s.Groups[1].Value, new Span(a.Index, a.Length));
                });

            if (includes.Count() != 0)
                first_pos = includes.First().Value.Start;

            var dict = new Dictionary<string, List<Span>>();
            foreach (var inc in includes)
            {
                if (!dict.ContainsKey(inc.Key))
                    dict[inc.Key] = new List<Span>();
                dict[inc.Key].Add(inc.Value);
            }

            return dict;
        }
        public async Task ApplyAsync()
        {
            if (output == "") return;

            while (true)
            {
                int pos = output.IndexOf(match);
                if (pos == -1) return;

                var tasks = output.Substring(0, pos)
                    .Split('\n').Select(l => l.Trim());

                pos = pos + match.Length;
                string part = output.Substring(pos);
                string path = part.Substring(0, part.IndexOf(':', 3));
                var doc = await VS.Documents.OpenAsync(path);
                var edit = doc.TextBuffer.CreateEdit();

                var dict = ParseIncludes(doc.TextBuffer.CurrentSnapshot, out int start);


                foreach (var task in tasks)
                {
                    if (task.StartsWith("- #include"))
                    {
                        var rem = include.Match(task).Groups[1].Value;
                        edit.Delete(dict[rem].First());
                        dict[rem].RemoveAt(0);
                    }
                    if (task.StartsWith("#include"))
                    {
                        edit.Insert(start, task);
                    }
                }
                edit.Apply();
                output = part.Substring(part.IndexOf("---"));
            }
        }

        public async Task<bool> StartAsync(string file, bool rebuild)
        {
            output = "";
            var cmd = await VCUtil.GetCommandLineAsync(rebuild);
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
