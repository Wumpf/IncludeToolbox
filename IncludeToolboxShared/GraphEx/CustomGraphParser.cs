using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IncludeToolbox.Graph
{
    public static class CustomGraphParser
    {
        /// <summary>
        /// Parses a given file text using our own simplified include parsing and adds the output to the original graph.
        /// </summary>
        /// <remarks>
        /// If this is the first file, the graph is *not* necessarily a tree after this operation.
        /// 
        /// For any entry in the graph, it will be assumed that all include have already been parsed, therefore no additional parsing takes place for such files!
        /// 
        /// For simplicity (one could argue also for completeness) we do not run any form of preprocessor.
        /// This also means, that we need to assume that every include is included always only exactly once.
        /// Obviously, this assumption is generally false! However, in practice a include is rarely included several times intentionally.
        /// </remarks>
        /// <param name="filename">Filename to the file that should be processed, may be relative.</param>
        /// <param name="documentText">File content that should be parsed.</param>
        /// <param name="includeDirectories">
        /// Directories for resolving includes.
        /// Unlike in other places in the code, we always try to resolve first with the local path. So you should *not* to include it here yourself.
        /// This is necessary since the local path changes during the recursion.
        /// </param>
        public static void AddIncludesRecursively_ManualParsing(this IncludeGraph graph, string filename, string fileContent, IEnumerable<string> includeDirectories, IEnumerable<string> nonParseDirectories)
        {
            var graphItem = graph.CreateOrGetItem(filename, out bool isNewGraphItem);
            if (!isNewGraphItem)
                return;

            ParseIncludesRecursively(graph, graphItem, fileContent, includeDirectories, nonParseDirectories);
        }

        /// <see cref="AddIncludesRecursively_ManualParsing(IncludeGraph, string, string, IEnumerable{string})"/>
        public static void AddIncludesRecursively_ManualParsing(this IncludeGraph graph, string filename, IEnumerable<string> includeDirectories, IEnumerable<string> nonParseDirectories)
        {
            var graphItem = graph.CreateOrGetItem(filename, out bool isNewGraphItem);
            if (!isNewGraphItem)
                return;

            ParseIncludesRecursively(graph, graphItem, File.ReadAllText(filename), includeDirectories, nonParseDirectories);
        }

        private static void ParseIncludesRecursively(IncludeGraph graph, IncludeGraph.GraphItem parentItem, string fileContent, IEnumerable<string> includeDirectories, IEnumerable<string> nonParseDirectories)
        {
            string currentDirectory = Path.GetDirectoryName(parentItem.AbsoluteFilename);
            var includeDirectoriesPlusLocal = includeDirectories.Prepend(currentDirectory);

            var includes = Formatter.IncludeLineInfo.ParseIncludes(fileContent, Formatter.ParseOptions.KeepOnlyValidIncludes);
            foreach (var includeLine in includes)
            {
                // Try to resolve the include (may fail)
                string resolvedInclude = includeLine.TryResolveInclude(includeDirectoriesPlusLocal, out bool successfullyResolved);
                // Create a link to the file in any case now even if resolving was unsuccessful.
                var includedFile = graph.CreateOrGetItem_AbsoluteNormalizedPath(resolvedInclude, out bool isNewGraphItem);

                if (successfullyResolved && isNewGraphItem && !nonParseDirectories.Any(x => resolvedInclude.StartsWith(x)))
                {
                    bool successReadingFile = true;
                    try
                    {
                        fileContent = File.ReadAllText(resolvedInclude);
                    }
                    catch
                    {
                        successReadingFile = false;
                        Output.Instance.WriteLine("Unable to read included file: '{0}'", resolvedInclude);
                    }
                    if (successReadingFile)
                    {
                        ParseIncludesRecursively(graph, includedFile, fileContent, includeDirectories, nonParseDirectories);
                    }
                }

                parentItem.Includes.Add(new IncludeGraph.Include { IncludeLine = includeLine, IncludedFile = includedFile });
            }
        }
    }
}
