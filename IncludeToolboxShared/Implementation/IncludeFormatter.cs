using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IncludeToolbox.Formatter
{
    public static class IncludeFormatter
    {
        public static string FormatPath(string absoluteIncludeFilename, PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            // todo: Treat std library files special?

            if (absoluteIncludeFilename == null) return null;

            int bestLength = int.MaxValue;
            string bestCandidate = null;

            foreach (string includeDirectory in includeDirectories)
            {
                string proposal = Utils.MakeRelative(includeDirectory, absoluteIncludeFilename);

                if (proposal.Length < bestLength)
                {
                    if (pathformat == PathMode.Shortest ||
                        (proposal.IndexOf("../") < 0 && proposal.IndexOf("..\\") < 0))
                    {
                        bestCandidate = proposal;
                        bestLength = proposal.Length;
                    }
                }
            }
            return bestCandidate;
        }
        private static void FormatPaths(IncludeLine[] lines, PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                string absoluteIncludeDir = lines[i].Resolve(includeDirectories);
                if (string.IsNullOrEmpty(absoluteIncludeDir)) continue;
                var formatted = FormatPath(absoluteIncludeDir, pathformat, includeDirectories);
                if (string.IsNullOrEmpty(formatted)) continue;
                lines[i].SetFile(formatted);
            }
        }

        private static void FormatDelimiters(IncludeLine[] lines, DelimiterMode delimiterMode)
        {
            switch (delimiterMode)
            {
                case DelimiterMode.AngleBrackets:
                    for (int i = 0; i < lines.Length; i++)
                        lines[i].SetDelimiter(DelimiterMode.AngleBrackets);
                    break;
                case DelimiterMode.Quotes:
                    for (int i = 0; i < lines.Length; i++)
                        lines[i].SetDelimiter(DelimiterMode.Quotes);
                    break;
            }
        }
        private static void FormatSlashes(IncludeLine[] lines, SlashMode slashMode)
        {
            switch (slashMode)
            {
                case SlashMode.ForwardSlash:
                    for (int i = 0; i < lines.Length; i++)
                        lines[i].ToForward();
                    break;
                case SlashMode.BackSlash:
                    for (int i = 0; i < lines.Length; i++)
                        lines[i].ToBackward();
                    break;
            }
        }

        private static IncludeLine[] SortIncludes(IncludeLine[] lines, FormatOptions settings, string documentName)
        {
            string[] precedenceRegexes = RegexUtils.FixupRegexes(settings.PrecedenceRegexes, documentName);
            List<IncludeLine> outSortedList = new(lines.Length);

            while (lines.Length != 0)
            {
                int line_n = lines.First().line;

                var pack = lines.TakeWhile(s =>
                {
                    bool a = s.line - line_n <= 1;
                    line_n = s.line;
                    return a;
                });
                var e = pack.ToArray();
                if (e.Count() > 1)
                    outSortedList.AddRange(SortIncludeBatch(settings, precedenceRegexes, e));
                else
                    outSortedList.AddRange(e);
                lines = lines.Skip(e.Count()).ToArray();
            }

            return outSortedList.ToArray();
        }

        private static void RemoveDuplicates(IncludeLine[] includes)
        {
            HashSet<string> uniqueIncludes = new();
            uniqueIncludes.UnionWith(includes.Where(s => s.Keep).Select(s => s.FullFile));

            for (int i = 0; i < includes.Length; i++)
            {
                ref var r = ref includes[i];
                if (!r.Keep && !uniqueIncludes.Add(r.FullFile))
                    r.SetFullContent("");
            }
        }
        private static IncludeLine[] SortIncludeBatch(FormatOptions settings,
                                                          string[] precedenceRegexes,
                                                          IncludeLine[] includeBatch)
        {
            // Fetch settings.
            TypeSorting typeSorting = settings.SortByType;
            bool regexIncludeDelimiter = settings.RegexIncludeDelimiter;
            bool blankAfterRegexGroupMatch = settings.BlankAfterRegexGroupMatch;

            // Select only valid include lines and sort them. They'll stay in this relative sorted
            // order when rearranged by regex precedence groups.
            var includeLines = includeBatch
                .OrderBy(x => { return x.Content; }).ToArray();

            if (settings.RemoveDuplicates)
            {
                // store kept headers first, to remove all the duplicates
                HashSet<string> uniqueIncludes = new();
                uniqueIncludes.UnionWith(includeLines.Where(s => s.Keep).Select(s => s.FullFile));

                for (int i = 0; i < includeLines.Length; i++)
                {
                    ref var r = ref includeLines[i];
                    if (!r.Keep && !uniqueIncludes.Add(r.FullFile))
                        r.SetFullContent("");
                }
            }

            // Group the includes by the index of the precedence regex they match, or
            // precedenceRegexes.Length for no match, and sort the groups by index.
            var includeGroups = includeLines
                .GroupBy(x =>
                {
                    if (!x.Valid) return precedenceRegexes.Length;
                    var includeContent = regexIncludeDelimiter ? x.FullFile : x.Content;
                    for (int precedence = 0; precedence < precedenceRegexes.Count(); ++precedence)
                    {
                        if (Regex.Match(includeContent, precedenceRegexes[precedence]).Success)
                            return precedence;
                    }
                    return precedenceRegexes.Length;
                }, x => x)
                .OrderBy(x => x.Key);

            // Optional newlines between regex match groups
            var groupStarts = new HashSet<IncludeLine>();
            if (blankAfterRegexGroupMatch && precedenceRegexes.Length > 0 && includeLines.Count() > 1)
            {
                // Set flag to prepend a newline to each group's first include
                foreach (var grouping in includeGroups)
                    groupStarts.Add(grouping.First());
            }

            // Flatten the groups
            var sortedIncludes = includeGroups.SelectMany(x => x.Select(y => y));

            // Sort by angle or quoted delimiters if either of those options were selected
            if (typeSorting == TypeSorting.AngleBracketsFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.delimiter == DelimiterMode.AngleBrackets ? 0 : 1);
            else if (typeSorting == TypeSorting.QuotedFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.delimiter == DelimiterMode.Quotes ? 0 : 1);

            return sortedIncludes.ToArray();
        }



        public static IncludeLine[] FormatIncludes(ReadOnlySpan<char> text, string documentPath, IEnumerable<string> includeDirectories, FormatOptions settings)
        {
            string documentDir = Path.GetDirectoryName(documentPath);
            string documentName = Path.GetFileNameWithoutExtension(documentPath);

            includeDirectories = new string[] { Microsoft.VisualStudio.PlatformUI.PathUtil.Normalize(documentDir) + Path.DirectorySeparatorChar }.Concat(includeDirectories);

            var lines = Parser.ParseInclues(text, settings.IgnoreIfdefs);

            // Format.
            IEnumerable<string> formatingDirs = includeDirectories;
            if (settings.IgnoreFileRelative)
                formatingDirs = formatingDirs.Skip(1);

            if (settings.RemoveDuplicates)
                RemoveDuplicates(lines);
            if (settings.PathFormat != PathMode.Unchanged)
                FormatPaths(lines, settings.PathFormat, formatingDirs);

            FormatDelimiters(lines, settings.DelimiterFormatting);
            FormatSlashes(lines, settings.SlashFormatting);


            // Sorting. Ignores non-include lines.
            return SortIncludes(lines, settings, documentName);
        }


        private static IEnumerable<KeyValuePair<Span, string>> RemoveWhitespaces(this IEnumerable<KeyValuePair<Span, string>> e, string text)
        {
            int start = 0;
            foreach (var a in e)
            {
                var x = a;
                if (start != 0 && a.Key.Start - start > 0)
                {
                    ReadOnlySpan<char> subspan = text.AsSpan(start, a.Key.Start - start);
                    if (subspan.IsWhiteSpace())
                        x = new(new(start, a.Key.Start - start + a.Key.Length), a.Value);
                }
                start = x.Key.End;
                yield return x;
            }
        }
        public static void ApplyChanges(IncludeLine[] includes, DocumentView doc, string text, int relative_pos, bool remove_empty = true)
        {
            var lb = Utils.GetLineBreak(doc.TextView);
            var enumerator = includes
                .OrderBy(s => s.line)
                .Zip(includes,
                (a, b) => { return new KeyValuePair<Span, string>(a.span, b.Project(text)); });


            using var edit = doc.TextBuffer.CreateEdit();

            if (remove_empty) enumerator = enumerator.RemoveWhitespaces(text);
            foreach (var line in enumerator)
                edit.Replace(line.Key.Move(relative_pos), line.Value);

            edit.Apply();
        }
    }
}
