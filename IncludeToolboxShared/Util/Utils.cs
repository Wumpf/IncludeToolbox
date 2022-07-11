using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace IncludeToolbox
{
    public struct BoolWithReason
    {
        public bool Result;
        public string Reason;
    }

    public static class Utils
    {
        public static string MakeRelative(string absoluteRoot, string absoluteTarget)
        {
            Uri rootUri, targetUri;

            try
            {
                rootUri = new Uri(absoluteRoot);
                targetUri = new Uri(absoluteTarget);
            }
            catch(UriFormatException)
            {
                return absoluteTarget;
            }

            if (rootUri.Scheme != targetUri.Scheme)
                return "";

            Uri relativeUri = rootUri.MakeRelativeUri(targetUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath;
        }

        public static string GetExactPathName(string pathName)
        {
            if (!File.Exists(pathName) && !Directory.Exists(pathName))
                return pathName;

            var di = new DirectoryInfo(pathName);

            if (di.Parent != null)
            {
                return Path.Combine(
                    GetExactPathName(di.Parent.FullName),
                    di.Parent.GetFileSystemInfos(di.Name)[0].Name);
            }
            else
            {
                return di.Name.ToUpper();
            }
        }

        /// <summary>
        /// Retrieves the dominant newline for a given piece of text.
        /// </summary>
        public static string GetDominantNewLineSeparator(string text)
        {
            string lineEndingToBeUsed = "\n";

            // For simplicity we're just assuming that every \r has a \n
            int numLineEndingCLRF = text.Count(x => x == '\r');
            int numLineEndingLF = text.Count(x => x == '\n') - numLineEndingCLRF;
            if (numLineEndingLF < numLineEndingCLRF)
                lineEndingToBeUsed = "\r\n";

            return lineEndingToBeUsed;
        }

        /// <summary>
        /// Prepending a single Item to an to an IEnumerable.
        /// </summary>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> seq, T val)
        {
            yield return val;
            foreach (T t in seq)
            {
                yield return t;
            }
        }
    }
}
