using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture("type=embedded;storesDirectory={1};storeName={2}", true)]
    [TestFixture(
        "type=dotnetrdf;configuration={0}dataObjectStoreConfig.ttl;storeName={1};store=http://www.brightstardb.com/tests#emptyStore"
        , false)]
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
    }
}
