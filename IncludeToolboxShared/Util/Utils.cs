using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace IncludeToolbox
{
    public struct BoolWithReason
    {
        public bool Result;
        public string Reason;
    }

    public static class Utils
    {
        public static string GetLineBreak(IWpfTextView view)
        {
            return view.Options.GetNewLineCharacter();
        }

        public static Span GetIncludeSpan(string text)
        {
            int[] line = new int[2];
            line[0] = text.IndexOf("#include"); //first
            line[1] = text.IndexOf("\n", text.LastIndexOf("#include")) - line[0] + 1; //last
            return new Span(line[0], line[1]);
        }
        
        public static ReadOnlySpan<char> GetIncludeSpanRO(string text)
        {
            int[] line = new int[2];
            line[0] = text.IndexOf("#include"); //first
            if (line[0] == -1) return new ReadOnlySpan<char>();
            line[1] = text.IndexOf("\n", text.LastIndexOf("#include")) - line[0] + 1; //last
            return text.AsSpan(line[0], line[1]);
        }


        public static string MakeRelative(string absoluteRoot, string absoluteTarget)
        {
            Uri rootUri, targetUri;

            if (!absoluteRoot.EndsWith(Path.DirectorySeparatorChar.ToString()))
                absoluteRoot += Path.DirectorySeparatorChar;

            try
            {
                rootUri = new Uri(absoluteRoot);
                targetUri = new Uri(absoluteTarget);
            }
            catch (UriFormatException)
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
    }
}
