using IncludeToolbox;
using static IncludeToolbox.Lexer;

namespace Tests
{
    public class LexTests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void TestBasicToken()
        {
            Context lexer = new("hello\n");

            var token = lexer.GetToken(true);
            Assert.IsTrue(token.Value.SequenceEqual("hello"));
            token = lexer.GetToken(true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\n"));
        }

        [Test]
        public void TestToken()
        {
            Context lexer = new("class B;\r\n // assault rifle");

            var token = lexer.GetToken(true);
            Assert.IsTrue(token.Type == TType.Class);
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.ID && token.Value.SequenceEqual("B"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Semicolon);
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\r\n"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Commentary && token.Value.SequenceEqual("// assault rifle"));
        }

        [Test]
        public void TestCommentaries()
        {
            Context lexer = new("/*class \n\n B;\r\n*/\r\n// assault rifle\r\n");
            var token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.MLCommentary && token.Value.SequenceEqual("/*class \n\n B;\r\n*/"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\r\n"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Commentary && token.Value.SequenceEqual("// assault rifle"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\r\n"));
        }

        [Test]
        public void TestNewlines()
        {
            Context lexer = new("\n \n  \r\n \r \n");
            var token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\n") && token.Position == 0);
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\n"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\r\n"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\r"));
            token = lexer.GetToken(true, true, true, true);
            Assert.IsTrue(token.Type == TType.Newline && token.Value.SequenceEqual("\n"));
        }
    }
}