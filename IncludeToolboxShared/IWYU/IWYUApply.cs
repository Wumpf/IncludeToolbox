﻿using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Text;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IncludeToolbox
{
    internal static class IWYUApply
    {
        static readonly string match = "The full include-list for ";

        public static void ClearNamespaces(DocumentView doc)
        {
            using var edit = doc.TextBuffer.CreateEdit();
            var text = doc.TextBuffer.CurrentSnapshot.GetText();
            var rem = Parser.ParseEmptyNamespaces(text);
            foreach (var ns in rem)
                edit.Delete(ns);
            edit.Apply();
        }

        public static async Task FormatAsync(DocumentView doc)
        {
            var include_directories = await VCUtil.GetIncludeDirsAsync();
            var settings = await FormatOptions.GetLiveInstanceAsync();
            var text = doc.TextBuffer.CurrentSnapshot.GetText();
            var span = Utils.GetIncludeSpan(text);
            var slice = text.Substring(span.Start, span.Length);

            var result = Formatter.IncludeFormatter.FormatIncludes(
                slice.AsSpan(),
                doc.FilePath,
                include_directories, settings
                );

            Formatter.IncludeFormatter.ApplyChanges(result, doc, slice, span.Start);
        }

        public static void ApplyCheap(ITextEdit edit, string result, bool commentary, string lb)
        {
            if (!commentary)
            {
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
            var span = Utils.GetIncludeSpan(edit.Snapshot.GetText());
            edit.Replace(span, result);
        }

        public static async Task ApplyAsync(IWYUOptions settings, string output)
        {
            if (output == "") return;

            int pos = output.IndexOf(match);
            if (pos == -1) return;

            pos += match.Length;
            string part = output.Substring(pos);

            int endp = part.IndexOf("---");
            string path = part.Substring(0, part.IndexOf(':', 3));
            var doc = await VS.Documents.OpenAsync(path);
            var lb = Utils.GetLineBreak(doc.TextView);

            using var edit = doc.TextBuffer.CreateEdit();

            int endl = part.IndexOf("\n");
            string result = part.Substring(endl, endp - endl);
            ApplyCheap(edit,
                result,
                settings.Comms != Comment.No, lb);

            edit.Apply();
        }

        public static async Task ApplyPreciseAsync(IWYUOptions settings, Parser.Output parsed, string output, Standard std)
        {
            if (output == "") return;

            int pos = output.IndexOf(match);
            if (pos == -1) return;

            var retasks = Parser.Parse(output.AsSpan().Slice(0, pos), true, true);
            int sep_index = output.IndexOf(" should remove these lines:"); //find middle ground

            pos += match.Length;
            string part = output.Substring(pos);

            string path = part.Substring(0, part.IndexOf(':', 3));
            var doc = await VS.Documents.OpenAsync(path);
            using var edit = doc.TextBuffer.CreateEdit();
            var lb = Utils.GetLineBreak(doc.TextView);

            var add_f = retasks.Declarations.Where(s => s.span.Start < sep_index);
            var rem_f = retasks.Declarations.Where(s => s.span.Start > sep_index);

            var add_i = retasks.Includes.Where(s => s.span.Start < sep_index);
            var rem_i = retasks.Includes.Where(s => s.span.Start > sep_index);

            foreach (var task in rem_i)
            {
                var found = parsed.Includes.FindLast(s => s == task);
                edit.Delete(found.span);
                parsed.Includes.Remove(found);
            }

            DeclNode tree = new(Lexer.TType.Namespace)
            {
                LineBreak = lb
            };
            if (settings.MoveDecls)
            {
                tree.AddChildren(parsed.Declarations.Where(s => !rem_f.Contains(s)));
                foreach (var task in parsed.Declarations)
                    edit.Delete(task.span);
            }
            else
            {
                foreach (var task in rem_f)
                {
                    var found = parsed.Declarations.FindLast(s => s == task);
                    edit.Delete(found.span);
                    parsed.Declarations.Remove(found);
                }
            }
            tree.AddChildren(add_f);

            string addition = "";

            foreach (var item in add_i.Select(s => s.Project(output)))
                addition+=lb+item;

            addition+= lb + lb + tree.ToString(std >= Standard.cpp17);
                
            edit.Insert(parsed.InsertionPoint, addition);
            edit.Apply();
        }
    }
}
