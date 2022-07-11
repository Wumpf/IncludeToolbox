using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IncludeToolbox.Formatter
{
    [Flags]
    public enum ParseOptions
    {
        None = 0,

        /// <summary>
        /// Whether IncludeLineInfo objects should be created for empty lines.</param>
        /// </summary>
        RemoveEmptyLines = 1,

        /// <summary>
        /// Marks all includes that are within preprocessor conditionals as inactive/non-includes
        /// </summary>
        IgnoreIncludesInPreprocessorConditionals = 2,

        /// <summary>
        /// Keep only lines that contain valid includes.
        /// </summary>
        KeepOnlyValidIncludes = 4 | RemoveEmptyLines,
    }

    /// <summary>
    /// A line of text + information about the include directive in this line if any.
    /// Allows for manipulation of the former.
    /// </summary>
    /// <remarks>
    /// This is obviously not a high performance representation of text, but very easy to use for our purposes here.
    /// </remarks>
    public class IncludeLineInfo
    {
        /// <summary>
        /// Parses a given text into IncludeLineInfo objects.
        /// </summary>
        /// <returns>A list of parsed lines.</returns>
        public static List<IncludeLineInfo> ParseIncludes(string text, ParseOptions options)
        {
            StringReader reader = new StringReader(text);

            var outInfo = new List<IncludeLineInfo>();

            // Simplistic parsing.
            int openMultiLineComments = 0;
            int openIfdefs = 0;
            string lineText;
            for (int lineNumber = 0; true; ++lineNumber)
            {
                lineText = reader.ReadLine();
                if (lineText == null)
                    break;

                if (options.HasFlag(ParseOptions.RemoveEmptyLines) && string.IsNullOrWhiteSpace(lineText))
                    continue;

                int commentedSectionStart = int.MaxValue;
                int commentedSectionEnd = int.MaxValue;

                // Check for single line comment.
                {
                    int singleLineCommentStart = lineText.IndexOf("//");
                    if (singleLineCommentStart != -1)
                        commentedSectionStart = singleLineCommentStart;
                }

                // Check for multi line comments.
                {
                    int multiLineCommentStart = lineText.IndexOf("/*");
                    if (multiLineCommentStart > -1 && multiLineCommentStart < commentedSectionStart)
                    {
                        ++openMultiLineComments;
                        commentedSectionStart = multiLineCommentStart;
                    }

                    int multiLineCommentEnd = lineText.IndexOf("*/");
                    if (multiLineCommentEnd > -1)
                    {
                        --openMultiLineComments;
                        commentedSectionEnd = multiLineCommentEnd;
                    }
                }

                bool isCommented(int pos) => (commentedSectionStart == int.MaxValue && openMultiLineComments > 0) || (pos > commentedSectionStart && pos < commentedSectionEnd);

                // Check for #if / #ifdefs.
                if (options.HasFlag(ParseOptions.IgnoreIncludesInPreprocessorConditionals))
                {
                    // There can be only a single preprocessor directive per line, so no need to parse more than this.
                    // (in theory it must be the first thing in the line, but MSVC is not strict on this, so we aren't either.
                    int ifdefStart = lineText.IndexOf("#if");
                    int ifdefEnd = lineText.IndexOf("#endif");
                    if (ifdefStart > -1 && !isCommented(ifdefStart))
                    {
                        ++openIfdefs;
                    }
                    else if (ifdefEnd > -1 && !isCommented(ifdefEnd))
                    {
                        --openIfdefs;
                    }
                }

                int includeOccurence = lineText.IndexOf("#include");

                // Not a valid include.
                if (includeOccurence == -1 ||        // Include not found
                    isCommented(includeOccurence) || // Include commented out
                    openIfdefs > 0)                // Inside an #ifdef block
                {
                    if (!options.HasFlag(ParseOptions.KeepOnlyValidIncludes))
                        outInfo.Add(new IncludeLineInfo() { lineText = lineText, LineNumber = lineNumber });
                }
                // A valid include
                else
                {
                    // Parse include delimiters.
                    int delimiter1 = -1;
                    int delimiter0 = lineText.IndexOf('\"', includeOccurence + "#include".Length);
                    if (delimiter0 == -1)
                    {
                        delimiter0 = lineText.IndexOf('<', includeOccurence + "#include".Length);
                        if (delimiter0 != -1)
                            delimiter1 = lineText.IndexOf('>', delimiter0 + 1);
                    }
                    else
                    {
                        delimiter1 = lineText.IndexOf('\"', delimiter0 + 1);
                    }

                    // Might not be valid after all!
                    if (delimiter0 != -1 && delimiter1 != -1)
                        outInfo.Add(new IncludeLineInfo() { lineText = lineText, LineNumber = lineNumber, delimiter0 = delimiter0, delimiter1 = delimiter1 });
                    else if (!options.HasFlag(ParseOptions.KeepOnlyValidIncludes))
                        outInfo.Add(new IncludeLineInfo() { lineText = lineText, LineNumber = lineNumber });
                }
            }

            return outInfo;
        }

        /// <summary>
        /// Whether the line includes an enabled include.
        /// </summary>
        /// <remarks>
        /// A line that contains a valid #include may still be ContainsActiveInclude==false if it is commented or (depending on parsing options) #if(def)'ed out.
        /// </remarks>
        public bool ContainsActiveInclude => delimiter0 != -1;

        public enum DelimiterType
        {
            Quotes,
            AngleBrackets,
            None
        }

        public DelimiterType LineDelimiterType
        {
            get
            {
                if (ContainsActiveInclude)
                {
                    DelimiterSanityCheck();

                    if (lineText[delimiter0] == '<')
                        return DelimiterType.AngleBrackets;
                    else if (lineText[delimiter0] == '\"')
                        return DelimiterType.Quotes;
                }
                return DelimiterType.None;
            }
        }

        /// <summary>
        /// Changes the type of this line.
        /// Has only an effect if ContainsActiveInclude is true.
        /// </summary>
        public void SetDelimiterType(DelimiterType newDelimiterType)
        {
            if (LineDelimiterType != newDelimiterType && ContainsActiveInclude)
            {
                DelimiterSanityCheck();

                if (newDelimiterType == DelimiterType.AngleBrackets)
                {
                    StringBuilder sb = new StringBuilder(lineText);
                    sb[delimiter0] = '<';
                    sb[delimiter1] = '>';
                    lineText = sb.ToString();
                }
                else if (newDelimiterType == DelimiterType.Quotes)
                {
                    StringBuilder sb = new StringBuilder(lineText);
                    sb[delimiter0] = '"';
                    sb[delimiter1] = '"';
                    lineText = sb.ToString();
                }
            }
        }

        /// <summary>
        /// Whether the line contains a preprocessor directive.
        /// Does not take into account surrounding block comments.
        /// </summary>
        public bool ContainsPreProcessorDirective
        {
            get
            {
                // In theory the '#' of a preprocessor directive MUST come first, but just like MSVC we relax the rules a bit here.
                foreach (char c in lineText)
                {
                    if (c == '#')
                        return true;
                    else if (!char.IsWhiteSpace(c))
                        return false;
                }

                return false;
            }
        }

        /// <summary>
        /// Tries to resolve the include (if any) using a list of directories.
        /// </summary>
        /// <param name="includeDirectories">Include directories. Keep in mind that IncludeLineInfo does not know the path of the file this include is from.</param>
        /// <returns>Empty string if this is not an include, absolute include path if possible or raw include if not.</returns>
        public string TryResolveInclude(IEnumerable<string> includeDirectories, out bool success)
        {
            if (!ContainsActiveInclude)
            {
                success = false;
                return "";
            }

            string includeContent = IncludeContent;

            foreach (string dir in includeDirectories)
            {
                string candidate = Path.Combine(dir, includeContent);
                if (File.Exists(candidate))
                {
                    success = true;
                    return Utils.GetExactPathName(candidate);
                }
            }

            Output.Instance.WriteLine("Unable to resolve include: '{0}'", includeContent);
            success = false;
            return includeContent;
        }

        /// <summary>
        /// Include content with added delimiters.
        /// </summary>
        public string GetIncludeContentWithDelimiters()
        {
            if (ContainsActiveInclude)
            {
                DelimiterSanityCheck();
                return lineText.Substring(delimiter0, delimiter1 - delimiter0 + 1);
            }
            else
                return string.Empty;
        }


        /// <summary>
        /// Changes in the include content will NOT be reflected immediately in the raw line text.
        /// </summary>
        /// <see cref="UpdateRawLineWithIncludeContentChanges"/>
        public string IncludeContent
        {
            get
            {
                if (ContainsActiveInclude)
                {
                    DelimiterSanityCheck();
                    int length = delimiter1 - delimiter0 - 1;
                    return length > 0 ? RawLine.Substring(delimiter0 + 1, length) : "";
                }
                else
                    return string.Empty;
            }
            set
            {
                if (!ContainsActiveInclude)
                    return;

                lineText = lineText.Remove(delimiter0 + 1, delimiter1 - delimiter0 - 1);
                lineText = lineText.Insert(delimiter0 + 1, value);
                delimiter1 = delimiter0 + value.Length + 1;
            }
        }

        /// <summary>
        /// Raw line text as found.
        /// </summary>
        public string RawLine
        {
            get { return lineText; }
        }
        private string lineText = "";

        /// <summary>
        /// Line number in which this include line occurred within its original file.
        /// </summary>
        /// <remarks>
        /// Starts of course with 0 unlike displayed line numbers.
        /// </remarks>
        public int LineNumber { get; private set; } = -1;

        public static bool ContainsPreserveFlag(string lineText)
        {
            return lineText.Contains("$include-toolbox-preserve$");
        }

        /// <summary>
        /// Whether the include line should not be removed by iwyu and Trial & Error Removal.
        /// </summary>
        public bool ShouldBePreserved { get { return ContainsPreserveFlag(lineText); } }

        private int delimiter0 = -1;
        private int delimiter1 = -1;

        [System.Diagnostics.Conditional("DEBUG")]
        private void DelimiterSanityCheck()
        {
            System.Diagnostics.Debug.Assert(delimiter0 >= 0 && delimiter0 < lineText.Length);
            System.Diagnostics.Debug.Assert(delimiter1 >= 0 && delimiter1 < lineText.Length);
            System.Diagnostics.Debug.Assert(delimiter0 < delimiter1);
        }
    }
}
