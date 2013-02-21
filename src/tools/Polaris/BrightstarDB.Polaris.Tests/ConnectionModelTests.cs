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
            var conn = new Connection("EmbeddedTest", "type=embedded;storesdirectory=c:\\brightstar;");
            Assert.AreEqual("c:\\brightstar", conn.DirectoryPath);
            Assert.IsNull(conn.ServerName);
            Assert.IsNull(conn.ServerPath);
            Assert.IsNull(conn.ServerPort);
            Assert.IsNull(conn.PipeName);
            Assert.AreEqual("EmbeddedTest", conn.Name);
        }

        [TestMethod]
        public void TestParseHttpConnectionString()
        {
            var conn = new Connection("HttpTest", "type=http;endpoint=http://localhost:8090");
            Assert.IsNull(conn.DirectoryPath);
            Assert.AreEqual("localhost",conn.ServerName);
            Assert.AreEqual("/", conn.ServerPath);
            Assert.AreEqual("8090", conn.ServerPort);
            Assert.IsNull(conn.PipeName);
            Assert.AreEqual("HttpTest", conn.Name);
        }

        [TestMethod]
        public void TestParseTcpConnectionString()
        {
            var conn = new Connection("TcpTest", "type=tcp;endpoint=net.tcp://localhost:8095/brightstar");
            Assert.IsNull(conn.DirectoryPath);
            Assert.AreEqual("localhost", conn.ServerName);
            Assert.AreEqual("/brightstar", conn.ServerPath);
            Assert.AreEqual("8095", conn.ServerPort);
            Assert.IsNull(conn.PipeName);
            Assert.AreEqual("TcpTest", conn.Name);
        }

        [TestMethod]
        public void TestParseNamedPipeConnectionString()
        {
            var conn = new Connection("NamedPipeTest", "type=namedpipe;endpoint=net.pipe://localhost/brightstar");
            Assert.IsNull(conn.DirectoryPath);
            Assert.AreEqual("localhost", conn.ServerName);
            Assert.IsNull(conn.ServerPath);
            Assert.IsNull(conn.ServerPort);
            Assert.AreEqual("brightstar", conn.PipeName);
            Assert.AreEqual("NamedPipeTest", conn.Name);
        }
    }
}
