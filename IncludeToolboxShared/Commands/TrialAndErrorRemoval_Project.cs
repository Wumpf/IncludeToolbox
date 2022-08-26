using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.VCProjectEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Project = Community.VisualStudio.Toolkit.Project;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    internal class TAERDispatcher
    {
        public Queue<VCFile> files = new();
        public int numTotalRemovedIncludes = 0;
        readonly TrialAndErrorRemoval impl = new();


        public async Task FindFilesAsync(Project project)
        {
            var vcproj = await project.ToVCProjectAsync();
            var xfiles = (IVCCollection)vcproj.Files;

            foreach (var item in xfiles)
            {
                if (item is not VCFile file || file.FileType != eFileType.eFileTypeCppCode)
                    continue;
                files.Enqueue(file);
            }
        }

        public async Task ProcessAsync()
        {
            foreach (var item in files)
            {
                _ = Output.WriteLineAsync($"\nStarting Trial And Error Include removal on {item.FullPath}");
                string err = await impl.StartAsync(item, await TrialAndErrorRemovalOptions.GetLiveInstanceAsync());
                if (string.IsNullOrEmpty(err)) continue;
                _ = Output.WriteLineAsync(err);
            }
            _ = Output.WriteLineAsync($"\nTrial And Error Over project removed {impl.Removed} includes in total.");
        }
    }


    [Command(PackageIds.ProjectWideTrialAndErrorRemoval)]
    internal sealed class TrialAndErrorRemoval_Project : BaseCommand<TrialAndErrorRemoval_Project>
    {
        protected override Task InitializeCompletedAsync()
        {
            return Task.CompletedTask;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Has to be synchronous")]
        protected override void BeforeQueryStatus(EventArgs e)
        {
            var project = GetSelectedVCProjectAsync().Result;
            Command.Visible = project != null;
        }


        static async Task<VCProject> GetSelectedVCProjectAsync()
        {
            var selection = await VS.Solutions.GetActiveItemsAsync();
            foreach (Project item in selection.Where(i => i is Project))
            {
                var vcp = await item.ToVCProjectAsync();
                if (vcp != null) return vcp;
            }
            return null;
        }


        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            if (TrialAndErrorRemoval.WorkInProgress)
            {
                await VS.MessageBox.ShowErrorAsync("Trial and error include removal already in progress!");
                return;
            }

            var proj = await VS.Solutions.GetActiveProjectAsync();
            if (proj == null) return;

            if (!await VS.MessageBox.ShowConfirmAsync("Attention! Trial and error include removal on large projects make take up to several hours! In this time you will not be able to use Visual Studio. Are you sure you want to continue?"))
                return;

            TAERDispatcher dispatcher = new();
            await dispatcher.FindFilesAsync(proj);
            await dispatcher.ProcessAsync();
        }
    }
}
