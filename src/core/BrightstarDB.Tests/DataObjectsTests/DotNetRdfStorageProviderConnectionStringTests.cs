using System.IO;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture]
    public class DotNetRdfStorageProviderConnectionStringTests
    {
        [Test]
        public void TestCanConnectToConfiguredStorageProvider()
        {
            var doContext =
                BrightstarService.GetDataObjectContext("type=dotNetRdf;configuration=" + Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation) +
                                                       "dataObjectStoreConfig.ttl");
            // Configuration contains a single store
            var store = doContext.OpenStore("http://www.brightstardb.com/tests#people");
            Assert.That(store, Is.Not.Null);
        }

        [Test]
        public void TestConnectToInvalidResourceThrowsException()
        {
            var doContext = BrightstarService.GetDataObjectContext("type=dotNetRdf;configuration=" + Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation) +
                                                                   "dataObjectStoreConfig.ttl");
            Assert.Throws<BrightstarClientException>(()=> doContext.OpenStore("http://www.brightstardb.com/tests#peopleStore"), "The store 'http://www.brightstardb.com/tests#peopleStore' does not exist or cannot be accessed.");
        }

        [Test]
        public void TestBasicConfigurationDoesStoreExist()
        {
            var doContext =
                BrightstarService.GetDataObjectContext("type=dotNetRdf;configuration=" + Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation) +
                                                       "dataObjectStoreConfig.ttl");
            // Configuration contains a single store
            Assert.IsTrue(doContext.DoesStoreExist("http://www.brightstardb.com/tests#people"));

            // Check that the other resources in the file have not been resolved to a store.
            Assert.IsFalse(doContext.DoesStoreExist("http://www.brightstardb.com/tests#peopleGraph")); // is a graph configuration
            Assert.IsFalse(doContext.DoesStoreExist("http://www.brightstardb.com/tests#addGraph")); // is a graph configuration
            Assert.IsFalse(doContext.DoesStoreExist("http://www.brightstardb.com/tests#peopleStoreQuery")); // is a sparql query processor
            Assert.IsFalse(doContext.DoesStoreExist("http://www.brightstardb.com/tests#peopleStoreUpdate")); // is a sparql update processor

        }

        [Test]
        public void TestConfigurationWithMultipleStores()
        {
            var doContext = BrightstarService.GetDataObjectContext(
                "type=dotNetRdf;configuration=" + Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation) +
                "multipleObjectStoreConfiguration.ttl");

            Assert.IsTrue(doContext.DoesStoreExist("#emptyStore"));
            Assert.IsTrue(doContext.DoesStoreExist("anotherStore"));
            Assert.IsTrue(doContext.DoesStoreExist("#yetAnotherStore"));

            // Check we can open these stores without an exception
            Assert.That(doContext.OpenStore("#emptyStore"), Is.Not.Null);
            Assert.That(doContext.OpenStore("anotherStore"), Is.Not.Null);
            Assert.That(doContext.OpenStore("#yetAnotherStore"), Is.Not.Null);
        }

    }
}