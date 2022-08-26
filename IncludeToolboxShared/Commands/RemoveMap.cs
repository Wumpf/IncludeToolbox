using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using System;
using Task = System.Threading.Tasks.Task;

namespace IncludeToolbox
{
    [Command(PackageIds.RemMap)]
    internal sealed class RemoveMap : BaseCommand<RemoveMap>
    {
        protected override Task InitializeCompletedAsync()
        {
            return base.InitializeCompletedAsync();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Has to be synchronous")]
        protected override void BeforeQueryStatus(EventArgs e)
        {
            Command.Visible = MapperOptions.Instance.IsInMapAsync().Result;
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var settings = await MapperOptions.GetLiveInstanceAsync();
            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            var file = Utils.MakeRelative(settings.Prefix, doc.FilePath).Replace('\\', '/');

            settings.Map.TryRemoveValue(file);
            settings.Map.WriteMapAsync(settings).FireAndForget();
        }
    }
}
