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
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullConnectionString()
        {
            var cs = new ConnectionString(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBlankConnectionString()
        {
            var cs = new ConnectionString(String.Empty);
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void TestHttpWithoutEndpoint()
        {
            var cs = new ConnectionString("type=http");
        }

        [Test]
        [ExpectedException(typeof(FormatException))]
        public void TestEmbeddedWithoutStoresDirectory()
        {
            var cs = new ConnectionString("type=embedded;storeDirectory=c:\brightstar");
        }
    }
}
