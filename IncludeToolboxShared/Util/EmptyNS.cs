using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using static IncludeToolbox.Lexer;

namespace IncludeToolbox
{
    internal struct NSTracker
    {
        Stack<KeyValuePair<string_view, bool>> nsscan = new();
        string_view ns = new();
        bool empty = true;

        public int Start { get => ns.begin; set => ns.begin = value; }
        public bool Empty { get => empty; set => empty = value; }

        public NSTracker()
        {
        }

        public void Push()
        {
            nsscan.Push(new(ns, empty));
            empty = true;
        }
        public Span Pop(int end)
        {
            var v = nsscan.Pop();
            empty = v.Value && empty;
            var s = ns;
            s.end = end + 1;
            ns = v.Key;
            return s.AsSpan();
        }
        public void Drop()
        {
            if (nsscan.Count > 0)
                nsscan.Pop();
            empty = false;
        }
    }

    internal static partial class Parser
    {
        static bool LLTableEN(ref Stack<TType> context, TType input, TType current)
        {
            switch (input)
            {
                case TType.Namespace:
                    switch (current)
                    {
                        case TType.T0:
                            context.Push(TType.T0);
                            context.Push(TType.T1);
                            return true;
                        case TType.T1:
                        case TType.T5:
                            context.Push(TType.CloseBr);
                            context.Push(TType.T3);
                            context.Push(TType.OpenBr);
                            context.Push(TType.T2);
                            context.Push(TType.Namespace);
                            return true;
                        case TType.T3:
                            context.Push(TType.T3);
                            context.Push(TType.T5);
                            return true;
                    }
                    break;
                case TType.OpenBr:
                    return current == TType.T2 || current == TType.T4;
                case TType.ID:
                    if (current == TType.T2)
                    {
                        context.Push(TType.T4);
                        context.Push(TType.ID);
                        return true;
                    }
                    break;
                case TType.Colon:
                    if (current == TType.T4)
                    {
                        context.Push(TType.T4);
                        context.Push(TType.ID);
                        context.Push(TType.Colon);
                        return true;
                    }
                    break;
                case TType.CloseBr:
                    return current == TType.T3;
                default:
                    break;
            }
            return false;
        }
        public static Span[] ParseEmptyNamespaces(string text)
        {
            List<Span> namespaces = new();
            Parser.Context pctx = new();
            Lexer.Context lctx = new(text.AsSpan());
            Token tok = lctx.GetToken(false);

            NSTracker tracker = new();

            bool accept = false;

            while (pctx.expected_tokens.Count != 0 && tok.valid())
            {
                TType expect = pctx.expected_tokens.Peek();

                if (expect >= TType.T0) //LL rules
                {
                    pctx.expected_tokens.Pop();
                    accept = LLTableEN(ref pctx.expected_tokens, tok.type, expect);
                    continue;
                }
                if (!accept && expect != tok.type)
                {
                    tracker.Empty = false;
                    pctx.Clear(); // unexpected token, start anew
                    if (tok.type == TType.OpenBr) // if scope, probably function or class
                        FFWD(ref lctx, (int)pctx.Scope, (int)pctx.Scope + 1);
                    if (tok.type == TType.CloseBr)
                    {
                        pctx--;
                        tracker.Drop();
                    }
                    tok = lctx.GetToken(accept);
                    continue;
                }

                pctx.expected_tokens.Pop();

                switch (expect)
                {
                    case TType.Namespace:
                        tracker.Push();
                        tracker.Start = tok.Position;
                        break;
                    case TType.CloseBr:
                        if (tracker.Empty)
                        {
                            var c = tracker.Pop(tok.Position);
                            namespaces.RemoveAll(x => c.Contains(x));
                            namespaces.Add(c);
                        }
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

            return namespaces.ToArray();
        }
    }
}
