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
    public static partial class Parser
    {
        static readonly Regex pragma = new("(?:\\/\\*|\\/\\/)(?:\\s*IWYU\\s+pragma:\\s+keep)");// IWYU pragma: keep 



        public static IncludeLine[] ParseInclues(ReadOnlySpan<char> text, bool ignore_ifdefs = true)
        {
            if (text.IsEmpty) return new IncludeLine[0];
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
                            xline.span = new(start_pos, tok.End - start_pos);
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
