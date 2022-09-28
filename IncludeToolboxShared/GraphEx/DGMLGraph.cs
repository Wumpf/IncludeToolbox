using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace IncludeToolbox.Graph
{
    /// <summary>
    /// A simple DGML graph file writer.
    /// </summary>
    public class DGMLGraph
    {
        public class Node
        {
            // Standard DGML attributes.
            [XmlAttribute]
            public string Id;
            [XmlAttribute]
            public string Label;
            [XmlAttribute]
            public string Background;
            [XmlAttribute]
            public string Group
            {
                get
                {
                    switch(GroupCollapse)
                    {
                        case GroupCollapseState.Expanded:
                            return "Expanded";
                        case GroupCollapseState.Collapsed:
                            return "Collapsed";
                    }
                    return null;
                }
                set { throw new NotSupportedException(); } // setter required for xml serialization
            }

            public enum GroupCollapseState
            {
                None,
                Expanded,
                Collapsed
            }

            [XmlIgnore]
            public GroupCollapseState GroupCollapse = GroupCollapseState.None;

            // Extra attributes.
            [XmlAttribute]
            public int NumIncludes = 0;
            [XmlAttribute]
            public int NumUniqueTransitiveChildren = 0;


            public bool ShouldSerializeNumIncludes()
            {
                return NumIncludes != 0;
            }
            public bool ShouldSerializeNumUniqueTransitiveChildren()
            {
                return NumUniqueTransitiveChildren != 0;
            }
        }

        public class Link
        {
            public enum LinkType
            {
                Normal,
                GroupContains
            }

            [XmlAttribute]
            public string Source;
            [XmlAttribute]
            public string Target;
            [XmlAttribute]
            public string Label;
            [XmlAttribute]
            public string Category
            {
                get { return Type == LinkType.Normal ? null : "Contains"; }
                set { throw new NotSupportedException(); } // setter required for xml serialization
            }


            [XmlIgnore]
            public LinkType Type = LinkType.Normal;
        }

        public List<Node> Nodes { get; private set; } = new List<Node>();
        public List<Link> Links { get; private set; } = new List<Link>();

        public DGMLGraph()
        {
        }

        public struct Graph
        {
            public Node[] Nodes;
            public Link[] Links;
        }

        public void Serialize(string xmlpath)
        {
            Graph g = new Graph();
            g.Nodes = Nodes.ToArray();
            g.Links = Links.ToArray();

            XmlRootAttribute root = new XmlRootAttribute("DirectedGraph");
            root.Namespace = "http://schemas.microsoft.com/vs/2009/dgml";
            XmlSerializer serializer = new XmlSerializer(typeof(Graph), root);
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter xmlWriter = XmlWriter.Create(xmlpath, settings))
            {
                serializer.Serialize(xmlWriter, g);
            }
        }

        public void ColorizeByTransitiveChildCount(System.Drawing.Color noChildrenColor, System.Drawing.Color maxChildrenColor)
        {
            int maxChildCount = Nodes.Max(x => x.NumUniqueTransitiveChildren);
            if (maxChildCount == 0)
                maxChildCount = 1; // Avoid division by zero later on.

            foreach(var node in Nodes)
            {
                float intensity = (float)node.NumUniqueTransitiveChildren / maxChildCount;
                System.Drawing.Color color = System.Drawing.Color.FromArgb
                (
                    red: (int)(noChildrenColor.R + (maxChildrenColor.R - noChildrenColor.R) * intensity + 0.5f),
                    green: (int)(noChildrenColor.G + (maxChildrenColor.G - noChildrenColor.G) * intensity + 0.5f),
                    blue: (int)(noChildrenColor.B + (maxChildrenColor.B - noChildrenColor.B) * intensity + 0.5f)
                );

                node.Background = System.Drawing.ColorTranslator.ToHtml(color);
            }
        }
    }
}
