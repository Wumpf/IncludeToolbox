using System;

namespace IncludeToolbox
{
    public class Lexer
    {
        public struct Desc
        {
            public bool ignore_ifdefs = true;
            public bool newlines = false;
            public bool commentaries = false;

            public Desc()
            {
            }
        }
        public enum TType
        {
            Null,
            Terminal,
            Newline,
            Commentary,
            MLCommentary,
            Namespace,
            Class,
            Struct,
            Enum,
            EnumClass,
            ID,
            AngleID,
            QuoteID,
            Colon,
            Include,
            Semicolon,
            OpenBr,
            CloseBr,

            Pragma,

            If,
            Ifdef,
            Ifndef,

            Elif,
            Else,
            Elifdef,

            Endif,

            T0,
            T1,
            T2,
            T3,
            T4,
            T5,
            T6,
            T7,
            T8,
            T9,

        }
        public ref struct Token
        {
            private readonly TType type;
            private readonly ReadOnlySpan<char> value;
            private readonly int pos = 0;

            public int Position { get { return pos; } }
            public int End { get { return pos + Value.Length; } }
            public ReadOnlySpan<char> Value => value;
            public TType Type => type;
            public bool IsPreprocStart => type >= TType.If && type <= TType.Ifndef;
            public bool IsPreprocEnd => type == TType.Endif;

            public Token(TType type, int pos, ReadOnlySpan<char> value)
            {
                this.type = type;
                this.value = value;
                this.pos = pos;
            }
            public Token(TType type = TType.Null, int pos = 0)
            {
                this.type = type;
                this.value = null;
                this.pos = pos;
            }
            public bool valid()
            {
                return Type != TType.Null;
            }
        }

        public ref struct Context
        {
            private readonly ReadOnlySpan<char> original;
            private ReadOnlySpan<char> code;
            private int current_pos = 0;

            public int Position { get => current_pos; }

            public Context(ReadOnlySpan<char> code)
            {
                this.code = code;
                original = code;
            }

            public char Fetch()
            {
                char c = Prefetch();
                if (c == 0) return c;
                current_pos++;
                code = code.Slice(1);
                return c;
            }
            public char Prefetch()
            {
                return Empty() ? (char)0 : code[0];
            }

            public readonly bool Empty()
            {
                return code.Length == 0;
            }
            internal void SkipComment()
            {
                int rem = code.IndexOfAny('\r', '\n');
                if (rem == -1)
                {
                    code = "".AsSpan();
                    return;
                }
                RemovePrefix(rem);
            }
            internal Token TakeComment()
            {
                int rem = code.IndexOfAny('\r', '\n');
                if (rem == -1)
                {
                    code = "".AsSpan();
                    return new(TType.Commentary, current_pos - 1, original.Slice(current_pos - 1));
                }
                Token tk = MakeValueToken(TType.Commentary, rem + 1);
                RemovePrefix(rem);
                return tk;
            }

            internal void SkipCommentML()
            {
                _ = Fetch(); //remove first *
                while (true)
                {
                    int rem = code.IndexOf('*');
                    RemovePrefix(rem + 1);
                    if (rem == -1) return;
                    if (Prefetch() == '/')
                    {
                        RemovePrefix(1);
                        return;
                    }
                }
            }
            internal Token TakeCommentML()
            {
                int start = current_pos - 1;

                _ = Fetch(); //remove first *
                while (true)
                {
                    int rem = code.IndexOf('*');
                    RemovePrefix(rem + 1);
                    if (rem == -1) return new();
                    if (Prefetch() == '/')
                    {
                        RemovePrefix(1);
                        return MakeValueToken(TType.MLCommentary, start, current_pos - start);
                    }
                }
            }

            private void RemovePrefix(int n)
            {
                if (n < 0) { current_pos = code.Length - 1; return; }
                code = code.Slice(n);
                current_pos += n;
            }

            private int FindDelim()
            {
                int i = 0;
                while (i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] == '_')) i++;
                return i;
            }
            private int FindBrace(char brace)
            {
                int i = 0;

                while (i < code.Length && code[i] != (brace == '<' ? '>' : brace))
                { if (code[i] == '\n') return -1; i++; }
                return i + 1;
            }

            private bool IsDelim(char c)
            {
                return !char.IsLetterOrDigit(c) && c != '_';
            }

            internal Token TryAssociateWith(ReadOnlySpan<char> tk, TType type)
            {
                int pos = FindDelim();
                var sl = code.Slice(0, pos);
                bool a = sl.SequenceEqual(tk);

                if (!a) return new();
                Token t = new(type, current_pos - 1);
                RemovePrefix(pos);
                return t;
            }

            internal Token GetID()
            {
                var len = FindDelim();
                Token o = new(TType.ID, current_pos - 1, original.Slice(current_pos - 1, len + 1));
                RemovePrefix(len);
                return o;
            }
            internal Token GetHeader(char delim)
            {
                var len = FindBrace(delim);
                if (len == -1) return new();
                Token o = new(delim == '<' ? TType.AngleID : TType.QuoteID, current_pos - 1, original.Slice(current_pos - 1, len + 1));
                RemovePrefix(len);
                return o;
            }
            internal void Skip()
            {
                int pos = FindDelim();
                RemovePrefix(pos);
            }
            internal void SkipSpace()
            {
                int i = 0;
                while (i < code.Length && char.IsWhiteSpace(code[i])) i++;
                RemovePrefix(i);
            }

            private Token MakeValueToken(TType type, int pos, int length)
            {
                return new(type, pos, original.Slice(pos, length));
            }
            private Token MakeValueToken(TType type, int length)
            {
                return MakeValueToken(type, current_pos - 1, length);
            }

            private Token GetToken(bool expect_id, Desc desc = default)
            {
                Token tk = new();
                while (!Empty())
                {
                    char c = Fetch();

                    if (desc.newlines)
                    {
                        switch (c)
                        {
                            case '\n': return MakeValueToken(TType.Newline, 1);
                            case '\r':
                                if (Prefetch() == '\n')
                                {
                                    tk = MakeValueToken(TType.Newline, 2);
                                    Fetch();
                                    return tk;
                                }
                                return MakeValueToken(TType.Newline, 1);
                        }
                    }

                    switch (c)
                    {
                        case '/':
                            switch (Prefetch())
                            {
                                case '/':
                                    if (desc.commentaries)
                                        return TakeComment();
                                    SkipComment();
                                    if (desc.newlines)
                                        return new Token(TType.Newline);
                                    break;
                                case '*':
                                    if (desc.commentaries)
                                        return TakeCommentML();
                                    SkipCommentML();
                                    break;
                                default: break;
                            }
                            continue;
                        case 'n':
                            tk = TryAssociateWith("amespace".AsSpan(), TType.Namespace);
                            break;
                        case 'c':
                            tk = TryAssociateWith("lass".AsSpan(), TType.Class);
                            break;
                        case 's':
                            tk = TryAssociateWith("truct".AsSpan(), TType.Struct);
                            break;
                        case '#':
                            //stupid, but rarely occurring
                            if (desc.ignore_ifdefs)
                            {
                                c = Prefetch();
                                switch (c)
                                {
                                    case 'i': tk = TryAssociateWith("include".AsSpan(), TType.Include); break;
                                    case 'p': tk = TryAssociateWith("pragma".AsSpan(), TType.Pragma); break;
                                }
                            }
                            else
                            {
                                c = Prefetch();
                                switch (c)
                                {
                                    case 'i':
                                        tk = TryAssociateWith("include".AsSpan(), TType.Include);
                                        if (tk.valid()) return tk;
                                        tk = TryAssociateWith("if".AsSpan(), TType.If);
                                        if (tk.valid()) return tk;
                                        tk = TryAssociateWith("ifdef".AsSpan(), TType.Ifdef);
                                        if (tk.valid()) return tk;
                                        tk = TryAssociateWith("ifndef".AsSpan(), TType.Ifndef);
                                        if (tk.valid()) return tk;
                                        break;
                                    case 'p':
                                        tk = TryAssociateWith("pragma".AsSpan(), TType.Pragma); break;
                                    case 'e':
                                        tk = TryAssociateWith("elif".AsSpan(), TType.Elif);
                                        if (tk.valid()) return tk;
                                        tk = TryAssociateWith("else".AsSpan(), TType.Else);
                                        if (tk.valid()) return tk;
                                        tk = TryAssociateWith("elifdef".AsSpan(), TType.Elifdef);
                                        if (tk.valid()) return tk;
                                        tk = TryAssociateWith("endif".AsSpan(), TType.Endif);
                                        if (tk.valid()) return tk;
                                        break;
                                }
                            }
                            break;
                        case 'e':
                            tk = TryAssociateWith("num".AsSpan(), TType.Enum);
                            break;
                        case ':':
                            if (Prefetch() == ':')
                            {
                                tk = new Token(TType.Colon, Position);
                                _ = Fetch(); //skip the second
                            }
                            break;
                        case ';':
                            tk = MakeValueToken(TType.Semicolon, 1);
                            break;
                        case '<':
                        case '"':
                            if (expect_id)
                                tk = GetHeader(c);
                            break;
                        case '{':
                            tk = MakeValueToken(TType.OpenBr, 1);
                            break;
                        case '}':
                            tk = MakeValueToken(TType.CloseBr, 1);
                            break;
                        default:
                            break;
                    }

                    if (tk.valid()) return tk;
                    if (expect_id && !char.IsWhiteSpace(c))
                        return GetID();
                    else if (!char.IsWhiteSpace(c))
                        Skip();
                }
                return tk;
            }
            public Token GetToken(bool expect_id, bool ignore_ifdefs = true, bool newlines = false, bool commentaries = false)
            {
                return GetToken(expect_id, new() { ignore_ifdefs = ignore_ifdefs, newlines = newlines, commentaries = commentaries });
            }
        }
    }
}
