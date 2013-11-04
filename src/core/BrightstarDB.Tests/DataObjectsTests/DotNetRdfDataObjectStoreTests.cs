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
        [ExpectedException(typeof(FormatException))]
        public void TestDnrConnectionRequiresConfigurationProperty()
        {
            BrightstarService.GetDataObjectContext("type=dotnetrdf;storename=foo");
        }

        [Test]
        [ExpectedException(typeof(BrightstarClientException))]
        public void TestCannotInitializeBrightstarClientWithDnrConnection()
        {
            var configFilePath = Path.GetFullPath(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=example";
            BrightstarService.GetClient(connectionString);
        }

        [Test]
        public void TestInitializeFromDnrStoreConfiguration()
        {
            var configFilePath = Path.GetFullPath(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=example;store=http://www.brightstardb.com/tests#peopleStore";
            var namespaceMappings = new Dictionary<string, string>
                {
                    {"npp", "http://www.networkedplanet.com/people/"},
                    {"foaf", "http://xmlns.com/foaf/0.1/"}
                };
            var doContext = BrightstarService.GetDataObjectContext(connectionString);
            Assert.That(doContext, Is.Not.Null);
            Assert.That(doContext.DoesStoreExist("example"));
            var doStore = doContext.OpenStore("example", namespaceMappings, updateGraph:"http://example.org/people");
            var alice = doStore.GetDataObject("npp:alice");
            Assert.That(alice, Is.Not.Null);
            var email = alice.GetPropertyValue("foaf:mbox");
            Assert.That(email, Is.Not.Null);
        }

        [Test]
        public void TestInitializeFromDnrQueryAndUpdateConfiguration()
        {
            var configFilePath = Path.GetFullPath(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=example;query=http://www.brightstardb.com/tests#peopleStoreQuery;update=http://www.brightstardb.com/tests#peopleStoreUpdate";
            var namespaceMappings = new Dictionary<string, string>
                {
                    {"npp", "http://www.networkedplanet.com/people/"},
                    {"foaf", "http://xmlns.com/foaf/0.1/"}
                };
            var doContext = BrightstarService.GetDataObjectContext(connectionString);
            Assert.That(doContext, Is.Not.Null);
            Assert.That(doContext.DoesStoreExist("example"));
            var doStore = doContext.OpenStore("example", namespaceMappings, updateGraph: "http://example.org/people");
            var alice = doStore.GetDataObject("npp:alice");
            Assert.That(alice, Is.Not.Null);
            var email = alice.GetPropertyValue("foaf:mbox");
            Assert.That(email, Is.Not.Null);
        }

    }
}
