using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
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
        readonly Process process = new();
        string output = "";
        string command_line = "";
        readonly string support_path = "";
        readonly string support_cpp_path = "";

        public string ProcOutput { get => output; }

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

            // initialize temp files (can be multithreaded, hence instance based)
            support_cpp_path = Path.ChangeExtension(Path.GetTempFileName(), ".cpp");
            support_path = Path.GetTempFileName();
        }

        public void BuildCommandLine(IWYUOptions settings)
        {
            process.StartInfo.FileName = settings.Executable;

            List<string> args = new()
            {
                string.Format("--verbose={0}", settings.Verbosity)
            };

            if (settings.Precompiled || settings.IgnoreHeader)
                args.Add("--pch_in_code");
            if (settings.Transitives)
                args.Add("--transitive_includes_only");
            if (settings.NoDefault)
                args.Add("--no_default_mappings");
            if (settings.UseProvided)
            {
                var path = IWYUDownload.GetDefaultMappingChecked();
                if (!string.IsNullOrEmpty(path))
                    args.Add(string.Format("--mapping_file=\"{0}\"", path));
            }
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
        }

        static public void MoveHeader(DocumentView view)
        {
            var buf = view.TextBuffer;
            var str = buf.CurrentSnapshot.GetText();
            var span = Utils.GetIncludeSpan(str);

            Regex regex = new($"#include\\s[<\"]([\\w\\\\\\/\\.]+{Path.GetFileNameWithoutExtension(view.FilePath)}.h(?:pp|xx)?)[>\"]");
            var match = regex.Match(str, span.Start, span.Length);
            if (!match.Success) return;
            var edit = buf.CreateEdit();
            _ = edit.Delete(new(match.Index, match.Length));

            edit.Insert(span.Start, match.Value + Utils.GetLineBreak(view.TextView));
            edit.Apply();
        }



        public async Task<bool> StartAsync(string file, Project proj, bool rebuild)
        {
            var cmd = await VCUtil.GetCommandLineAsync(rebuild, proj);
            if (string.IsNullOrEmpty(cmd))
            {
                Output.WriteLineAsync("Failed to gather command line for c++ project").FireAndForget();
                return false;
            }
            return StartImpl(file, cmd);
        }
        public async Task<bool> StartAsync(string file, bool rebuild)
        {
            var cmd = await VCUtil.GetCommandLineAsync(rebuild);
            if (string.IsNullOrEmpty(cmd))
            {
                Output.WriteLineAsync("Failed to gather command line for c++ project").FireAndForget();
                return false;
            }
            return StartImpl(file, cmd);
        }

        bool StartImpl(string file, string cmd)
        {
            output = "";
            File.WriteAllText(support_path, cmd);

            var ext = Path.GetExtension(file);
            if (ext == ".h" || ext == ".hpp" || ext == ".hxx")
            {
                File.WriteAllText(support_cpp_path, "#include \"" + file + "\"");
                file = " -Xiwyu --check_also=" + "\"" + file + "\"";
                file += " \"" + support_cpp_path.Replace("\\", "\\\\") + "\"";
            }
            else
            {
                file = "\"" + file + "\"";
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
            Output.WriteLineAsync($"IWYU Process {process.ProcessName} was cancelled.").FireAndForget();
        }
    }
}
