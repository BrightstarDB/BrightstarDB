using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture]
    public class DotNetRdfDataObjectStoreTests
    {
        [Test]
        public void TestDnrConnectionRequiresConfigurationProperty()
        {
            Assert.Throws<FormatException>(() => BrightstarService.GetDataObjectContext("type=dotnetrdf;storename=foo"));
        }

        [Test]
        public void TestCannotInitializeBrightstarClientWithDnrConnection()
        {
            var configFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=example";
            Assert.Throws<BrightstarClientException>(() => BrightstarService.GetClient(connectionString));
        }

        [Test]
#if PORTABLE
        [Ignore("Configuration not supported by DotNetRDF Portable")]
#endif
        public void TestInitializeFromInMemoryStore()
        {
            var configFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath;
            var namespaceMappings = new Dictionary<string, string>
                {
                    {"npp", "http://www.networkedplanet.com/people/"},
                    {"foaf", "http://xmlns.com/foaf/0.1/"}
                };
            var doContext = BrightstarService.GetDataObjectContext(connectionString);
            Assert.That(doContext, Is.Not.Null);
            Assert.That(doContext.DoesStoreExist("http://www.brightstardb.com/tests#people"));
            var doStore = doContext.OpenStore("http://www.brightstardb.com/tests#people", namespaceMappings, updateGraph: "http://example.org/people");
            var alice = doStore.GetDataObject("npp:alice");
            Assert.That(alice, Is.Not.Null);
            var email = alice.GetPropertyValue("foaf:mbox");
            Assert.That(email, Is.Not.Null);
        }

        [Test]
#if PORTABLE
        [Ignore("Configuration not supported by DotNetRDF Portable")]
#endif
        public void TestInitializeFromFusekiStorageProvider()
        {
            var configFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath;
            var doContext = BrightstarService.GetDataObjectContext(connectionString);
            Assert.That(doContext.DoesStoreExist("http://www.brightstardb.com/tests#fuseki"));
            Assert.That(doContext.OpenStore("http://www.brightstardb.com/tests#fuseki"), Is.Not.Null);
        }

        [Test]
        public void TestInitializeFromDnrSparqlEndpoints()
        {
            var configFilePath = Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath +
                                   ";query=http://example.org/configuration#sparqlQuery;update=http://example.org/configuration#sparqlUpdate";
            var doContext = BrightstarService.GetDataObjectContext(connectionString);
        }
    }
}
