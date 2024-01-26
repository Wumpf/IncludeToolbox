using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IncludeToolbox;

namespace Tests
{
    internal class IncludeParseTests
    {
        [Test]
        public void TestBasic()
        {
            var text = "#include <hell>";
            var lines = Parser.ParseInclues(text, true);
            Assert.IsTrue(lines.Any());
            var line = lines[0];
            Assert.IsTrue(line.keep == false);
            Assert.IsTrue(line.FullFile.SequenceEqual("<hell>"));
            Assert.IsTrue(line.Project(text).SequenceEqual(text));
        }

        [Test]
        public void TestBasicNewline()
        {
            var text = "#include <hell>\r\n //gigachad";
            var lines = Parser.ParseInclues(text, true);
            Assert.IsTrue(lines.Any());
            var line = lines[0];
            Assert.IsTrue(line.keep == false);
            Assert.IsTrue(line.FullFile.SequenceEqual("<hell>"));
            Assert.IsTrue(line.Project(text).SequenceEqual("#include <hell>\r\n"));
            
            var text2 = "#include <hell> //gigachad";
            var lines2 = Parser.ParseInclues(text2, true);
            Assert.IsTrue(lines.Any());
            var line2 = lines2[0];
            Assert.IsTrue(line2.keep == false);
            Assert.IsTrue(line2.FullFile.SequenceEqual("<hell>"));
            Assert.IsTrue(line2.Project(text2).SequenceEqual(text2));
            
            var text3 = "#include <hell> //gigachad\n";
            var lines3 = Parser.ParseInclues(text3, true);
            Assert.IsTrue(lines.Any());
            var line3 = lines3[0];
            Assert.IsTrue(line3.keep == false);
            Assert.IsTrue(line3.FullFile.SequenceEqual("<hell>"));
            Assert.IsTrue(line3.Project(text3).SequenceEqual(text3));
        }

        [Test]
        public void TestSpan()
        {
            var text = "   #include <hell>\r\n#include <hell>";
            var lines = Parser.ParseInclues(text[3..], true);
            Assert.IsTrue(lines.Length == 2);
            var line = lines[1];
            Assert.IsTrue(line.keep == false);
            Assert.IsTrue(line.FullFile.SequenceEqual("<hell>"));
            var offs = line.ReplaceSpan(3);
            Assert.IsTrue(text.Substring(offs.Start, offs.Length).SequenceEqual("#include <hell>"));
            
            var text2 = "   #include <hell>\r\n#include <hell> //Giga \n";
            var lines2 = Parser.ParseInclues(text2[3..], true);
            Assert.IsTrue(lines2.Length == 2);
            var line2 = lines2[1];
            Assert.IsTrue(line2.keep == false);
            Assert.IsTrue(line2.FullFile.SequenceEqual("<hell>"));
            var offs2 = line2.ReplaceSpan(3);
            Assert.IsTrue(text2.Substring(offs2.Start, offs2.Length).SequenceEqual("#include <hell> //Giga \n"));
        }
    }
}
