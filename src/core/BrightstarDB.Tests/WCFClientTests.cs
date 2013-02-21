using System;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class WCFClientTests
    {
        /// <summary>
        /// This test checks that the configuration for using the http clients works as expected for a single operation.
        /// </summary>
        [TestMethod]
        public void TestBasicHttpBindingClient()
        {
            var client = BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
            var stores = client.ListStores();
            Assert.IsNotNull(stores);
        }


        [TestMethod]
        public void TestNamedPipeBindingClient()
        {
            var client = BrightstarService.GetClient("type=namedPipe;endpoint=net.pipe://localhost/brightstar");
            var stores = client.ListStores();
            Assert.IsNotNull(stores);
        }

        [TestMethod]
        public void TestNetTcpBindingClient()
        {
            var client = BrightstarService.GetClient("type=tcp;endpoint=net.tcp://localhost:8095/brightstar");
            var stores = client.ListStores();
            Assert.IsNotNull(stores);
        }



    }
}
