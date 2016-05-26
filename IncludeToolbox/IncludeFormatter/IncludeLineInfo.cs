using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IncludeToolbox.IncludeFormatter
{
    public class IncludeLineInfo
    {
        public static IncludeLineInfo[] ParseIncludes(string text, bool removeEmptyLines, List<string> includeDirectories)
        {
            var lines = Regex.Split(text, "\r\n|\r|\n");
            if (removeEmptyLines)
            {
                lines = lines.Where(x => x.Length > 0).ToArray();
            }
            var outInfo = new IncludeLineInfo[lines.Length];

            // Simplistic parsing.
            int openMultiLineComments = 0;
            for (int line = 0; line < lines.Length; ++line)
            {
                outInfo[line] = new IncludeLineInfo();
                outInfo[line].text = lines[line];
                outInfo[line].lineType = IncludeLineInfo.Type.NoInclude;

                // Check for single line comment.
                int singleLineComment = lines[line].IndexOf("//");
                if (singleLineComment == -1)
                    singleLineComment = int.MaxValue;
                // Check for multi line comments.
                int multiLineCommentEnd = lines[line].IndexOf("*/");
                if (multiLineCommentEnd > -1 && multiLineCommentEnd < singleLineComment)
                    --openMultiLineComments;
                int multiLineCommentStart = lines[line].IndexOf("/*");
                if (multiLineCommentStart > -1 && multiLineCommentStart < singleLineComment)
                    ++openMultiLineComments;

                int includeOccurence = lines[line].IndexOf("#include");
                if (includeOccurence == -1) // No include found
                    continue;
                if (includeOccurence > singleLineComment) // Single line before #include
                    continue;
                if (openMultiLineComments > 0 && multiLineCommentStart == -1 && multiLineCommentEnd == -1) // Multi comment around #include.
                    continue;
                if (multiLineCommentEnd > includeOccurence) // Multi line comment ended in same line but after #include.
                    continue;
                if (multiLineCommentStart > -1 && multiLineCommentStart < includeOccurence) // Multi line comment started in same line, but before #include.
                    continue;


                outInfo[line].Delimiter0 = lines[line].IndexOf('\"', includeOccurence + "#include".Length);
                if (outInfo[line].Delimiter0 == -1)
                {
                    outInfo[line].Delimiter0 = lines[line].IndexOf('<', includeOccurence + "#include".Length);
                    if (outInfo[line].Delimiter0 == -1)
                        continue;
                    outInfo[line].Delimiter1 = lines[line].IndexOf('>', outInfo[line].Delimiter0 + 1);
                    outInfo[line].lineType = IncludeLineInfo.Type.AngleBrackets;
                }
                else
                {
                    outInfo[line].Delimiter1 = lines[line].IndexOf('\"', outInfo[line].Delimiter0 + 1);
                    outInfo[line].lineType = IncludeLineInfo.Type.Quotes;
                }
                if (outInfo[line].Delimiter1 == -1)
                    continue;

                outInfo[line].includeContent = lines[line].Substring(outInfo[line].Delimiter0 + 1, outInfo[line].Delimiter1 - outInfo[line].Delimiter0 - 1);

                // Try to resolve include path to an existing file.
                if (includeDirectories != null)
                {
                    foreach (string dir in includeDirectories)
                    {
                        string candidate = Path.Combine(dir, outInfo[line].IncludeContent);
                        if (File.Exists(candidate))
                        {
                            outInfo[line].AbsoluteIncludePath = Utils.GetExactPathName(candidate);
                            break;
                        }
                    }
                    if (outInfo[line].AbsoluteIncludePath == null)
                    {
                        Output.Instance.WriteLine("Unabled to resolve include: {0}", outInfo[line].IncludeContent);
                    }
                }
            }

            return outInfo;
        }


        public enum Type
        {
            Quotes,
            AngleBrackets,

            NoInclude
        }

        public Type LineType
        {
            get { return lineType; }
        }
        private Type lineType;

        public void SetLineType(Type newLineType)
        {
            if (lineType != newLineType)
            {
                lineType = newLineType;
                if(Delimiter0 >= 0 && Delimiter1 >= 0)
                {
                    if (lineType == Type.AngleBrackets)
                    {
                        StringBuilder sb = new StringBuilder(text);
                        sb[Delimiter0] = '<';
                        sb[Delimiter1] = '>';
                        text = sb.ToString();
                    }
                    else if (lineType == Type.Quotes)
                    {
                        StringBuilder sb = new StringBuilder(text);
                        sb[Delimiter0] = '"';
                        sb[Delimiter1] = '"';
                        text = sb.ToString();
                    }
                }
                else
                {
                    lineType = Type.NoInclude;
                }
            }
        }

        public void UpdateTextWithIncludeContent()
        {
            if (lineType == Type.NoInclude)
                return;

            text = text.Remove(Delimiter0 + 1, Delimiter1 - Delimiter0 - 1);
            text = text.Insert(Delimiter0 + 1, includeContent);
            Delimiter1 = Delimiter0 + includeContent.Length + 1;
        }

        public string IncludeContentForRegex(bool regexIncludeDelimiter)
        {
            if (!regexIncludeDelimiter || lineType == Type.NoInclude)
                return includeContent;

            char[] delimiters = { '"', '"' };
            if (lineType == Type.AngleBrackets)
            {
                delimiters[0] = '<';
                delimiters[1] = '>';
            }

            return String.Format("{0}{1}{2}", delimiters[0], includeContent, delimiters[1]);
        }

        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        private string text = "";

        public string IncludeContent
        {
            get { return includeContent; }
            set { includeContent = value; }
        }
        private string includeContent = "";

        public string AbsoluteIncludePath { get; private set; } = null;

        public int Delimiter0 { get; private set; } = -1;
        public int Delimiter1 { get; private set; } = -1;

        public int OriginalLineNumber { get; private set; }
    }
}
