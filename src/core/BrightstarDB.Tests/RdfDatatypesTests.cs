using System.Globalization;
using BrightstarDB.Rdf;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class RdfDatatypesTests
    {
        [Test]
        public void TestRoundtripFormatParsing()
        {
            var dbl = 1.123456789;
            var literalIn = dbl.ToString("R", CultureInfo.InvariantCulture);
            var dblOut = (double)RdfDatatypes.ParseLiteralString(literalIn, RdfDatatypes.Double, null);
            Assert.That(dblOut, Is.EqualTo(dbl));
        }

        [Test]
        public void TestStandardDoubleParsing()
        {
            var dbl = 1.123456789;
            var literalIn = dbl.ToString(CultureInfo.InvariantCulture);
            var dblOut = (double)RdfDatatypes.ParseLiteralString(literalIn, RdfDatatypes.Double, null);
            Assert.That(dblOut, Is.EqualTo(dbl));
        }
        
    }
}
