using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using static IncludeToolbox.Lexer;
using static Microsoft.VisualStudio.VSConstants;

namespace IncludeToolbox
{
    public struct IncludeLine
    {
        public string file = "";
        public DelimiterMode delimiter = DelimiterMode.Unchanged;
        public string_view span;
        public string_view file_subspan = new();
        public int line = 0;
        public bool keep = false;

        public IncludeLine(string file, DelimiterMode delimiter, string_view span, int line)
        {
            this.file = file;
            this.delimiter = delimiter;
            this.span = span;
            this.line = line;
        }

        public string Content => Valid ? file.Substring(1, file.Length - 2) : "";
        public string FullLine => "#include " + file;
        public bool Keep => keep;
        public bool Valid => !string.IsNullOrEmpty(file);
        public Span ReplaceSpan(int relative_pos) => new(relative_pos + span.begin, span.end - span.begin);

        public string Project(string over)
        {
            if (!Valid) return "";
            var x = span.str(over);
            return x.Remove(file_subspan.begin, file_subspan.Length).Insert(file_subspan.begin, file);
        }
        public void SetFullContent(string content) { file = content; }

        public void SetFile(string val)
        {
            switch (delimiter)
            {
                case DelimiterMode.AngleBrackets:
                    file = '<' + val + '>';
                    break;
                case DelimiterMode.Quotes:
                    file = '"' + val + '"';
                    break;
            }
        }
        public void SetDelimiter(DelimiterMode delimiter)
        {
            if (this.delimiter == delimiter) return;
            this.delimiter = delimiter;
            SetFile(Content);
        }
        public void ToForward()
        {
            file.Replace('\\', '/');
        }
        public void ToBackward()
        {
            file.Replace('/', '\\');
        }

        public string Resolve(IEnumerable<string> includeDirectories)
        {
            foreach (string dir in includeDirectories)
            {
                string candidate = Path.Combine(dir, Content);
                if (File.Exists(candidate))
                    return Utils.GetExactPathName(candidate);
            }

            Output.WriteLine($"Unable to resolve include: '{Content}'");
            return "";
        }
    }
    internal static partial class Parser
    {
        static readonly Regex pragma = new("(?:\\/\\*|\\/\\/)(?:\\s*IWYU\\s+pragma:\\s+keep)");// IWYU pragma: keep 


        public static IncludeLine[] ParseInclues(ReadOnlySpan<char> text, bool ignore_ifdefs)
        {
            List<IncludeLine> lines = new();
            Lexer.Context lctx = new(text);


            IncludeLine xline = new();
            bool skip = false;
            bool accept = true;
            bool comments = false;
            int line = 0;


            while (!lctx.Empty())
            {
                Token tok = lctx.GetToken(accept, ignore_ifdefs, true, comments);
                switch (tok.type)
                {
                    case TType.Newline:
                        comments = accept = false;
                        line++;
                        if (xline.Valid)
                        {
                            xline.span.end = tok.Position;
                            lines.Add(xline);
                            xline = new IncludeLine();
                        }
                        break;
                    case TType.Include:
                        accept = !skip;
                        xline.span.begin = tok.Position;
                        break;
                    case TType.AngleID:
                    case TType.QuoteID:
                        if (!skip && accept)
                        {
                            var begin = tok.Position - xline.span.begin;
                            xline.file = tok.value.ToString();
                            xline.delimiter = tok.type == TType.AngleID ? DelimiterMode.AngleBrackets : DelimiterMode.Quotes;
                            xline.span.end = tok.Position + tok.value.Length;
                            xline.line = line;
                            xline.file_subspan = new(begin, begin + tok.value.Length); // subspan of file for replacement

                            accept = false;
                            comments = true;
                        }
                        break;
                    case TType.Ifdef:
                    case TType.Ifndef:
                    case TType.Elif:
                    case TType.Else:
                    case TType.Elifdef:
                        skip = true;
                        break;
                    case TType.Endif:
                        skip = false;
                        break;
                    case TType.Commentary:
                        line++;
                        goto case TType.MLCommentary;
                    case TType.MLCommentary:
                        xline.span.end = tok.Position + tok.value.Length;
                        xline.keep = pragma.IsMatch(tok.value.ToString());
                        break;
                    default:
                        accept = false;
                        break;
                }
            }

            if (xline.Valid)
                lines.Add(xline);

            return lines.ToArray();
        }
    }
}
