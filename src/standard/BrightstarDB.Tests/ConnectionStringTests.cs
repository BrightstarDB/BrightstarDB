using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class ConnectionStringTests
    {
        [Test]
        public void TestNullConnectionString()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var cs = new ConnectionString(null);
            });
        }

        [Test]
        public void TestBlankConnectionString()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var cs = new ConnectionString(string.Empty);
            });
        }

        [Test]
        public void TestHttpWithoutEndpoint()
        {
            Assert.Throws<FormatException>(() =>
            {
                var cs = new ConnectionString("type=http");
            });
        }

        [Test]
        public void TestEmbeddedWithoutStoresDirectory()
            {
                Assert.Throws<FormatException>(() =>
                {
                    var cs = new ConnectionString("type=embedded;storeDirectory=c:\\brightstar");
                });
            }
    }
}
