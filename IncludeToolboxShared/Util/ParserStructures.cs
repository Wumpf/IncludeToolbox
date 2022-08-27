using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static IncludeToolbox.Lexer;

namespace IncludeToolbox
{
    public struct Namespace
    {
        public Span span = new();
        public string[] namespaces = null;
        public uint scope = 0;

        public Namespace() { }
    }

    public struct FWDDecl
    {
        public Span span = new();
        public string[] namespaces = null;
        public TType type = TType.Null;
        public string ID = "";

        public FWDDecl() { }

        public bool Valid => !string.IsNullOrEmpty(ID);

        public override bool Equals(object obj)
        {
            return obj is FWDDecl decl &&
                   namespaces.SequenceEqual(decl.namespaces) &&
                   type == decl.type &&
                   ID == decl.ID;
        }

        public override int GetHashCode()
        {
            int hashCode = -1447791890;
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(namespaces);
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ID);
            return hashCode;
        }

        static public bool operator ==(FWDDecl decl1, FWDDecl decl2)
        {
            bool a = decl2.namespaces.SequenceEqual(decl1.namespaces);
            return decl1.ID == decl2.ID && decl1.type == decl2.type && a;
        }
        static public bool operator !=(FWDDecl decl1, FWDDecl decl2)
        {
            return !(decl1 == decl2);
        }
    }

    public struct IncludeLine : IEquatable<IncludeLine>
    {
        private string file = "";
        public DelimiterMode delimiter = DelimiterMode.Unchanged;
        public Span span = new();
        public Span file_subspan = new();
        public int line = 0;
        public bool keep = false;
        public NewlineChar newlineChar = NewlineChar.N;

        public IncludeLine()
        { }

        public string Content => Valid ? file.Substring(1, file.Length - 2) : "";
        public string FullFile { get => file; set => file = value; }
        public bool Keep => keep;
        public bool Valid => !string.IsNullOrEmpty(file);
        public int NewlineLength => newlineChar switch { NewlineChar.N => 0, NewlineChar.CR => 2, _ => 1 };
        public int End => span.End;



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

        public override bool Equals(object obj)
        {
            return obj is IncludeLine line && Equals(line);
        }

        public bool Equals(IncludeLine other)
        {
            return FullFile == other.FullFile && keep == other.keep;
        }

        public override int GetHashCode()
        {
            int hashCode = -1366893598;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FullFile);
            hashCode = hashCode * -1521134295 + Keep.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(IncludeLine left, IncludeLine right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(IncludeLine left, IncludeLine right)
        {
            return !(left == right);
        }
    }

    public enum NewlineChar
    {
        N,
        CR,
        LF,
        CRLF
    }
}
