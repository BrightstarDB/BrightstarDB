using System;
using BrightstarDB.Polaris.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Polaris.Tests
{
    [TestClass]
    public class ConnectionModelTests
    {
        [TestMethod]
        public void TestParseEmbeddedConnectionString()
        {
            var conn = new Connection("EmbeddedTest", "type=embedded;storesdirectory=c:\\brightstar;", false);
            Assert.AreEqual("c:\\brightstar", conn.DirectoryPath);
            Assert.IsNull(conn.ServerEndpoint);
            Assert.AreEqual("EmbeddedTest", conn.Name);
            Assert.AreEqual(false, conn.RequiresAuthentication);
        }


        [TestMethod]
        public void TestParseRestConnectionString()
        {
            var conn = new Connection("RestTest", "type=rest;endpoint=http://localhost:8090/brightstar", true);
            Assert.AreEqual("http://localhost:8090/brightstar", conn.ServerEndpoint);
            Assert.IsNull(conn.DirectoryPath);
            Assert.AreEqual("RestTest", conn.Name);
            Assert.AreEqual(true, conn.RequiresAuthentication);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestParseHttpConnectionString()
        {
            var conn = new Connection("HttpTest", "type=http;endpoint=http://localhost:8090", false);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestParseTcpConnectionString()
        {
            var conn = new Connection("TcpTest", "type=tcp;endpoint=net.tcp://localhost:8095/brightstar", false);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void TestParseNamedPipeConnectionString()
        {
            var conn = new Connection("NamedPipeTest", "type=namedpipe;endpoint=net.pipe://localhost/brightstar", false);
        }
    }
}
