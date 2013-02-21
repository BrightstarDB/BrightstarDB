using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class ConnectionStringTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNullConnectionString()
        {
            var cs = new ConnectionString(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestBlankConnectionString()
        {
            var cs = new ConnectionString(String.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestHttpWithoutEndpoint()
        {
            var cs = new ConnectionString("type=http");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestEmbeddedWithoutStoresDirectory()
        {
            var cs = new ConnectionString("type=embedded;storeDirectory=c:\brightstar");
        }
    }
}
