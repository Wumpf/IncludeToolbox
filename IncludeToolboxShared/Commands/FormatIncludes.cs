using System;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Task = System.Threading.Tasks.Task;


namespace IncludeToolbox.Commands
{
    [Command(PackageIds.FormatIncludesId)]
    internal sealed class FormatIncludes : BaseCommand<RunIWYU>
    {
        SnapshotSpan GetSelectionLines(IWpfTextView viewHost)
        {
            if (viewHost == null) return new SnapshotSpan();
            var sel = viewHost.Selection.StreamSelectionSpan;
            var start = new SnapshotPoint(viewHost.TextSnapshot, sel.Start.Position).GetContainingLine().Start;
            var end = new SnapshotPoint(viewHost.TextSnapshot, sel.End.Position).GetContainingLine().End;

            return new SnapshotSpan(start, end);
        }
        async Task<SnapshotSpan> GetSelectionLinesAsync()
        {
            IWpfTextView viewHost = (await VS.Documents.GetActiveDocumentViewAsync())?.TextView;
            return GetSelectionLines(viewHost);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Has to be synchronous")]
        protected override void BeforeQueryStatus(EventArgs e)
        {
            var selection_span = GetSelectionLinesAsync().Result;
            var lines = Formatter.IncludeLineInfo.ParseIncludes(selection_span.GetText(), Formatter.ParseOptions.RemoveEmptyLines);
            Command.Visible = lines.Any(x => x.ContainsActiveInclude);
        }




        protected override async Task ExecuteAsync(OleMenuCmdEventArgs args)
        {
            var settings = await FormatOptions.GetLiveInstanceAsync();
            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            // Read.
            var selection_span = await GetSelectionLinesAsync();
            var include_directories = await VCUtil.GetIncludeDirsAsync();

            // Format
            string formatedText = Formatter.IncludeFormatter.FormatIncludes(selection_span.GetText(), doc.FilePath, include_directories, settings);

            // Overwrite.
            using (var edit = doc.TextBuffer.CreateEdit())
            {
                edit.Replace(selection_span, formatedText);
                edit.Apply();
            }
        }
    }
}
