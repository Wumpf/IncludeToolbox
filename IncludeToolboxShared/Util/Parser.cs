using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IncludeToolbox.Lexer;

namespace IncludeToolbox
{
    public struct string_view
    {
        public int begin, end;

        public string_view(int begin, int end)
        {
            this.begin = begin;
            this.end = end;
        }

        public int Length => end - begin;

        public string str(string str)
        {
            return str.Substring(begin, end - begin);
        }
        public Microsoft.VisualStudio.Text.Span AsSpan()
        {
            return new Microsoft.VisualStudio.Text.Span(begin, end - begin - 1);
        }
    }

    public struct Namespace
    {
        public string_view head = new();
        public string[] namespaces = null;
        uint scope = 0;

        public uint Scope { get { return scope; } }

        public Namespace(Token tk, uint scope)
        {
            head.begin = tk.Position;
            this.scope = scope;
        }
        public Namespace(string[] namespaces)
        {
            this.namespaces = namespaces;
        }
        public void SetEnd(int end)
        {
            head.end = end + 1;
        }
        public int GetEnd()
        {
            return head.end;
        }
        public bool Valid()
        {
            return namespaces != null;
        }
    }

    public struct FWDDecl
    {
        public string_view span = new();
        public string[] namespaces = null;
        public TType type = TType.Null;
        public bool finished = false;
        public string ID = "";


        public FWDDecl(Token tk)
        {
            span.begin = tk.Position;
            type = tk.type;
        }
        public void SetEnd(int end)
        {
            span.end = end + 1;
            finished = true;
        }
        public bool Valid()
        {
            return finished;
        }
        public Microsoft.VisualStudio.Text.Span AsSpan()
        {
            return span.AsSpan();
        }

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

    public struct Include
    {
        public string_view span = new();
        public TType type = TType.Null;
        public string value = "";
        public bool finished = false;

        public Include(Token tk)
        {
            span.begin = tk.Position;
        }
        public void SetEnd(int end)
        {
            span.end = end + 1;
            finished = true;
        }
        public bool Valid()
        {
            return finished;
        }
        public Microsoft.VisualStudio.Text.Span AsSpan()
        {
            return span.AsSpan();
        }

        public override bool Equals(object obj)
        {
            return obj is Include decl &&
                   type == decl.type &&
                   value == decl.value;
        }

        public override int GetHashCode()
        {
            int hashCode = 1148455455;
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(value);
            return hashCode;
        }

        static public bool operator ==(Include decl1, Include decl2)
        {
            return decl1.type == decl2.type && decl1.value == decl2.value;
        }
        static public bool operator !=(Include decl1, Include decl2)
        {
            return !(decl1 == decl2);
        }
    }

    internal static partial class Parser
    {
        struct Context
        {
            public Stack<string> ns_tree = new(); //where are we
            public Stack<uint> depth = new(); //wie tief
            public Stack<TType> expected_tokens = new(); //what are we waiting for
            bool ex = false;
            uint curr = 0;
            uint scope = 0;

            public static Context operator ++(Context a)
            {
                a.scope++;
                return a;
            }
            public static Context operator --(Context a)
            {
                a.scope--;
                return a;
            }

            public bool Namespace
            {
                get => ex; set
                {
                    ex = value;
                    if (!ex)
                    {
                        depth.Push(curr);
                        curr = 0;
                    }
                }
            }
            public uint Scope { get => scope; }

            public Context()
            {
                expected_tokens.Push(TType.Terminal); //terminal
                expected_tokens.Push(TType.T0); //first rule

                depth.Push(0); //global namespace
                ns_tree.Push("");
            }
            public void PushNamespace(string ns)
            {
                ns_tree.Push(ns);
                curr++;
            }
            public void PopNamespace()
            {
                curr = depth.Pop();
                while (curr-- != 0)
                    ns_tree.Pop();
                curr = 0;
            }
            public string[] GetNamespace()
            {
                return ns_tree.ToArray();
            }
            public void Clear()
            {
                expected_tokens.Clear();
                expected_tokens.Push(TType.Terminal); //terminal
                expected_tokens.Push(TType.T0); //first rule
            }
        }
        public struct Output
        {
            private readonly List<Namespace> namespaces;
            private readonly List<FWDDecl> decls;
            private readonly List<Include> includes;
            private readonly int last_include = -1;

            public List<Namespace> Namespaces { get => namespaces; }
            public List<FWDDecl> Declarations { get => decls; }
            public List<Include> Includes { get => includes; }
            public int LastInclude { get => last_include; }

            public Output(List<Namespace> namespaces, List<FWDDecl> decls, List<Include> includes, int last_include)
            {
                this.namespaces = namespaces;
                this.decls = decls;
                this.includes = includes;
                this.last_include = last_include;
                if (last_include == -1)
                {
                    if (includes.Count == 0)
                    {
                        this.last_include = 0;
                        return;
                    }
                    this.last_include = includes.Last().span.end; //empty file and include
                }
            }
        }

        static void FFWD(ref Lexer.Context ctx, int to_scope, int from_scope)
        {
            while (from_scope != to_scope && !ctx.Empty())
            {
                var tok = ctx.GetToken(false);
                if (tok.type == TType.OpenBr)
                    from_scope++;
                if (tok.type == TType.CloseBr)
                    from_scope--;
            }
        }

        static bool LL_ID(ref Stack<TType> stack, TType current)
        {
            switch (current)
            {
                case TType.T4:
                    return true;
                default:
                    break;
            }
            return false;
        }
        static bool LL_Class(ref Stack<TType> stack, TType current)
        {
            switch (current)
            {
                case TType.T0:
                case TType.T3:
                    stack.Push(current);
                    stack.Push(TType.Semicolon);
                    stack.Push(TType.ID);
                    stack.Push(TType.T1);
                    return true;
                case TType.T1:
                case TType.T4:
                    stack.Push(TType.Class);
                    return true;
                default:
                    break;
            }
            return false;
        }
        static bool LL_Struct(ref Stack<TType> stack, TType current)
        {
            switch (current)
            {
                case TType.T0:
                case TType.T3:
                    stack.Push(current);
                    stack.Push(TType.Semicolon);
                    stack.Push(TType.ID);
                    stack.Push(TType.T1);
                    return true;
                case TType.T1:
                    stack.Push(TType.Struct);
                    return true;
                default:
                    break;
            }
            return false;
        }
        static bool LL_Namespace(ref Stack<TType> stack, TType current)
        {
            switch (current)
            {
                case TType.T0:
                case TType.T3:
                    stack.Push(current);
                    stack.Push(TType.CloseBr);
                    stack.Push(TType.T3);
                    stack.Push(TType.OpenBr);
                    stack.Push(TType.T2);
                    stack.Push(TType.ID);
                    stack.Push(TType.Namespace);
                    return true;
                default:
                    break;
            }
            return false;
        }
        static bool LL_Enum(ref Stack<TType> stack, TType current)
        {
            switch (current)
            {
                case TType.T0:
                case TType.T3:
                    stack.Push(current);
                    stack.Push(TType.Semicolon);
                    stack.Push(TType.ID);
                    stack.Push(TType.T1);
                    return true;
                case TType.T1:
                    stack.Push(TType.T4);
                    stack.Push(TType.Enum);
                    return true;
                default:
                    break;
            }
            return false;
        }

        static bool LLTable(ref Stack<TType> lctx, TType input, TType current)
        {
            switch (input)
            {
                case TType.Namespace: return LL_Namespace(ref lctx, current);
                case TType.Class: return LL_Class(ref lctx, current);
                case TType.Struct: return LL_Struct(ref lctx, current);
                case TType.Enum: return LL_Enum(ref lctx, current);
                case TType.ID: return LL_ID(ref lctx, current);
                case TType.Colon:
                    if (current == TType.T2)
                    {
                        lctx.Push(TType.T2);
                        lctx.Push(TType.ID);
                        lctx.Push(TType.Colon);
                        return true;
                    }
                    return false;
                case TType.OpenBr: return current == TType.T2;
                case TType.CloseBr: return current == TType.T3;
                case TType.Include:
                    if (current == TType.T0)
                    {
                        lctx.Push(TType.T5);
                        lctx.Push(TType.Include);
                        return true;
                    }
                    break;
                case TType.AngleID:
                case TType.QuoteID:
                    if (current == TType.T5)
                    {
                        lctx.Push(TType.T0);
                        lctx.Push(input);
                        return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }



        public static Output Parse(ReadOnlySpan<char> text, bool disable_ns = false, bool disable_count = false)
        {
            List<Namespace> namespaces = new();
            List<FWDDecl> fwd = new();
            List<Include> includes = new();
            Namespace ns = new();
            FWDDecl decl = new();
            Include inc = new();

            Lexer.Context lctx = new(text);
            Parser.Context pctx = new();
            bool accept = false;
            bool include_end = false;
            Token tok = lctx.GetToken(accept);
            int last_include = -1; //eof


            while (pctx.expected_tokens.Count != 0 && tok.valid())
            {
                TType expect = pctx.expected_tokens.Peek();

                if (expect >= TType.T0) //LL rules
                {
                    pctx.expected_tokens.Pop();
                    accept = LLTable(ref pctx.expected_tokens, tok.type, expect);
                    continue;
                }
                if (!accept || expect != tok.type)
                {
                    pctx.Clear(); // unexpected token, start anew
                    if (tok.type == TType.OpenBr) // if scope, probably function or class
                        FFWD(ref lctx, (int)pctx.Scope, (int)pctx.Scope + 1);
                    if (tok.type == TType.CloseBr)
                        pctx--;
                    tok = lctx.GetToken(accept);
                    continue;
                }

                pctx.expected_tokens.Pop();

                if (!disable_count && !include_end 
                    && expect != TType.Include 
                    && expect != TType.AngleID 
                    && expect != TType.QuoteID
                    && includes.Count > 0)
                {
                    include_end = true; 
                    last_include = includes.Last().span.end;
                }

                switch (expect)
                {
                    case TType.Namespace:
                        pctx.Namespace = true;
                        if (!disable_ns)
                            ns = new(tok, pctx.Scope);
                        break;
                    case TType.Class:
                        if (!decl.finished && decl.type == TType.Enum)
                            decl.type = TType.EnumClass; //special case
                        else goto case TType.Struct;
                        break;
                    case TType.Struct:
                    case TType.Enum:
                        decl = new(tok);
                        break;
                    case TType.ID:
                        if (pctx.Namespace)
                            pctx.PushNamespace(tok.value.ToString());
                        else
                            decl.ID = tok.value.ToString();
                        break;
                    case TType.OpenBr:
                        if (pctx.Namespace)
                        {
                            if (!disable_ns)
                            {
                                ns.namespaces = pctx.GetNamespace();
                                ns.SetEnd(tok.Position);
                                namespaces.Add(ns);
                            }
                            pctx.Namespace = false;
                        }
                        break;
                    case TType.CloseBr:
                        pctx.PopNamespace(); break;
                    case TType.Include:
                        inc = new(tok); break;
                    case TType.QuoteID:
                    case TType.AngleID:
                        inc.value = tok.value.ToString();
                        inc.SetEnd(tok.Position + tok.value.Length);
                        inc.type = expect;
                        includes.Add(inc);
                        break;
                    case TType.Semicolon:
                        decl.SetEnd(tok.Position);
                        decl.finished = true;
                        decl.namespaces = pctx.ns_tree.ToArray();
                        fwd.Add(decl);
                        break;
                    default:
                        break;
                }

                if (tok.type == TType.OpenBr)
                    pctx++;
                if (tok.type == TType.CloseBr)
                    pctx--;

                tok = lctx.GetToken(accept);
            }
            return new Output(namespaces, fwd, includes, last_include);
        }
        public static Output Parse(string text)
        {
            return Parse(text.AsSpan());
        }
        public static async Task<Output> ParseAsync(string text)
        {
            return await Task.Run(() => Parse(text));
        }
    }
}
