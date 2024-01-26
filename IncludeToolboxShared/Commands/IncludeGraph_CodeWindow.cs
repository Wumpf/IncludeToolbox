using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox.Commands
{
    [Command(PackageIds.IncludeGraphCodeId)]
    internal class IncludeGraph_CodeWindow : BaseCommand<IncludeGraph_CodeWindow>
    {
        protected override Task InitializeCompletedAsync()
        {
            Command.Supported = false;
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var pane = await IncludeGraphToolWindow.ShowAsync();
            var cont = (IncludeGraphControl)pane.Content;
            var context = (IncludeGraphViewModel)cont.DataContext;
            var file = await VS.Documents.GetActiveDocumentViewAsync();

            context.RecalculateForAsync(file).FireAndForget();
        }
    }
}
