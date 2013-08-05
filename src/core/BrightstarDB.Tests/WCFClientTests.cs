#if !PORTABLE
using System;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class WCFClientTests
    {
        /// <summary>
        /// This test checks that the configuration for using the http clients works as expected for a single operation.
        /// </summary>
        [Test]
        public void TestBasicHttpBindingClient()
        {
            var client = BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
            var stores = client.ListStores();
            Assert.IsNotNull(stores);
        }


        [Test]
        public void TestNamedPipeBindingClient()
        {
            var client = BrightstarService.GetClient("type=namedPipe;endpoint=net.pipe://localhost/brightstar");
            var stores = client.ListStores();
            Assert.IsNotNull(stores);
        }

        [Test]
        public void TestNetTcpBindingClient()
        {
            var client = BrightstarService.GetClient("type=tcp;endpoint=net.tcp://localhost:8095/brightstar");
            var stores = client.ListStores();
            Assert.IsNotNull(stores);
        }



    }
}
#endif