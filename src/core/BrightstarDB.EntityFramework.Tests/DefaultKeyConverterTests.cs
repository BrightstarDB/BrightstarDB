using BrightstarDB.EntityFramework.Tests.ContextObjects;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class DefaultKeyConverterTests
    {
        [Test]
        public void TestConvertInt()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.Convert(42), Is.EqualTo("42"));
            Assert.That(converter.Convert(-42), Is.EqualTo("-42")); // Leading minus sign
            Assert.That(converter.Convert(6000000), Is.EqualTo("6000000")); // No thousands separators
        }

        [Test]
        public void TestConvertUInt()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.Convert(42u), Is.EqualTo("42"));
            Assert.That(converter.Convert(6000000u), Is.EqualTo("6000000")); // No thousands separators
        }

        [Test]
        public void TestConvertLong()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.Convert(42L), Is.EqualTo("42"));
            Assert.That(converter.Convert(-42L), Is.EqualTo("-42")); // Leading minus sign
            Assert.That(converter.Convert(6000000L), Is.EqualTo("6000000")); // No thousands separators
        }

        [Test]
        public void TestConvertUlong()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.Convert(42ul), Is.EqualTo("42"));
            Assert.That(converter.Convert(6000000ul), Is.EqualTo("6000000")); // No thousands separators
        }

        [Test]
        public void TestConvertDecimal()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.Convert(42m), Is.EqualTo("42"));
            Assert.That(converter.Convert(42.0m), Is.EqualTo("42.0"));
            Assert.That(converter.Convert(0.42m), Is.EqualTo("0.42"));
            Assert.That(converter.Convert(00.42m), Is.EqualTo("0.42"));
            Assert.That(converter.Convert(6000000m), Is.EqualTo("6000000"));
            Assert.That(converter.Convert(6.12345678901234567890m), Is.EqualTo("6.12345678901234567890"));
        }

        [Test]
        public void TestConvertBrightstarEntityObject()
        {
            var converter = new DefaultKeyConverter();
            var mock = new MockEntityObject{Key="foo"};
            Assert.That(converter.Convert(mock), Is.EqualTo("foo"));
        }

        [Test]
        public void TestConvertMultipleValues()
        {
            var converter = new DefaultKeyConverter();
            var objectValue = new MockEntityObject {Key = "foo"};
            const int intValue = 42;
            Assert.That(converter.GenerateKey(new object[]{objectValue, intValue},"/", typeof(MockEntityObject)),
                Is.EqualTo("foo/42"));
        }

        [Test]
        public void TestConvertMultipleValuesIgnoresNulls()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.GenerateKey(new object[]{null, 42}, "/", typeof(MockEntityObject)), 
                Is.EqualTo("42"));
        }

        [Test]
        public void TestUriEscapingOfValues()
        {
            var converter = new DefaultKeyConverter();
            Assert.That(converter.Convert("foo/bar"), Is.EqualTo("foo/bar")); // Can still include path separators
            Assert.That(converter.Convert("foo#bar"), Is.EqualTo("foo#bar")); // Can still include fragment separators
            Assert.That(converter.Convert("foo?bar&bletch=1"), Is.EqualTo("foo?bar&bletch=1")); // Can still include query
            Assert.That(converter.Convert("foo bar"), Is.EqualTo("foo%20bar")); // unreserver characters get escaped
        }
    }
}
