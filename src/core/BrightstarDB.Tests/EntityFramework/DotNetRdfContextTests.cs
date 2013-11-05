using System.IO;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.Tests.EntityFramework
{
    [TestFixture]
    public class DotNetRdfContextTests
    {
        [Test]
        public void TestInitializeWithStoreConfiguration()
        {
            var configFilePath = Path.GetFullPath(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            var connectionString = "type=dotNetRdf;configuration=" + configFilePath + ";storeName=example;store=http://www.brightstardb.com/tests#peopleStore";
            const string baseGraph = "http://example.org/people";

            var context = new MyEntityContext(connectionString, updateGraphUri:baseGraph, datasetGraphUris:new string[]{baseGraph});
            // Can find by property
            var alice = context.FoafPersons.FirstOrDefault(p => p.Name.Equals("Alice"));
            Assert.That(alice, Is.Not.Null);
            // Can find by ID
            alice = context.FoafPersons.FirstOrDefault(p => p.Id.Equals("alice"));
            Assert.That(alice, Is.Not.Null);
            Assert.That(alice.Name, Is.EqualTo("Alice"));
            Assert.That(alice.Knows, Is.Not.Null);
        }

        [Test]
        public void TestInsertIntoDefaultGraph()
        {
            var connectionString = MakeStoreConnectionString("example", "http://www.brightstardb.com/tests#emptyStore");
            string aliceId;
            using (var context = new MyEntityContext(connectionString))
            {
                var alice = context.FoafPersons.Create();
                aliceId = alice.Id;
                context.SaveChanges();
            }

            using (var context = new MyEntityContext(connectionString))
            {
                var alice = context.FoafPersons.FirstOrDefault(p => p.Id.Equals(aliceId));
                Assert.That(alice, Is.Not.Null);
            }
        }

        private static string MakeStoreConnectionString(string storeName, string storeId)
        {
            var configFilePath = Path.GetFullPath(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            return string.Format("type=dotNetRdf;configuration={0};storeName={1};store={2}", configFilePath, storeName,
                                 storeId);
        }
    }
}
