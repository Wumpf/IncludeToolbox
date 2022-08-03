using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Threading.Tasks;
using static IncludeToolbox.Parser;

namespace IncludeToolbox
{
    internal static class IWYUApply
    {
        static readonly string match = "The full include-list for ";


        public static Span GetIncludeSpan(string text)
        {
            int[] line = new int[2];
            line[0] = text.IndexOf("#include"); //first
            line[1] = text.IndexOf("\n", text.LastIndexOf("#include")) - line[0]; //last
            return new Span(line[0], line[1]);
        }

        public static string GetLineBreak(ITextEdit edit)
        {
            return edit.Snapshot.Lines.ElementAt(0).GetLineBreakText();
        }
        public static void ApplyCheap(ITextEdit edit, string result, bool commentary)
        {
            if (!commentary)
            {
                var lb = edit.Snapshot.Lines.ElementAt(0).GetLineBreakText();
                lb = string.IsNullOrEmpty(lb) ? "\r\n" : lb;
                result = string.Join(lb, result.Split('\n')
                    .Select(s =>
                    {
                        var str = s.Trim();
                        var idx = str.IndexOf("//");
                        if (idx >= 0)
                            return str.Substring(0, idx).Trim();
                        return str;
                    }).ToArray());
            }
            var span = GetIncludeSpan(edit.Snapshot.GetText());
            edit.Replace(span, result);
        }

        public static async Task<string> PreformatAsync(string text, string file)
        {
            var include_directories = await VCUtil.GetIncludeDirsAsync();
            return Formatter.IncludeFormatter.FormatIncludes(text, file, include_directories, await FormatOptions.GetLiveInstanceAsync());
        }

        public static async Task FormatAsync(DocumentView doc)
        {
            using var xedit = doc.TextBuffer.CreateEdit();
            var text = xedit.Snapshot.GetText();
            var span = IWYUApply.GetIncludeSpan(text);
            var result = await IWYUApply.PreformatAsync(new SnapshotSpan(xedit.Snapshot, span).GetText(), doc.FilePath);
            xedit.Replace(span, result);
            xedit.Apply();
        }

        public static async Task ApplyAsync(IWYUOptions settings, string output)
        {
            if (output == "") return;

            while (true)
            {
                int pos = output.IndexOf(match);
                if (pos == -1) return;

                pos += match.Length;
                string part = output.Substring(pos);

                int endp = part.IndexOf("---");
                string path = part.Substring(0, part.IndexOf(':', 3));
                var doc = await VS.Documents.OpenAsync(path);
                using var edit = doc.TextBuffer.CreateEdit();


                if (settings.Sub == Substitution.Cheap)
                {
                    int endl = part.IndexOf("\n");
                    string result = part.Substring(endl, endp - endl);
                    IWYUApply.ApplyCheap(edit,
                        result,
                        settings.Comms != Comment.No);
                }
                edit.Apply();

                if (settings.Format)
                    await FormatAsync(doc);
                if (settings.FormatDoc)
                    await VS.Commands.ExecuteAsync(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID.FORMATDOCUMENT);

                output = part.Substring(endp);
            }
        }

        public static async Task ApplyPreciseAsync(IWYUOptions settings, Parser.Output parsed, string output, Standard std)
        {
            if (output == "") return;

            while (true)
            {
                int pos = output.IndexOf(match);
                if (pos == -1) return;

                var retasks = Parser.Parse(output.AsSpan().Slice(0, pos), true, true);
                int sep_index = output.IndexOf(" should remove these lines:"); //find middle ground


                pos += match.Length;
                string part = output.Substring(pos);


                int endp = part.IndexOf("---");
                string path = part.Substring(0, part.IndexOf(':', 3));
                var doc = await VS.Documents.OpenAsync(path);
                using var edit = doc.TextBuffer.CreateEdit();



                var add_f = retasks.Declarations.Where(s => s.span.begin < sep_index);
                var rem_f = retasks.Declarations.Where(s => s.span.begin > sep_index);

                var add_i = retasks.Includes.Where(s => s.span.begin < sep_index);
                var rem_i = retasks.Includes.Where(s => s.span.begin > sep_index);


                DeclNode tree = new(Lexer.TType.Namespace);
                if (settings.MoveDecls)
                {
                    tree.AddChildren(parsed.Declarations.Where(s => !rem_f.Contains(s)));
                    foreach (var task in parsed.Declarations)
                        edit.Delete(task.AsSpan());
                }

                tree.AddChildren(add_f);
                string result = tree.ToString(std >= Standard.cpp17);
                edit.Insert(parsed.LastInclude, '\n' + result);


                foreach (var item in add_i)
                {
                    edit.Insert(parsed.LastInclude, '\n' + item.span.str(output));
                }
                if (!settings.MoveDecls)
                    foreach (var task in rem_f)
                    {
                        var found = parsed.Declarations.FindLast(s => s == task);

                        if (!found.Valid()) continue;
                        edit.Delete(found.AsSpan());
                        parsed.Declarations.Remove(found);

                    }

                foreach (var task in rem_i)
                {
                    var found = parsed.Includes.FindLast(s => s == task);

                    if (!found.Valid()) continue;
                    edit.Delete(found.AsSpan());
                    parsed.Includes.Remove(found);
                }


                edit.Apply();

                if (settings.Format)
                    await FormatAsync(doc);
                if (settings.FormatDoc)
                    await VS.Commands.ExecuteAsync(Microsoft.VisualStudio.VSConstants.VSStd2KCmdID.FORMATDOCUMENT);
                output = part.Substring(endp);
            }
        }
    }
}
