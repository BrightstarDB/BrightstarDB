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
        public void TestInitializeFromDnrConfiguration()
        {
            var configFilePath = Path.GetFullPath(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=example";
            var namespaceMappings = new Dictionary<string, string>
                {
                    {"ex", "http://example.org/"},
                    {"foaf", "http://xmlns.com/foaf/0.1/"}
                };
            var doContext = BrightstarService.GetDataObjectContext(connectionString);
            Assert.That(doContext, Is.Not.Null);
            Assert.That(doContext.DoesStoreExist("example"));
            var doStore = doContext.OpenStore("example", namespaceMappings, updateGraph:"http://example.org/people");
            var alice = doStore.GetDataObject("ex:alice");
            Assert.That(alice, Is.Not.Null);
            var email = alice.GetPropertyValue("foaf:mbox");
            Assert.That(email, Is.Not.Null);
        }
    }
}
