using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static IncludeToolbox.Lexer;

namespace IncludeToolbox
{
    public static partial class Parser
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
            private readonly List<IncludeLine> includes;
            private readonly int insertion_point = 0;

            public List<Namespace> Namespaces { get => namespaces; }
            public List<FWDDecl> Declarations { get => decls; }
            public List<IncludeLine> Includes { get => includes; }
            public int InsertionPoint { get => insertion_point; }

            public Output(List<Namespace> namespaces, List<FWDDecl> decls, List<IncludeLine> includes, int insertion_point = 0)
            {
                this.namespaces = namespaces;
                this.decls = decls;
                this.includes = includes;
                this.insertion_point = insertion_point;
            }
        }

        static void FFWD(ref Lexer.Context ctx, int to_scope, int from_scope)
        {
            while (from_scope != to_scope && !ctx.Empty())
            {
                var tok = ctx.GetToken(false);
                if (tok.Type == TType.OpenBr)
                    from_scope++;
                if (tok.Type == TType.CloseBr)
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
            List<IncludeLine> includes = new();

            Namespace ns = new();
            FWDDecl decl = new();
            IncludeLine inc = new();

            Lexer.Context lctx = new(text);
            Parser.Context pctx = new();
            bool accept = false;
            bool pragma = false;
            bool include_end = false;

            int insertion_point = 0;
            int preproc = 0;
            int start = 0;

            Token tok = lctx.GetToken(accept, false);


            while (pctx.expected_tokens.Count != 0 && tok.valid())
            {
                TType expect = pctx.expected_tokens.Peek();

                if (expect >= TType.T0) //LL rules
                {
                    pctx.expected_tokens.Pop();
                    accept = LLTable(ref pctx.expected_tokens, tok.Type, expect);
                    continue;
                }
                if (!accept || expect != tok.Type)
                {
                    pctx.Clear(); // unexpected token, start anew

                    switch (tok.Type)
                    {
                        case TType.OpenBr: // if scope, probably function or class
                            FFWD(ref lctx, (int)pctx.Scope, (int)pctx.Scope + 1); break;
                        case TType.CloseBr:
                            pctx--; break;
                        case TType.Pragma:
                            pragma = true; tok = lctx.GetToken(true, false); continue;
                        case TType.ID:
                            if (pragma && tok.Value.SequenceEqual("once".AsSpan()))
                                insertion_point = tok.End; break;
                    }

                    preproc += tok.IsPreprocStart && includes.Any() ? 1 : 0;
                    preproc -= preproc>0?tok.IsPreprocEnd ? 1 : 0:0;

                    tok = lctx.GetToken(accept, false);
                    decl.type = TType.Null; //interference with enum{} class;
                    continue;
                }

                pctx.expected_tokens.Pop();


                if (!disable_count && !include_end
                && expect != TType.Include
                && expect != TType.AngleID
                && expect != TType.QuoteID
                && includes.Count > 0)
                { include_end = true; }

                switch (expect)
                {
                    case TType.Namespace:
                        pctx.Namespace = true;
                        if (!disable_ns)
                        {
                            start = tok.Position;
                            ns.scope = pctx.Scope;
                        }
                        break;
                    case TType.Class:
                        if (decl.type == TType.Enum)
                            decl.type = TType.EnumClass; //special case
                        else goto case TType.Struct;
                        break;
                    case TType.Struct:
                    case TType.Enum:
                        decl.type = tok.Type;
                        start = tok.Position;
                        break;
                    case TType.ID:
                        if (pctx.Namespace)
                            pctx.PushNamespace(tok.Value.ToString());
                        else
                            decl.ID = tok.Value.ToString();
                        break;
                    case TType.OpenBr:
                        if (pctx.Namespace)
                        {
                            if (!disable_ns)
                            {
                                ns.namespaces = pctx.GetNamespace();
                                ns.span = new(start, tok.Position - start);
                                namespaces.Add(ns);
                                ns = new();
                            }
                            pctx.Namespace = false;
                        }
                        break;
                    case TType.CloseBr:
                        pctx.PopNamespace(); break;
                    case TType.Include:
                        start = tok.Position;
                        break;
                    case TType.QuoteID:
                    case TType.AngleID:
                        var begin = tok.Position - start;
                        inc.FullFile = tok.Value.ToString();
                        inc.delimiter = tok.Type == TType.AngleID ? DelimiterMode.AngleBrackets : DelimiterMode.Quotes;
                        inc.span = new(start, tok.End - start);
                        inc.file_subspan = new(begin, tok.Value.Length); // subspan of file for replacement

                        if (!include_end && !disable_count && preproc == 0) insertion_point = tok.End;

                        includes.Add(inc);
                        inc = new();
                        break;
                    case TType.Semicolon:
                        decl.span = new(start, tok.End - start);
                        decl.namespaces = pctx.ns_tree.ToArray();
                        fwd.Add(decl);
                        decl = new();
                        break;
                    default:
                        break;
                }

                if (tok.Type == TType.OpenBr)
                    pctx++;
                if (tok.Type == TType.CloseBr)
                    pctx--;

                tok = lctx.GetToken(accept, false);
            }
            return new Output(namespaces, fwd, includes, insertion_point);
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
