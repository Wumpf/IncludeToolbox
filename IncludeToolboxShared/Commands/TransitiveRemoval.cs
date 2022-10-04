using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    //[Command(PackageIds.TransitiveRemovalId)]
    internal class TransitiveRemoval : BaseCommand<TransitiveRemoval>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {

        }
    }
}
