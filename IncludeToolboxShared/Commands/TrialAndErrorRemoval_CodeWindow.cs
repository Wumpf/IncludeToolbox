using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    internal sealed class TempGuard : IDisposable
    {
        readonly VCFile file;
        private bool disposedValue = false;

        private void Dispose()
        {
            if (!disposedValue)
            {
                file.Remove();
                disposedValue = true;
            }
        }
        public TempGuard(VCFile file)
        {
            this.file = file;
        }

        ~TempGuard()
        {
            Dispose();
        }

        void IDisposable.Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }
    }


    [Command(PackageIds.TrialAndError)]
    internal sealed class TrialAndErrorRemoval_CodeWindow : BaseCommand<TrialAndErrorRemoval_CodeWindow>
    {
        TrialAndErrorRemoval impl = new();
        static string support_cpp_path = null;

        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            support_cpp_path = Path.ChangeExtension(Path.GetTempFileName(), ".cpp");
            return Task.CompletedTask;
        }

        async Task<string> TAERHeaderAsync(VCFile file)
        {
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

            return await impl.StartHeaderAsync(file, xfile, await TrialAndErrorRemovalOptions.GetLiveInstanceAsync());
        }
        async Task<string> TAERCodeAsync(VCFile file)
        {
            _ = Output.WriteLineAsync($"Starting Trial And Error Include removal on {file.FullPath}");
            return await impl.StartAsync(file, await TrialAndErrorRemovalOptions.GetLiveInstanceAsync());
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var x = await VS.Solutions.GetActiveItemAsync();

            if ((await x.ToVCProjectItemAsync()) is not VCFile file)
                return;
            string err = "";

            switch (file.FileType)
            {
                case eFileType.eFileTypeCppCode:
                    err = await TAERCodeAsync(file);
                    break;
                case eFileType.eFileTypeCppHeader:
                    err = await TAERHeaderAsync(file);
                    break;
                default:
                    break;
            }

            if (string.IsNullOrEmpty(err)) return;
            _ = Output.WriteLineAsync(err);
        }
    }
}