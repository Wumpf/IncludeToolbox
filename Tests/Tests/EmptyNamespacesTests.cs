using IncludeToolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    internal class EmptyNamespacesTests
    {
        static readonly string V = "namespace Core\n{\nclass CdwkAttributeData;\nclass Command;\n}\n\n#ifdef CACHE_OBJECTS\n#include <App/Layer.h>\n#endif\n\n\nnamespace Core\n{\n\n}\n";
        
        [Test]
        public void TestRealistic()
        {
            var text = V;
            var lines = Parser.ParseEmptyNamespaces(text);
            var lines2 = Parser.Parse(text);
            Assert.IsTrue(lines.Any());
            Assert.That(lines.Length, Is.EqualTo(1));
            Assert.That(lines[0].Start, Is.EqualTo(lines2.Namespaces[1].span.Start));
        }
    }
}
