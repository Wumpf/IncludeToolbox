using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using static IncludeToolbox.Lexer;
using static Microsoft.VisualStudio.VSConstants;

namespace IncludeToolbox
{
    public enum NewlineChar
    {
        N,
        CR,
        LF,
        CRLF
    }

    public struct IncludeLine
    {
        private string file = "";
        public DelimiterMode delimiter = DelimiterMode.Unchanged;
        public Span span = new();
        public Span file_subspan = new();
        public int line = 0;
        public bool keep = false;
        public NewlineChar newlineChar = NewlineChar.N;

        public IncludeLine()
        {}

        public string Content => Valid ? file.Substring(1, file.Length - 2) : "";
        public string FullFile { get => file; set => file = value; }
        public bool Keep => keep;
        public bool Valid => !string.IsNullOrEmpty(file);
        public int NewlineLength => newlineChar switch { NewlineChar.N => 0, NewlineChar.CR => 2, _ => 1 };


        public Span ReplaceSpan(int relative_pos) => new(relative_pos + span.Start, span.Length);
        public Span ReplaceSpan(int relative_pos, int offset_end) =>
            offset_end >= span.Length ? new() : new(relative_pos + span.Start, span.Length - offset_end);
        public Span ReplaceSpanWithoutNewline(int relative_pos) =>
            ReplaceSpan(relative_pos, NewlineLength);

        public string Project(string over)
        {
            if (!Valid) return "";
            var x = over.Substring(span.Start, span.Length);
            return x.Remove(file_subspan.Start, file_subspan.Length).Insert(file_subspan.Start, FullFile);
        }
        public void SetFullContent(string content) { FullFile = content; }

        public void SetFile(string val)
        {
            switch (delimiter)
            {
                case DelimiterMode.AngleBrackets:
                    FullFile = '<' + val + '>';
                    break;
                case DelimiterMode.Quotes:
                    FullFile = '"' + val + '"';
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
            FullFile.Replace('\\', '/');
        }
        public void ToBackward()
        {
            FullFile.Replace('/', '\\');
        }

        public string Resolve(IEnumerable<string> includeDirectories)
        {
            foreach (string dir in includeDirectories)
            {
                string candidate = Path.Combine(dir, Content);
                if (System.IO.File.Exists(candidate))
                    return Utils.GetExactPathName(candidate);
            }

            Output.WriteLine($"Unable to resolve include: '{Content}'");
            return "";
        }
    }
    public static partial class Parser
    {
        static readonly Regex pragma = new("(?:\\/\\*|\\/\\/)(?:\\s*IWYU\\s+pragma:\\s+keep)");// IWYU pragma: keep 



        public static IncludeLine[] ParseInclues(ReadOnlySpan<char> text, bool ignore_ifdefs = true)
        {
            List<IncludeLine> lines = new();
            Lexer.Context lctx = new(text);


            IncludeLine xline = new();
            bool skip = false;
            bool accept = true;
            bool comments = false;

            int line = 0;
            int start_pos = 0;
            int end_pos = 0;


            while (!lctx.Empty())
            {
                Token tok = lctx.GetToken(accept, ignore_ifdefs, true, comments);
                switch (tok.Type)
                {
                    case TType.Newline:
                        comments = accept = false;
                        line++;
                        if (xline.Valid)
                        {
                            xline.newlineChar = tok.Value.ToString() switch
                            {
                                "\n" => NewlineChar.LF,
                                "\r" => NewlineChar.CR,
                                "\r\n" => NewlineChar.CRLF,
                                _ => NewlineChar.N
                            };
                            end_pos = tok.End;
                            xline.span = new(start_pos, end_pos - start_pos);
                            lines.Add(xline);
                            xline = new IncludeLine();
                        }
                        break;
                    case TType.Include:
                        accept = !skip;
                        start_pos = tok.Position;
                        break;
                    case TType.AngleID:
                    case TType.QuoteID:
                        if (!skip && accept)
                        {
                            var begin = tok.Position - start_pos;
                            xline.FullFile = tok.Value.ToString();
                            xline.delimiter = tok.Type == TType.AngleID ? DelimiterMode.AngleBrackets : DelimiterMode.Quotes;
                            end_pos = tok.End;
                            xline.line = line;
                            xline.file_subspan = new(begin, tok.Value.Length); // subspan of file for replacement

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
                    case TType.MLCommentary:
                        end_pos = tok.End;
                        xline.keep = pragma.IsMatch(tok.Value.ToString());
                        break;
                    default:
                        accept = false;
                        break;
                }
            }

            if (xline.Valid)
            {
                xline.span = new(start_pos, end_pos - start_pos);
                lines.Add(xline);
            }

            return lines.ToArray();
        }
    }
}
