using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture("type=embedded;storesDirectory={0}", true)]
    // TODO: Reinstate REST client tests when REST service is reinstated
    // [TestFixture("type=rest;endpoint=http://localhost:8090/brightstar", true)]
    /*[TestFixture(
        "type=dotnetrdf;configuration={1}dataObjectStoreConfig.ttl;storeName=http://www.brightstardb.com/tests#empty"
        , false)]*/
    public class BasicDataObjectsTests : ClientTestBase
    {
        private readonly ConnectionString _connectionString;
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
            var cs = String.Format(connectionString, Path.GetFullPath(Configuration.StoreLocation),
                Path.GetFullPath(Configuration.DataLocation));
            _connectionString = new ConnectionString(cs);
            _isPersistent = isPersistent;
        }

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            if (_connectionString.Type == ConnectionType.Rest)
            {
                StartService();
            }
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            if (_connectionString.Type == ConnectionType.Rest)
            {
                CloseService();
            }
        }

        [SetUp]
        public void SetUp()
        {
            var storeName = "BasicDataObjectTests_" + DateTime.Now.Ticks;
            if (_isPersistent)
            {
                var client = BrightstarService.GetClient(_connectionString);
                if (client.DoesStoreExist(storeName))
                {
                    client.DeleteStore(storeName);
                    Thread.Sleep(500);
                }
                client.CreateStore(storeName);
            }
            _context = BrightstarService.GetDataObjectContext(_connectionString);
            _store = _context.OpenStore(storeName, NamespaceMappings);
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

        [Test]
        [SetUICulture("en-US")]
        public void TestDateTimeRoundtripWithUSLocale()
        {
            var alice = _store.MakeDataObject("p:Alice");
            alice.AddProperty("foaf:dateOfBirth", new DateTime(1970, 01, 02));
            _store.SaveChanges();

            var retrieved = _store.GetDataObject("p:Alice");
            var dob = (DateTime)retrieved.GetPropertyValue("foaf:dateOfBirth");
            Assert.AreEqual(new DateTime(1970, 01, 02), dob);
        }

        [Test]
        public void TestDateTimeRoundtripUTC()
        {
            var alice = _store.MakeDataObject("p:Alice");
            alice.AddProperty("foaf:dateOfBirth", new DateTime(1970, 01, 02, 23, 58, 0, DateTimeKind.Utc));
            _store.SaveChanges();

            var retrieved = _store.GetDataObject("p:Alice");
            var dob = (DateTime) retrieved.GetPropertyValue("foaf:dateOfBirth");
            Assert.AreEqual(DateTimeKind.Utc, dob.Kind);
            Assert.AreEqual(new DateTime(1970, 01, 02, 23, 58, 0, DateTimeKind.Utc), dob);
        }

        [Test]
        public void TestDateTimeRoundtripLocal()
        {
            var alice = _store.MakeDataObject("p:Alice");
            alice.AddProperty("foaf:dateOfBirth", new DateTime(1970, 01, 02, 23, 58, 0, DateTimeKind.Local));
            _store.SaveChanges();

            var retrieved = _store.GetDataObject("p:Alice");
            var dob = (DateTime)retrieved.GetPropertyValue("foaf:dateOfBirth");
            Assert.AreEqual(DateTimeKind.Local, dob.Kind);
            Assert.AreEqual(new DateTime(1970, 01, 02, 23, 58, 0, DateTimeKind.Local), dob);
        }
    }
}
