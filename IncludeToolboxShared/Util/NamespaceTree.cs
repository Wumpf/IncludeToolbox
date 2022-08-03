using System;
using System.Collections.Generic;
using System.Linq;
using static IncludeToolbox.Lexer;

namespace IncludeToolbox
{
    public class DeclNode
    {
        private readonly TType _type;
        private readonly Dictionary<string, DeclNode> _children = new();

        public DeclNode(TType ty = TType.Null)
        {
            _type = ty;
        }

        public void AddChild(IEnumerable<string> namespaces, FWDDecl decl)
        {
            if (namespaces.Count() == 0)
            {
                _children.Add(decl.ID, new(decl.type));
                return;
            }
            if (!_children.ContainsKey(namespaces.First()))
                _children.Add(namespaces.First(), new DeclNode(TType.Namespace));
            _children[namespaces.First()].AddChild(namespaces.Skip(1), decl);
        }
        public void AddChild(FWDDecl value)
        {
            var x = value.namespaces;
            AddChild(x.Reverse().Skip(1), value);
        }
        public void AddChildren(IEnumerable<FWDDecl> decls)
        {
            foreach (var declsItem in decls)
                AddChild(declsItem);
        }

        private string Type()
        {
            return _type switch
            {
                TType.Namespace => "namespace",
                TType.Class => "class",
                TType.Struct => "struct",
                TType.Enum => "enum",
                TType.EnumClass => "enum class",
                _ => "",
            };
        }


        private string ToString(bool c17, string s)
        {
            var o = "";
            if (_type != TType.Namespace)
                return $"{Type()} {s};\n";

            if (c17 && _children.Count == 1 && _children.First().Value._type == TType.Namespace)
                return $"namespace {s}::{ new string(_children.First().Value.ToString(c17, _children.First().Key).Skip(10).ToArray())}\n";

            foreach (var item in _children)
                o += '\t' + item.Value.ToString(c17, item.Key);
            return $"namespace {s}\n{{\n{o}}}\n";
        }
        public string ToString(bool c17)
        {
            var o = "";
            foreach (var item in _children)
                o += item.Value.ToString(c17, item.Key);
            return o;
        }
    }
}
