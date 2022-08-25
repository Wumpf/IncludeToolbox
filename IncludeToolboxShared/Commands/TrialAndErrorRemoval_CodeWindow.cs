using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace IncludeToolbox.Commands
{
    [Command(PackageIds.TrialAndError)]
    internal sealed class TrialAndErrorRemoval_CodeWindow : BaseCommand<TrialAndErrorRemoval_CodeWindow>
    {
        TrialAndErrorRemoval impl = new();

        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var x = await VS.Solutions.GetActiveItemAsync();
            if (x.Type != SolutionItemType.PhysicalFile) return;
            _ = Output.WriteLineAsync($"Starting Trial And Error Include removal on {x.FullPath}");
            string err = await impl.StartAsync((PhysicalFile)x, await TrialAndErrorRemovalOptions.GetLiveInstanceAsync());
            if (string.IsNullOrEmpty(err)) return;
            _ = Output.WriteLineAsync(err);
        }
    }
}