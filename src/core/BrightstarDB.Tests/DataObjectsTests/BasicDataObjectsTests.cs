using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture("type=embedded;storesDirectory={1};storeName={2}", true)]
    /*[TestFixture(
        "type=dotnetrdf;configuration={0}dataObjectStoreConfig.ttl;storeName=http://www.brightstardb.com/tests#empty"
        , false)]*/
    public class BasicDataObjectsTests
    {
        private readonly string _connectionString;
        private readonly bool _isPersistent;

        private IDataObjectContext _context;
        private IDataObjectStore _store;

        private static readonly Dictionary<string, string> NamespaceMappings = new Dictionary<string, string>
            {
                {"foaf", "http://xmlns.com/foaf/0.1/"},
                {"p", "http://networkedplanet.com/people/"}
            };

        public BasicDataObjectsTests(string connectionString, bool isPersistent)
        {
            _connectionString = connectionString;
            _isPersistent = isPersistent;
        }

        [SetUp]
        public void SetUp()
        {
            var storeName = "BasicDataObjectTests_" + DateTime.Now.Ticks;
            var connectionString = new ConnectionString(String.Format(_connectionString,
                Path.GetFullPath(Configuration.DataLocation),
                Path.GetFullPath(Configuration.StoreLocation),
                storeName));

            if (_isPersistent)
            {
                var client = BrightstarService.GetClient(connectionString);
                if (client.DoesStoreExist(connectionString.StoreName))
                {
                    client.DeleteStore(connectionString.StoreName);
                    Thread.Sleep(500);
                }
                client.CreateStore(connectionString.StoreName);
            }
            _context = BrightstarService.GetDataObjectContext(connectionString);
            _store = _context.OpenStore(connectionString.StoreName, NamespaceMappings);
        }

        [Test]
        public void TestInsertDataObject()
        {
            var k = _store.MakeDataObject("p:kal");
            k.AddProperty("foaf:name", "Kal Ahmed");
            _store.SaveChanges();

            Assert.That(_store.GetDataObject("p:kal"), Is.Not.Null);
            Assert.That(_store.GetDataObject("foaf:name"), Is.Not.Null);
            Assert.That(_store.GetDataObject("foaf:person"), Is.Not.Null);
        }

        [Test]
        public void TestEnumerateDistinctPropertyTypes()
        {
            var alice = _store.MakeDataObject("p:Alice");
            alice.AddProperty("foaf:name", "Alice");
            alice.AddProperty("foaf:mbox", "alice@example.org");
            alice.AddProperty("foaf:mbox", "alice.example@gmail.com");
            alice.AddProperty("foaf:nick", "Call Me Al");
            _store.SaveChanges();

            var retrieved = _store.GetDataObject("p:Alice");
            var propertyTypes = retrieved.GetPropertyTypes().ToList();
            Assert.That(propertyTypes.Count, Is.EqualTo(3));
            Assert.That(propertyTypes.Contains(_store.GetDataObject("foaf:name")));
            Assert.That(propertyTypes.Contains(_store.GetDataObject("foaf:mbox")));
            Assert.That(propertyTypes.Contains(_store.GetDataObject("foaf:nick")));
        }
    }
}
