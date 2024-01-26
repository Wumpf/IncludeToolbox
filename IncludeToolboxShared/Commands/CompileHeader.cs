using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System.IO;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    [Command(PackageIds.CompileHeader)]
    internal sealed class CompileHeader : BaseCommand<CompileHeader>
    {
        static string support_cpp_path = null;

        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            support_cpp_path = Path.ChangeExtension(Path.GetTempFileName(), ".cpp");
            return Task.CompletedTask;
        }

        private async Task<bool> TestCompileAsync(VCFileConfiguration config)
        {
            using AsyncDispatcher dispatcher = new();
            return await dispatcher.CompileAsync(config);
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var x = await VS.Solutions.GetActiveItemAsync();

            if ((await x.ToVCProjectItemAsync()) is not VCFile file)
                return;

            _ = Output.WriteLineAsync($"Starting Trial And Error Include removal on header file {file.FullPath}");
            string xout = "";

            var pch = VCUtil.GetPCH((VCProject)file.project);
            if (!string.IsNullOrEmpty(pch))
                xout = "#include \"" + pch + "\"\n";

            File.WriteAllText(support_cpp_path, xout + "#include \"" + file.FullPath + "\"");

            var proj = await VS.Solutions.GetActiveProjectAsync();
            var vc = await proj.ToVCProjectAsync();
            var xfile = (VCFile)vc.AddFile(support_cpp_path);
            using TempGuard tg = new(xfile);

            VCFileConfiguration config = VCUtil.GetVCFileConfig(xfile);
            if (config != null)
            {
                await TestCompileAsync(config);
                return;
            }
            _ = Output.WriteLineAsync($"{xfile.Name} has failed to yield a config.");
        }
    }
}