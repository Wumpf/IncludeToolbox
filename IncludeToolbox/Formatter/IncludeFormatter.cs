using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace IncludeToolbox.Formatter
{
    public static class IncludeFormatter
    {
        public static string FormatPath(string absoluteIncludeFilename,
                                        FormatterOptionsPage.PathMode pathformat,
                                        IEnumerable<string> includeDirectories,
                                        string includeRootDirectory = null)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Absolute)
            {
                return absoluteIncludeFilename;
            }
            else if (pathformat == FormatterOptionsPage.PathMode.Absolute_FromParentDirWithFile &&
                     null != absoluteIncludeFilename &&
                     null != includeRootDirectory)
            {
                return Utils.MakeRelative(includeRootDirectory, absoluteIncludeFilename);
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
        private static void FormatPaths(IEnumerable<IncludeLineInfo> lines,
                                        FormatterOptionsPage.PathMode pathformat,
                                        FormatterOptionsPage.IgnoreFileRelativeMode ignoreFileRelativeMode,
                                        IEnumerable<string> includeDirectories,
                                        string documentDir,
                                        string includeRootDirectory = null)
        {
            if (pathformat == FormatterOptionsPage.PathMode.Unchanged)
                return;

            foreach (var line in lines)
            {
                string absoluteIncludePath = line.TryResolveInclude(includeDirectories, out bool resolvedPath);
                if (resolvedPath)
                {
                    if (pathformat == FormatterOptionsPage.PathMode.Absolute_FromParentDirWithFile &&
                        includeRootDirectory != null &&
                        !absoluteIncludePath.StartsWith(includeRootDirectory))
                    {
                        continue;
                    }

                    var currentPathFormat = pathformat;
                    var currentIncludeRootDirectory = includeRootDirectory;
                    string includeFilename = Path.GetFileName(absoluteIncludePath);
                    if ((ignoreFileRelativeMode == FormatterOptionsPage.IgnoreFileRelativeMode.InSameDirectory &&
                         Path.Combine(documentDir, includeFilename) == absoluteIncludePath) ||
                        (ignoreFileRelativeMode == FormatterOptionsPage.IgnoreFileRelativeMode.InSameOrSubDirectory &&
                         absoluteIncludePath.StartsWith(documentDir)))
                    {
                        currentPathFormat = FormatterOptionsPage.PathMode.Absolute_FromParentDirWithFile;
                        currentIncludeRootDirectory = documentDir;
                    }
                    line.IncludeContent = FormatPath(absoluteIncludePath,
                                                     currentPathFormat,
                                                     includeDirectories,
                                                     currentIncludeRootDirectory) ?? line.IncludeContent;
                }
            }
        }

        private static void FormatDelimiters(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.DelimiterMode delimiterMode)
        {
            switch (delimiterMode)
            {
                case FormatterOptionsPage.DelimiterMode.AngleBrackets:
                    foreach (var line in lines)
                        line.SetDelimiterType(IncludeLineInfo.DelimiterType.AngleBrackets);
                    break;
                case FormatterOptionsPage.DelimiterMode.Quotes:
                    foreach (var line in lines)
                        line.SetDelimiterType(IncludeLineInfo.DelimiterType.Quotes);
                    break;
            }
        }

        private static void FormatSlashes(IEnumerable<IncludeLineInfo> lines, FormatterOptionsPage.SlashMode slashMode)
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

        private static List<IncludeLineInfo> SortIncludes(IList<IncludeLineInfo> lines, FormatterOptionsPage settings, string documentName)
        {
            string[] precedenceRegexes = RegexUtils.FixupRegexes(settings.PrecedenceRegexes, documentName);

            List<IncludeLineInfo> outSortedList = new List<IncludeLineInfo>(lines.Count);

            IEnumerable<IncludeLineInfo> includeBatch;
            int numConsumedItems = 0;

            do
            {
                // Fill in all non-include items between batches.
                var nonIncludeItems = lines.Skip(numConsumedItems).TakeWhile(x => !x.ContainsActiveInclude);
                numConsumedItems += nonIncludeItems.Count();
                outSortedList.AddRange(nonIncludeItems);

                // Process until we hit a preprocessor directive that is not an include.
                // Those are boundaries for the sorting which we do not want to cross.
                includeBatch = lines.Skip(numConsumedItems).TakeWhile(x => x.ContainsActiveInclude || !x.ContainsPreProcessorDirective);
                numConsumedItems += includeBatch.Count();

            } while (SortIncludeBatch(settings, precedenceRegexes, outSortedList, includeBatch) && numConsumedItems != lines.Count);

            return outSortedList;
        }

        private static bool SortIncludeBatch(FormatterOptionsPage settings, string[] precedenceRegexes,
                                            List<IncludeLineInfo> outSortedList, IEnumerable<IncludeLineInfo> includeBatch)
        {
            // Get enumerator and cancel if batch is empty.
            if (!includeBatch.Any())
                return false;

            // Fetch settings.
            FormatterOptionsPage.TypeSorting typeSorting = settings.SortByType;
            bool regexIncludeDelimiter = settings.RegexIncludeDelimiter;
            bool blankAfterRegexGroupMatch = settings.BlankAfterRegexGroupMatch;

            // Select only valid include lines and sort them. They'll stay in this relative sorted
            // order when rearranged by regex precedence groups.
            var includeLines = includeBatch
                .Where(x => x.ContainsActiveInclude)
                .OrderBy(x => x.IncludeContent)
                .ToList();

            if (settings.RemoveDuplicates)
            {
                HashSet<string> uniqueIncludes = new HashSet<string>();
                includeLines.RemoveAll(x => !x.ShouldBePreserved &&
                                            !uniqueIncludes.Add(x.GetIncludeContentWithDelimiters()));
            }

            // Group the includes by the index of the precedence regex they match, or
            // precedenceRegexes.Length for no match, and sort the groups by index.
            var includeGroups = includeLines
                .GroupBy(x =>
                {
                    var includeContent = regexIncludeDelimiter ? x.GetIncludeContentWithDelimiters() : x.IncludeContent;
                    for (int precedence = 0; precedence < precedenceRegexes.Count(); ++precedence)
                    {
                        if (Regex.Match(includeContent, precedenceRegexes[precedence]).Success)
                            return precedence;
                    }

                    return precedenceRegexes.Length;
                }, x => x)
                .OrderBy(x => x.Key);

            // Optional newlines between regex match groups
            var groupStarts = new HashSet<IncludeLineInfo>();
            if (blankAfterRegexGroupMatch && precedenceRegexes.Length > 0 && includeLines.Count() > 1)
            {
                // Set flag to prepend a newline to each group's first include
                foreach (var grouping in includeGroups)
                    groupStarts.Add(grouping.First());
            }

            // Flatten the groups
            var sortedIncludes = includeGroups.SelectMany(x => x.Select(y => y));

            // Sort by angle or quoted delimiters if either of those options were selected
            if (typeSorting == FormatterOptionsPage.TypeSorting.AngleBracketsFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineDelimiterType == IncludeLineInfo.DelimiterType.AngleBrackets ? 0 : 1);
            else if (typeSorting == FormatterOptionsPage.TypeSorting.QuotedFirst)
                sortedIncludes = sortedIncludes.OrderBy(x => x.LineDelimiterType == IncludeLineInfo.DelimiterType.Quotes ? 0 : 1);

            // Merge sorted includes with original non-include lines
            var sortedIncludeEnumerator = sortedIncludes.GetEnumerator();
            var sortedLines = includeBatch.Select(originalLine =>
            {
                if (originalLine.ContainsActiveInclude)
                {
                    // Replace original include with sorted includes
                    return sortedIncludeEnumerator.MoveNext() ? sortedIncludeEnumerator.Current : new IncludeLineInfo();
                }
                return originalLine;
            });

            if (settings.RemoveEmptyLines)
            {
                // Removing duplicates may have introduced new empty lines
                sortedLines = sortedLines.Where(sortedLine => !string.IsNullOrWhiteSpace(sortedLine.RawLine));
            }

            // Finally, update the actual lines
            {
                bool firstLine = true;
                foreach (var sortedLine in sortedLines)
                {
                    // Handle prepending a newline if requested, as long as:
                    // - this include is the begin of a new group
                    // - it's not the first line
                    // - the previous line isn't already a non-include
                    if (groupStarts.Contains(sortedLine) && !firstLine && outSortedList[outSortedList.Count - 1].ContainsActiveInclude)
                    {
                        outSortedList.Add(new IncludeLineInfo());
                    }
                    outSortedList.Add(sortedLine);
                    firstLine = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Formats all includes in a given piece of text.
        /// </summary>
        /// <param name="text">Text to be parsed for includes.</param>
        /// <param name="documentName">Path to the document the edit is occuring in.</param>
        /// <param name="includeDirectories">A list of include directories</param>
        /// <param name="settings">Settings that determine how the formating should be done.</param>
        /// <returns>Formated text.</returns>
        public static string FormatIncludes(string text, string documentPath, IEnumerable<string> includeDirectories, FormatterOptionsPage settings)
        {
            string documentDir = Utils.GetExactPathName(Path.GetDirectoryName(documentPath));
            string documentName = Path.GetFileNameWithoutExtension(documentPath);
            string includeRootDirectory = null;
            if (settings.PathFormat == FormatterOptionsPage.PathMode.Absolute_FromParentDirWithFile &&
                !String.IsNullOrWhiteSpace(settings.FromParentDirWithFile))
            {
                var dir = new DirectoryInfo(documentDir);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, settings.FromParentDirWithFile)))
                    {
                        includeRootDirectory = Utils.GetExactPathName(dir.FullName);
                        break;
                    }

                    try
                    {
                        dir = dir.Parent;
                    }
                    catch (System.Security.SecurityException)
                    {
                        // Permission denied
                        break;
                    }
                }
            }

            includeDirectories = new string[] { Microsoft.VisualStudio.PlatformUI.PathUtil.Normalize(documentDir) + Path.DirectorySeparatorChar }.Concat(includeDirectories);

            string newLineChars = Utils.GetDominantNewLineSeparator(text);

            var lines = IncludeLineInfo.ParseIncludes(text, settings.RemoveEmptyLines ? ParseOptions.RemoveEmptyLines : ParseOptions.None);

            // Format.
            IEnumerable<string> formatingDirs = includeDirectories;
            if (settings.IgnoreFileRelative == FormatterOptionsPage.IgnoreFileRelativeMode.Always)
            {
                formatingDirs = formatingDirs.Skip(1);
            }
            FormatPaths(lines,
                        settings.PathFormat,
                        settings.IgnoreFileRelative,
                        formatingDirs,
                        documentDir,
                        includeRootDirectory);
            FormatDelimiters(lines, settings.DelimiterFormatting);
            FormatSlashes(lines, settings.SlashFormatting);

            // Sorting. Ignores non-include lines.
            lines = SortIncludes(lines, settings, documentName);

            // Combine again.
            return string.Join(newLineChars, lines.Select(x => x.RawLine));
        }
    }
}
