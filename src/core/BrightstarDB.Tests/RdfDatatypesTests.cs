using System;
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

#if !NETCOREAPP10
    [TestFixture("en-US")]
    [TestFixture("de")]
    [TestFixture("ja-JP")]
    public class LiteralRoundtripTests
    {
        private readonly CultureInfo _testCulture;
        private CultureInfo _oldCulture;
        private CultureInfo _oldUiCulture;

        public LiteralRoundtripTests(string culture)
        {
            _testCulture = new CultureInfo(culture);// CultureInfo.GetCultureInfo(culture);
        }

        [SetUp]
        public void SetUp()
        {
            _oldCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            _oldUiCulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            System.Threading.Thread.CurrentThread.CurrentCulture =
                System.Threading.Thread.CurrentThread.CurrentUICulture = _testCulture;
        }

        [TearDown]
        public void TearDown()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = _oldCulture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = _oldUiCulture;
        }

        [Test]
        public void TestBooleanRoundtrip()
        {
            var bsString = RdfDatatypes.GetLiteralString(true);
            Assert.That(RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Boolean, null), Is.True);
            bsString = RdfDatatypes.GetLiteralString(false);
            Assert.That(RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Boolean, null), Is.False);
        }

        [Test]
        public void TestDateTimeRoundtrip()
        {
            var v = new DateTime(2012, 03, 04, 09, 10, 11, 25, DateTimeKind.Utc);
            var bsString = RdfDatatypes.GetLiteralString(v);
            var parsed = RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.DateTime, null);
            Assert.That(parsed, Is.EqualTo(v));
        }

        [Test]
        public void TestDoubleRoundtrip()
        {
            const double dbl = 1.123456789D;
            var bsString = RdfDatatypes.GetLiteralString(dbl);
            var bsParsed = (double)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Double, null);
            Assert.That(bsParsed, Is.EqualTo(dbl));
        }

        [Test]
        public void TestIntegerRoundtrip()
        {
            const int v = -123456789;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (int)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Integer, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestNonNegativeIntegerParsing()
        {
            const int v = 123456789;
            var bsParsed = (int)RdfDatatypes.ParseLiteralString("123456789", RdfDatatypes.NonNegativeInteger, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestPositiveIntegerParsing()
        {
            const int v = 123456789;
            var bsParsed = (int)RdfDatatypes.ParseLiteralString("123456789", RdfDatatypes.PositiveInteger, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestNonPositiveIntegerParsing()
        {
            const int v = -123456789;
            var bsParsed = (int)RdfDatatypes.ParseLiteralString("-123456789", RdfDatatypes.NonPositiveInteger, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestNegativeIntegerParsing()
        {
            const int v = -123456789;
            var bsParsed = (int)RdfDatatypes.ParseLiteralString("-123456789", RdfDatatypes.NegativeInteger, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestFloatRoundtrip()
        {
            const float v = 1.12345F;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (float)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Float, null);
            Assert.That(bsParsed, Is.EqualTo(v));
            
        }

        [Test]
        public void TestLongRoundtrip()
        {
            const long v = -9876543210;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (long)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Long, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestByteRoundtrip()
        {
            const sbyte v = -64;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (sbyte)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Byte, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestDecimalRoundtrip()
        {
            const decimal d = 1.123456789M;
            var bsString = RdfDatatypes.GetLiteralString(d);
            var bsParsed = (decimal)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Decimal, null);
            Assert.That(bsParsed, Is.EqualTo(d));
        }

        [Test]
        public void TestShortRoundtrip()
        {
            const short v = -16384;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (short)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.Short, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestUnsignedLongRoundtrip()
        {
            const ulong v = 9876543210L;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (ulong)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.UnsignedLong, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestUnsignedIntegerRoundtrip()
        {
            const uint v = 123456789;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (uint)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.UnsignedInteger, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestUnsignedShortRoundtrip()
        {
            const ushort v = 64000;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (ushort)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.UnsignedShort, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestUnsignedByteRoundtrip()
        {
            const byte v = 244;
            var bsString = RdfDatatypes.GetLiteralString(v);
            var bsParsed = (byte)RdfDatatypes.ParseLiteralString(bsString, RdfDatatypes.UnsignedByte, null);
            Assert.That(bsParsed, Is.EqualTo(v));
        }

        [Test]
        public void TestParseStringDerivatives()
        {
            const string v = "HelloWorld";
            var p = RdfDatatypes.ParseLiteralString(v, RdfDatatypes.NormalizedString, null);
            Assert.That(p, Is.EqualTo(v));
            p = RdfDatatypes.ParseLiteralString(v, RdfDatatypes.Token, null);
            Assert.That(p, Is.EqualTo(v));
            p = RdfDatatypes.ParseLiteralString(v, RdfDatatypes.Language, null);
            Assert.That(p, Is.EqualTo(v));
        }
    }
#endif
}
