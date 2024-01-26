using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using static IncludeToolbox.Lexer;

namespace IncludeToolbox
{
    internal struct NSTracker
    {
        Stack<KeyValuePair<int, bool>> nsscan = new();
        public int Start { get; set ; } = 0;
        public bool Empty { get; set; } = true;

        public NSTracker()
        {
        }

        public void Push()
        {
            nsscan.Push(new(Start, Empty));
            Empty = true;
        }
        public Span Pop(int end)
        {
            var v = nsscan.Pop();
            Empty = v.Value && Empty;
            return new(v.Key, end - v.Key);
        }
        public void Drop()
        {
            if (nsscan.Count > 0)
                nsscan.Pop();
            Empty = false;
        }
    }

    public static partial class Parser
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
                    accept = LLTableEN(ref pctx.expected_tokens, tok.Type, expect);
                    continue;
                }
                if (!accept && expect != tok.Type)
                {
                    tracker.Empty = false;
                    pctx.Clear(); // unexpected token, start anew
                    if (tok.Type == TType.OpenBr) // if scope, probably function or class
                        FFWD(ref lctx, (int)pctx.Scope, (int)pctx.Scope + 1);
                    if (tok.Type == TType.CloseBr)
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
                        tracker.Start = tok.Position;
                        tracker.Push();
                        break;
                    case TType.CloseBr:
                        if (tracker.Empty)
                        {
                            var c = tracker.Pop(tok.End);
                            namespaces.RemoveAll(x => c.Contains(x));
                            namespaces.Add(c);
                        }
                        break;
                    default:
                        break;
                }

                if (tok.Type == TType.OpenBr)
                    pctx++;
                if (tok.Type == TType.CloseBr)
                    pctx--;
                tok = lctx.GetToken(accept);
            }

            return namespaces.ToArray();
        }
    }
}
