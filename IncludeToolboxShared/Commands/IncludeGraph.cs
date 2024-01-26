using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    [Command(PackageIds.IncludeGraphId)]
    internal sealed class IncludeGraph : BaseCommand<IncludeGraph>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            return IncludeGraphToolWindow.ShowAsync();
        }
    }
}
