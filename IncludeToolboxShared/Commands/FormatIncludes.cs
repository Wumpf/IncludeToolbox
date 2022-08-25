using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;


namespace IncludeToolbox.Commands
{
    [Command(PackageIds.FormatIncludesId)]
    internal sealed class FormatIncludes : BaseCommand<FormatIncludes>
    {
        SnapshotSpan GetSelectionLines(IWpfTextView viewHost)
        {
            if (viewHost == null) return new SnapshotSpan();
            var sel = viewHost.Selection.StreamSelectionSpan;
            var start = new SnapshotPoint(viewHost.TextSnapshot, sel.Start.Position).GetContainingLine().Start;
            var end = new SnapshotPoint(viewHost.TextSnapshot, sel.End.Position).GetContainingLine().EndIncludingLineBreak;

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
            var lines = Parser.ParseInclues(selection_span.GetText().AsSpan(), false);// faster a times! tested with QPC 
            Command.Visible = lines.Count() != 0;
        }




        protected override async Task ExecuteAsync(OleMenuCmdEventArgs args)
        {
            var settings = await FormatOptions.GetLiveInstanceAsync();
            var doc = await VS.Documents.GetActiveDocumentViewAsync();
            // Read.
            var selection_span = await GetSelectionLinesAsync();
            var include_directories = await VCUtil.GetIncludeDirsAsync();
            var text = selection_span.GetText();
            // Format
            var formated_lines = Formatter.IncludeFormatter.FormatIncludes(text.AsSpan(), doc.FilePath, include_directories, settings);

            // Overwrite.
            using var edit = doc.TextBuffer.CreateEdit();
            Formatter.IncludeFormatter.ApplyChanges(formated_lines, edit, text, selection_span.Start, settings.RemoveEmptyLines);
            edit.Apply();
        }
    }
}
