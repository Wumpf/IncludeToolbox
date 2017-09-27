using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
//using Microsoft.VisualStudio.VCProjectEngine;

namespace IncludeToolbox
{
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

        // Shamelessly stolen from https://stackoverflow.com/a/5076517/153079
        private static string GetFileSystemCasing(string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = path.TrimEnd(Path.DirectorySeparatorChar); // if you type c:\foo\ instead of c:\foo
                try
                {
                    string name = Path.GetFileName(path);
                    if (name == "")
                        return path.ToUpper() + Path.DirectorySeparatorChar; // root reached

                    string parent = Path.GetDirectoryName(path);

                    parent = GetFileSystemCasing(parent);

                    DirectoryInfo diParent = new DirectoryInfo(parent);
                    FileSystemInfo[] fsiChildren = diParent.GetFileSystemInfos(name);
                    FileSystemInfo fsiChild = fsiChildren.First();
                    return fsiChild.FullName;
                }
                catch (Exception ex)
                {
                    Output.Instance.ErrorMsg("Invalid path: '{0}'\nError:\n{1}", path, ex.Message);
                }
            }
            else
            {
                Output.Instance.ErrorMsg("Absolute path needed, not relative: '{0}'", path);
            }

            return "";
        }

        /// <summary>
        /// Gets the path name as it exists it the file system, normalizing it.
        /// </summary>
        /// <param name="pathName">Path name</param>
        /// <returns>Normalized path name. Directories are returned with trailing separator.</returns>
        public static string GetExactPathName(string pathName)
        {
            if (!File.Exists(pathName) && !Directory.Exists(pathName))
            {
                return pathName;
            }

            string exactPathName = GetFileSystemCasing(Path.GetFullPath(pathName).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));
            if (exactPathName == "")
                return pathName;

            // Add trailing slash for directories
            exactPathName = exactPathName.TrimEnd(Path.DirectorySeparatorChar);
            if (File.GetAttributes(exactPathName).HasFlag(FileAttributes.Directory))
                return exactPathName + Path.DirectorySeparatorChar;
            return exactPathName;
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
