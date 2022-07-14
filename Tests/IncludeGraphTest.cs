//using System.Linq;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using IncludeToolbox;
//using System.IO;
//using IncludeToolbox.Graph;
//using IncludeToolbox.Formatter;
//using System.Collections.Generic;

//namespace Tests
//{
//    [TestClass]
//    public class IncludeGraphTest
//    {
//        [TestMethod]
//        public void WriteSimpleDGML()
//        {
//            string filenameTestOutput = "testdata/output.dgml";
//            string filenameComparision= "testdata/simplegraph.dgml";
//            try
//            {
//                File.Delete(filenameTestOutput);
//            }
//            catch { }

//            var writer = new DGMLGraph();
//            writer.Nodes.Add(new DGMLGraph.Node { Id = "0", Label = "0" });
//            writer.Nodes.Add(new DGMLGraph.Node { Id = "1", Label = "1label" });
//            writer.Nodes.Add(new DGMLGraph.Node { Id = "2", Label = "2" });
//            writer.Nodes.Add(new DGMLGraph.Node { Id = "3", Label = "3" });
//            writer.Links.Add(new DGMLGraph.Link { Source = "0", Target = "1" });
//            writer.Links.Add(new DGMLGraph.Link { Source = "1", Target = "0", Label = "backlink"});
//            writer.Links.Add(new DGMLGraph.Link { Source = "1", Target = "1", Label = "loop" });
//            writer.Links.Add(new DGMLGraph.Link { Source = "2", Target = "3", });
//            writer.Links.Add(new DGMLGraph.Link { Source = "4", Target = "3", });
//            writer.Links.Add(new DGMLGraph.Link { Source = "2", Target = "0", });
//            writer.Serialize(filenameTestOutput);

//            string expectedFile = File.ReadAllText(filenameComparision);
//            string writtenFile = File.ReadAllText(filenameTestOutput);

//            Assert.AreEqual(expectedFile, writtenFile);

//            // For a clean environment!
//            File.Delete(filenameTestOutput);
//        }

//        [TestMethod]
//        public void CustomGraphParse()
//        {
//            string[] noParseDirectories = new[] { Utils.GetExactPathName("testdata/subdir/subdir") };

//            IncludeGraph graph = new IncludeGraph();
//            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/source0.cpp"), Enumerable.Empty<string>(), noParseDirectories);
//            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/source1.cpp"), Enumerable.Empty<string>(), noParseDirectories);
//            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/testinclude.h"), Enumerable.Empty<string>(), noParseDirectories); // Redundancy shouldn't matter.

//            // Check items.
//            Assert.AreEqual(7, graph.GraphItems.Count);
//            bool newItem = false;
//            var source0 = graph.CreateOrGetItem("testdata/source0.cpp", out newItem);
//            Assert.AreEqual(false, newItem);
//            var source1 = graph.CreateOrGetItem("testdata/source1.cpp", out newItem);
//            Assert.AreEqual(false, newItem);
//            var testinclude = graph.CreateOrGetItem("testdata/testinclude.h", out newItem);
//            Assert.AreEqual(false, newItem);
//            var subdir_testinclude = graph.CreateOrGetItem("testdata/subdir/teStinclude.h", out newItem);
//            Assert.AreEqual(false, newItem);
//            var subdir_inline = graph.CreateOrGetItem("testdata/subdir/inline.inl", out newItem);
//            Assert.AreEqual(false, newItem);
//            var subdirsubdir_subsub = graph.CreateOrGetItem("testdata/subdir/subdir/subsub.h", out newItem);
//            Assert.AreEqual(false, newItem);
//            var broken = graph.CreateOrGetItem("broken!", out newItem);
//            Assert.AreEqual(false, newItem);

//            // Check includes in source0.
//            Assert.AreEqual(2, source0.Includes.Count);
//            Assert.AreEqual(0, source0.Includes[0].IncludeLine.LineNumber); // Different line numbers.
//            Assert.AreEqual(5, source0.Includes[1].IncludeLine.LineNumber);
//            Assert.AreEqual(subdir_testinclude, source0.Includes[0].IncludedFile);  // But point to the same include.
//            Assert.AreEqual(subdir_testinclude, source0.Includes[1].IncludedFile);

//            // Check includes in source1.
//            Assert.AreEqual(1, source1.Includes.Count);
//            Assert.AreEqual(testinclude, source1.Includes[0].IncludedFile);

//            // Check includes in testinclude.
//            Assert.AreEqual(1, testinclude.Includes.Count);
//            Assert.AreEqual(subdir_testinclude, testinclude.Includes[0].IncludedFile);

//            // Check includes in subdir_testinclude.
//            Assert.AreEqual(2, subdir_testinclude.Includes.Count);
//            Assert.AreEqual(subdir_inline, subdir_testinclude.Includes[0].IncludedFile);
//            Assert.AreEqual(subdirsubdir_subsub, subdir_testinclude.Includes[1].IncludedFile);

//            // Check includes in subdir_inline.
//            Assert.AreEqual(1, subdir_inline.Includes.Count);
//            Assert.AreEqual(broken, subdir_inline.Includes[0].IncludedFile);
//            Assert.AreEqual(true, subdir_inline.Includes[0].IncludeLine.ContainsActiveInclude);

//            // Check includes in subdirsubdir_subsub - should be empty since we have this dir on the ignore list.
//            Assert.AreEqual(0, subdirsubdir_subsub.Includes.Count);

//            // Check item representing a unresolved include.
//            Assert.AreEqual(0, broken.Includes.Count);
//        }

//        private void RemoveAbsolutePathsFromDGML(DGMLGraph dgml, IEnumerable<string> includeDirectories)
//        {
//            foreach (var item in dgml.Nodes)
//            {
//                item.Id = IncludeFormatter.FormatPath(item.Id, FormatterOptionsPage.PathMode.Shortest, includeDirectories);
//                item.Label = IncludeFormatter.FormatPath(item.Label, FormatterOptionsPage.PathMode.Shortest, includeDirectories);
//            }
//            foreach (var link in dgml.Links)
//            {
//                link.Source = IncludeFormatter.FormatPath(link.Source, FormatterOptionsPage.PathMode.Shortest, includeDirectories);
//                link.Target = IncludeFormatter.FormatPath(link.Target, FormatterOptionsPage.PathMode.Shortest, includeDirectories);
//            }
//        }

//        [TestMethod]
//        public void CustomGraphParseToDGML_NoGrouping()
//        {
//            CustomGraphParseToDGML(false);
//        }

//        [TestMethod]
//        public void CustomGraphParseToDGML_WithGrouping()
//        {
//            CustomGraphParseToDGML(true);
//        }

//        public void CustomGraphParseToDGML(bool grouping)
//        {
//            string filenameTestOutput = "testdata/output.dgml";
//            string filenameComparision = grouping ? "testdata/includegraph_grouped.dgml" : "testdata/includegraph.dgml";
//            string filenameComparision_color = grouping ? "testdata/includegraph_withcolors_grouped.dgml" : "testdata/includegraph_withcolors.dgml";

//            string[] noParseDirectories = new[] { Utils.GetExactPathName("testdata/subdir/subdir") };

//            IncludeGraph graph = new IncludeGraph();
//            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/source0.cpp"), Enumerable.Empty<string>(), noParseDirectories);
//            graph.AddIncludesRecursively_ManualParsing(Utils.GetExactPathName("testdata/source1.cpp"), Enumerable.Empty<string>(), noParseDirectories);

//            // Formatting...
//            var includeDirectories = new[] { Path.Combine(System.Environment.CurrentDirectory, "testdata") };
//            foreach (var item in graph.GraphItems)
//                item.FormattedName = IncludeFormatter.FormatPath(item.AbsoluteFilename, FormatterOptionsPage.PathMode.Shortest, includeDirectories);

//            // To DGML and save.
//            // Since we don't want to have absolute paths in our compare/output dgml we hack the graph before writing it out.
//            var dgml = graph.ToDGMLGraph(grouping, true);
//            RemoveAbsolutePathsFromDGML(dgml, new[] { System.Environment.CurrentDirectory });

//            // Without colors.
//            {
//                dgml.Serialize(filenameTestOutput);
//                string expectedFile = File.ReadAllText(filenameComparision);
//                string writtenFile = File.ReadAllText(filenameTestOutput);
//                Assert.AreEqual(expectedFile, writtenFile);
//            }

//            // With colors.
//            {
//                dgml.ColorizeByTransitiveChildCount(System.Drawing.Color.White, System.Drawing.Color.Black);
//                dgml.Serialize(filenameTestOutput);
//                string expectedFile = File.ReadAllText(filenameComparision_color);
//                string writtenFile = File.ReadAllText(filenameTestOutput);
//                Assert.AreEqual(expectedFile, writtenFile);
//            }

//            // For a clean environment!
//            File.Delete(filenameTestOutput);
//        }

//        [TestMethod]
//        public void IncludeFolderGrouping()
//        {
//            IncludeGraph graph = new IncludeGraph();
//            string sourceFile = Utils.GetExactPathName("testdata/source0.cpp");
//            graph.AddIncludesRecursively_ManualParsing(sourceFile, Enumerable.Empty<string>(), new string[] { });

//            var root = new IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Root(graph.GraphItems, graph.CreateOrGetItem(sourceFile, out bool isNew));
//            var children = root.Children;

//            // Check if tree is as expected.
//            {
//                Assert.AreEqual(2, children.Count);

//                var unresolvedFolder = children.First(x => x.Name == "<unresolved>");
//                Assert.IsNotNull(unresolvedFolder);
//                Assert.IsInstanceOfType(unresolvedFolder, typeof(IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Folder));

//                var subdirFolder = children.First(x => x.Name.EndsWith("testdata\\subdir\\"));
//                Assert.IsTrue(Path.IsPathRooted(subdirFolder.Name));
//                Assert.IsNotNull(unresolvedFolder);
//                Assert.IsInstanceOfType(unresolvedFolder, typeof(IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Folder));

//                // subdir folder
//                {
//                    Assert.AreEqual(3, subdirFolder.Children.Count);

//                    var testinclude = subdirFolder.Children.First(x => x.Name == "testinclude.h");
//                    Assert.IsNotNull(testinclude);
//                    Assert.IsInstanceOfType(testinclude, typeof(IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Leaf));

//                    var inline = subdirFolder.Children.First(x => x.Name == "inline.inl");
//                    Assert.IsNotNull(inline);
//                    Assert.IsInstanceOfType(inline, typeof(IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Leaf));

//                    var subdirsubdirFolder = subdirFolder.Children.First(x => x.Name == "subdir\\");
//                    Assert.IsNotNull(subdirsubdirFolder);
//                    Assert.IsInstanceOfType(subdirsubdirFolder, typeof(IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Folder));

//                    // subdir\subdir folder
//                    {
//                        Assert.AreEqual(1, subdirsubdirFolder.Children.Count);

//                        var subsubh = subdirsubdirFolder.Children.First(x => x.Name == "subsub.h");
//                        Assert.IsNotNull(subsubh);
//                        Assert.IsInstanceOfType(subsubh, typeof(IncludeToolbox.GraphWindow.FolderIncludeTreeViewItem_Leaf));
//                    }
//                }
//            }
//        }
//    }
//}
