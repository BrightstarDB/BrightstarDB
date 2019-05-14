using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using BrightstarDB.Config;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class EmbeddedClientTests
    {
        private readonly string _baseConnectionString;

        public EmbeddedClientTests()
        {
            _baseConnectionString = "type=embedded;storesDirectory=" + Configuration.StoreLocation;
        }

        private static string MakeStoreName(string testName)
        {
            return "EmbeddedClientTests_" + testName + "_" + DateTime.UtcNow.Ticks;
        }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            // Ensure a complete shutdown of embedded services before running these tests as they depend on re-initialization
            BrightstarService.Shutdown();
        }

        [TearDown]
        public void Cleanup()
        {
            // Ensure a complete shutdown between tests to prevent reuse of the embedded client configuration
            BrightstarService.Shutdown();
        }
        
        [Test]
        public void TestCreateStoreWithTransactionLoggingDisabled()
        {
            var client = BrightstarService.GetClient(_baseConnectionString,
                new EmbeddedServiceConfiguration(enableTransactionLoggingOnNewStores: false));
            var storeName = MakeStoreName("CreateStoreWithTransactionLoggingDisabled");
            client.CreateStore(storeName);
            Assert.IsFalse(File.Exists(Path.Combine(Configuration.StoreLocation, storeName, "transactionheaders.bs")));
            Assert.IsFalse(File.Exists(Path.Combine(Configuration.StoreLocation, storeName, "transactions.bs")));
            Assert.IsFalse(client.GetTransactions(storeName, 0, 10).Any());
        }

        [Test]
        public void TestCreateStoreWithTransactionLoggingEnabled()
        {
            var client = BrightstarService.GetClient(_baseConnectionString,
                new EmbeddedServiceConfiguration(enableTransactionLoggingOnNewStores: true));
            var storeName = MakeStoreName("CreateStoreWithTransactionLoggingEnabled");
            client.CreateStore(storeName);
            Assert.IsTrue(File.Exists(Path.Combine(Configuration.StoreLocation, storeName, "transactionheaders.bs")));
            Assert.IsTrue(File.Exists(Path.Combine(Configuration.StoreLocation, storeName, "transactions.bs")));
            Assert.IsFalse(client.GetTransactions(storeName, 0, 10).Any()); // Will still be false as no operations executed yet
        }
    }
}
