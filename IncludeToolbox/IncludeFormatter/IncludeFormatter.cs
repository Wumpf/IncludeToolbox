using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IncludeToolbox.IncludeFormatter
{
    static class IncludeFormatter
    {
        public static string FormatPath(string absoluteIncludeFilename, FormatterOptionsPage.PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Absolute)
            {
                return absoluteIncludeFilename;
            }
            else
            {
                // todo: Treat std library files special?
                if (absoluteIncludeFilename != null)
                {
                    int bestLength = Int32.MaxValue;
                    string bestCandidate = null;

                    foreach (string includeDirectory in includeDirectories)
                    {
                        string proposal = Utils.MakeRelative(includeDirectory, absoluteIncludeFilename);

                        if (proposal.Length < bestLength)
                        {
                            if (pathformat == FormatterOptionsPage.PathMode.Shortest ||
                                (proposal.IndexOf("../") < 0 && proposal.IndexOf("..\\") < 0))
                            {
                                bestCandidate = proposal;
                                bestLength = proposal.Length;
                            }
                        }
                    }

                    return bestCandidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Formats the paths of a given list of include line info.
        /// </summary>
        public static void FormatPaths(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.PathMode pathformat, IEnumerable<string> includeDirectories)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Unchanged)
                return;

            foreach (var line in lines)
            {
                line.IncludeContent = FormatPath(line.AbsoluteIncludePath, pathformat, includeDirectories) ?? line.IncludeContent;
            }
        }

        public static void FormatDelimiters(IncludeLineInfo[] lines, FormatterOptionsPage.DelimiterMode delimiterMode)
        {
            switch (delimiterMode)
            {
                case FormatterOptionsPage.DelimiterMode.AngleBrackets:
                    foreach (var line in lines)
                        line.SetLineType(IncludeLineInfo.Type.AngleBrackets);
                    break;
                case FormatterOptionsPage.DelimiterMode.Quotes:
                    foreach (var line in lines)
                        line.SetLineType(IncludeLineInfo.Type.Quotes);
                    break;
            }
        }

        public static void FormatSlashes(IncludeLineInfo[] lines, FormatterOptionsPage.SlashMode slashMode)
        {
            switch (slashMode)
            {
                case FormatterOptionsPage.SlashMode.ForwardSlash:
                    foreach (var line in lines)
                        line.IncludeContent = line.IncludeContent.Replace('\\', '/');
                    break;
                case FormatterOptionsPage.SlashMode.BackSlash:
                    foreach (var line in lines)
                        line.IncludeContent = line.IncludeContent.Replace('/', '\\');
                    break;
            }
        }

        public static void SortIncludes(IncludeLineInfo[] lines, FormatterOptionsPage settings, string documentName)
        {
            FormatterOptionsPage.TypeSorting typeSorting = settings.SortByType;
            bool regexIncludeDelimiter = settings.RegexIncludeDelimiter;
            bool blankAfterRegexGroupMatch = settings.BlankAfterRegexGroupMatch;
            bool removeEmptyLines = settings.RemoveEmptyLines;

            var comparer = new IncludeComparer(settings.PrecedenceRegexes, documentName);
            string[] precedenceRegexes = comparer.PrecedenceRegexes;

            var sortedIncludes = lines.Where(x => x.LineType != IncludeLineInfo.Type.NoInclude).OrderBy(x => x.IncludeContentForRegex(regexIncludeDelimiter), comparer).ToArray();

            var mapSortedIndexToRealIndex = new Dictionary<int, int>();
            {
                int sortedIdx = 0;
                for (int realIdx = 0; realIdx < lines.Length && sortedIdx < sortedIncludes.Length; ++realIdx)
                {
                    if (lines[realIdx].LineType != IncludeLineInfo.Type.NoInclude)
                    {
                        mapSortedIndexToRealIndex.Add(sortedIdx, realIdx);
                        ++sortedIdx;
                    }
                }
            }

            // Optionally insert newlines between regex match groups
            if (blankAfterRegexGroupMatch && precedenceRegexes.Length > 0 && sortedIncludes.Length > 1)
            {
                // Zip the sorted includes up with their index
                var sortedIncludesAndIndex = Enumerable.Range(0, sortedIncludes.Length).Zip(sortedIncludes, (x, y) => new { index = x, sortedInclude = y });

                // Group the sorted includes by the index of the precedence regex they match, -1 for
                // NoInclude lines, or precedenceRegexes.Length for no match.
                var includeGroups = sortedIncludesAndIndex.GroupBy(x =>
                {
                    if (x.sortedInclude.LineType == IncludeLineInfo.Type.NoInclude)
                        return -1;

                    var sortedIncludeContent = x.sortedInclude.IncludeContentForRegex(regexIncludeDelimiter);
                    for (int precedence = 0; precedence < precedenceRegexes.Count(); ++precedence)
                    {
                        if (Regex.Match(sortedIncludeContent, precedenceRegexes[precedence]).Success)
                        {
                            return precedence;
                        }
                    }

                    return precedenceRegexes.Length;
                }, x => x);

                // Go through all but the first group and prepend a newline to each group's first
                // include
                foreach (var grouping in includeGroups.Where(x => x.Key >= 0).Skip(1))
                {
                    var startGroup = grouping.First();

                    if (!removeEmptyLines)
                    {
                        // If we don't want to remove empty lines and the line before this group is
                        // already empty, do nothing
                        int realIdx = mapSortedIndexToRealIndex[startGroup.index];
                        if (realIdx > 0 && lines[realIdx - 1].LineType == IncludeLineInfo.Type.NoInclude)
                            continue;
                    }

                    startGroup.sortedInclude.Text = String.Format("{0}{1}", Environment.NewLine, startGroup.sortedInclude.Text);
                }
            }
            
            if (typeSorting == FormatterOptionsPage.TypeSorting.AngleBracketsFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.AngleBrackets ? 0 : 1).ToArray();
            else if (typeSorting == FormatterOptionsPage.TypeSorting.QuotedFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineType == IncludeLineInfo.Type.Quotes ? 0 : 1).ToArray();

            for (int sortedIncludeIdx = 0; sortedIncludeIdx < sortedIncludes.Count(); ++sortedIncludeIdx)
            {
                int realIdx = mapSortedIndexToRealIndex[sortedIncludeIdx];
                lines[realIdx] = sortedIncludes[sortedIncludeIdx];
            }
        }
    }
}
