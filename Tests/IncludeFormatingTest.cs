using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IncludeToolbox.Formatter;

namespace Tests
{
    [TestClass]
    public class IncludeFormatingTest
    {
        private static string sourceCode_NoBlanks =
@"#include ""a.h""
#include <b.hpp>
#include ""a.h""
#include ""filename.h""
#include <b.hpp>
#include <d_firstanyways>
#include <e_secondanyways>
#include <e_secondanyways>
#include <c.hpp>
#include <d_firstanyways>";

        private static string sourceCode_WithBlanks =
@"#include ""c_third""

#include ""filename.h""

#include ""z_first""

#include <b_second>
#include <b_second>
// A comment
#include ""z_first""

#include <b_second>
#include ""filename.h""";

        [TestMethod]
        public void Sorting_BlanksAfterRegexGroup()
        {
            // Blanks after groups.
            string expectedFormatedCode_NoBlanks =
@"#include ""filename.h""

#include <d_firstanyways>
#include <e_secondanyways>

#include ""a.h""
#include <b.hpp>
#include <c.hpp>



";

            string expectedFormatedCode_WithBlanks =
@"#include ""filename.h""

#include <b_second>

#include ""c_third""

#include ""z_first""

// A comment



";


            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.None;
            settings.PrecedenceRegexes = new string[]
                {
                    IncludeToolbox.RegexUtils.CurrentFileNameKey,
                    ".+_.+"
                };
            settings.BlankAfterRegexGroupMatch = true;
            settings.RemoveEmptyLines = false;


            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_NoBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_NoBlanks, formatedCode);
            formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }

        [TestMethod]
        public void Sorting_AngleBracketsFirst()
        {
            // With sort by type.
            string expectedFormatedCode_NoBlanks =
@"#include <d_firstanyways>
#include <e_secondanyways>
#include <b.hpp>
#include <c.hpp>
#include ""filename.h""
#include ""a.h""



";


            string expectedFormatedCode_WithBlanks =
@"#include <b_second>

#include ""filename.h""

#include ""c_third""

#include ""z_first""

// A comment



";


            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.AngleBracketsFirst;
            settings.PrecedenceRegexes = new string[]
            {
                    IncludeToolbox.RegexUtils.CurrentFileNameKey,
                    ".+_.+"
            };
            settings.BlankAfterRegexGroupMatch = false;
            settings.RemoveEmptyLines = false;

            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_NoBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_NoBlanks, formatedCode);
            formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }

        [TestMethod]
        public void Sorting_DontRemoveDuplicates()
        {
            // With sort by type.
            string expectedFormatedCode_NoBlanks =
@"#include ""filename.h""

#include <d_firstanyways>
#include <d_firstanyways>
#include <e_secondanyways>
#include <e_secondanyways>

#include ""a.h""
#include ""a.h""
#include <b.hpp>
#include <b.hpp>
#include <c.hpp>";


            string expectedFormatedCode_WithBlanks =
@"#include ""filename.h""
#include ""filename.h""

#include <b_second>
#include <b_second>
#include <b_second>
// A comment
#include ""c_third""
#include ""z_first""
#include ""z_first""";


            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.None;
            settings.PrecedenceRegexes = new string[]
            {
                    IncludeToolbox.RegexUtils.CurrentFileNameKey,
                    ".+_.+"
            };
            settings.BlankAfterRegexGroupMatch = true;
            settings.RemoveEmptyLines = true;
            settings.RemoveDuplicates = false;

            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_NoBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_NoBlanks, formatedCode);
            formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }

        [TestMethod]
        public void RemoveEmptyLines()
        {
            string expectedFormatedCode_WithBlanks =
@"#include ""filename.h""
#include <b_second>
#include ""c_third""
#include ""z_first""
// A comment";

            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.None;
            settings.PrecedenceRegexes = new string[] { IncludeToolbox.RegexUtils.CurrentFileNameKey };
            settings.BlankAfterRegexGroupMatch = false;
            settings.RemoveEmptyLines = true;

            string formatedCode = IncludeFormatter.FormatIncludes(sourceCode_WithBlanks, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode_WithBlanks, formatedCode);
        }

        [TestMethod]
        public void EmptySelection()
        {
            // Activate all features
            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.AngleBracketsFirst;
            settings.PrecedenceRegexes = new string[] { IncludeToolbox.RegexUtils.CurrentFileNameKey };
            settings.BlankAfterRegexGroupMatch = true;
            settings.RemoveEmptyLines = true;
            settings.DelimiterFormatting = IncludeToolbox.FormatterOptionsPage.DelimiterMode.AngleBrackets;
            settings.SlashFormatting = IncludeToolbox.FormatterOptionsPage.SlashMode.BackSlash;

            string formatedCode = IncludeFormatter.FormatIncludes("", "filename.cpp", new string[] { }, settings);
            Assert.AreEqual("", formatedCode);
        }

        [TestMethod]
        public void OtherPreprocessorDirectives()
        {
            string source =
@"#pragma once
// SomeComment
#include ""z""
#include <b>

#include ""filename.h""

#if test
#include <d>
// A comment
#include ""a9""
#include <d>
#include <c>
#else
#include <d>

#include <a3>   // comment
//#endif

#include <a2>
#endif
#include <b>
#include <a1>";

            string expectedFormatedCode =
@"#pragma once
// SomeComment
#include <b>
#include ""filename.h""

#include ""z""

#if test
#include <c>
// A comment
#include <d>
#include ""a9""

#else
#include <a2>

#include <a3>   // comment
//#endif

#include <d>
#endif
#include <a1>
#include <b>";

            var settings = new IncludeToolbox.FormatterOptionsPage();
            settings.SortByType = IncludeToolbox.FormatterOptionsPage.TypeSorting.AngleBracketsFirst;
            settings.PrecedenceRegexes = new string[] { IncludeToolbox.RegexUtils.CurrentFileNameKey };
            settings.BlankAfterRegexGroupMatch = false;
            settings.RemoveEmptyLines = false;

            string formatedCode = IncludeFormatter.FormatIncludes(source, "filename.cpp", new string[] { }, settings);
            Assert.AreEqual(expectedFormatedCode, formatedCode);
        }
}
}
