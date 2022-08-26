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
        public Queue<SolutionItem> files = new();
        public int numTotalRemovedIncludes = 0;
        readonly TrialAndErrorRemoval impl = new();


        public void FindFiles(SolutionItem project)
        {
            foreach (var item in project.Children)
            {
                switch (item.Type)
                {
                    case SolutionItemType.PhysicalFile:
                        files.Enqueue(item);
                        break;
                    case SolutionItemType.VirtualFolder:
                    case SolutionItemType.PhysicalFolder:
                        FindFiles(item);
                        break;
                    default:
                        break;
                }
            }
        }

        public async Task ProcessAsync()
        {
            foreach (PhysicalFile item in files.Where(s=>((VCFile)s.ToVCProjectItemAsync()).FileType == eFileType.eFileTypeCppCode).Cast<PhysicalFile>())
            {
                _ = Output.WriteLineAsync($"\nStarting Trial And Error Include removal on {item.FullPath}");
                string err = await impl.StartAsync(item, await TrialAndErrorRemovalOptions.GetLiveInstanceAsync());
                if (string.IsNullOrEmpty(err)) continue;
                _ = Output.WriteLineAsync(err);
            }
            _ = Output.WriteLineAsync($"\nTrial And Error Over project removed {impl.Removed} includes in total.");
        }
    }

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
            dispatcher.FindFiles(proj);
            await dispatcher.ProcessAsync();
        }
    }
}
