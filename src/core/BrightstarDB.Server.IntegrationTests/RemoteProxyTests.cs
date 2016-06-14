using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
{
    [TestFixture]
    public class RemoteProxyTests : ClientTestBase
    {
        private readonly ConnectionString _connectionString = new ConnectionString("type=rest;endpoint=http://localhost:8090/brightstar");

        [TestFixtureSetUp]
        public void SetUp()
        {
            StartService();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            CloseService();
        }


        [Test]
        [Category("RemoteProxyTests")]
        public void TestCreateDataObjectContext()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestCreateDataObjectStore()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestOpenDataObjectStore()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName);
            Assert.IsNotNull(store);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestOpenDataObjectStoreWithNamespaceMappings()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string>() { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestCreateDataObjectWithUri()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestCreateDataObjectWithString()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestCreateDataObject()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestCreateDataObjectWithCurie()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string>() { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);

            var p1 = store.MakeDataObject("people:gra");
            Assert.IsNotNull(p1);
            Assert.AreEqual("http://www.networkedplanet.com/people/gra", p1.Identity.ToString());
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestSaveAndFetchDataObject()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);

            p1.SetProperty(store.MakeDataObject("http://www.np.com/label"), "graham");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.IsNotNull(p2);
            Assert.AreEqual(p1.Identity, p2.Identity);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestSavedDataObjectPropertyIsSameAfterSave()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);

            var labelType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(labelType, "graham");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.IsNotNull(p2);
            Assert.AreEqual(p1.Identity, p2.Identity);

            var label = p2.GetPropertyValue(labelType);
            Assert.AreEqual("graham", label);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestLocalStateAfterSetProperty()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "graham");

            var propValue = p1.GetPropertyValue(ageType);

            Assert.AreEqual(propValue, "graham");
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestSetSamePropertyResultsInOnlyOneProperty()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "graham");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);

            Assert.AreEqual(propValue, "kal");

        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestRemoveProperty()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            p1.RemovePropertiesOfType(ageType);

            Assert.That(p1.GetPropertyValue(ageType), Is.Null);
        }


        [Test]
        [Category("RemoteProxyTests")]
        public void TestRemovePropertyPersisted()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            p1.RemovePropertiesOfType(ageType);

            Assert.That(p1.GetPropertyValue(ageType), Is.Null);

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.That(p2.GetPropertyValue(ageType), Is.Null);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestAddAndRemovePropertyPersisted()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.That(p2.GetPropertyValue(ageType), Is.EqualTo("kal"));

            p2.RemovePropertiesOfType(ageType);

            Assert.That(p2.GetPropertyValue(ageType), Is.Null);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestSetSamePropertyResultsInOnlyOnePropertyAfterSave()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "graham");
            p1.SetProperty(ageType, "kal");

            store.SaveChanges();

            store = context.OpenStore(storeId);

            var p2 = store.GetDataObject(p1.Identity);
            var values = p2.GetPropertyValues(ageType);
            Assert.That(values.Count(), Is.EqualTo(1));
            Assert.That(values.First(), Is.EqualTo("kal"));
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestDataObjectFluentApi()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId, new Dictionary<string, string>() { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var gra = store.MakeDataObject()
                        .SetType(store.MakeDataObject("http://www.networkedplanet.com/types/person"))
                        .SetProperty(store.MakeDataObject("http://www.networkedplanet.com/types/age"), 23)
                        .SetProperty(store.MakeDataObject("http://www.networkedplanet.com/types/worksfor"),
                                        store.MakeDataObject("http://www.networkedplanet.com/companies/np")
                                     );

            var kal = store.MakeDataObject()
                        .SetType(store.MakeDataObject("http://www.networkedplanet.com/types/person"))
                        .SetProperty("ont:age", 23)
                        .SetProperty("rdfs:label", "Kal")
                        .SetProperty("ont:worksfor",
                                        store.MakeDataObject("http://www.networkedplanet.com/companies/np")
                                            .SetProperty("rdfs:label", "Networked Planet")
                                     )
                        .SetProperty("ont:email", "kal@networkedplanet.com");

            store.SaveChanges();
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestAddProperty()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            var values = p2.GetPropertyValues(ageType).ToList();
            Assert.That(values.Count(), Is.EqualTo(2));
            Assert.That(values, Contains.Item("graham"));
            Assert.That(values, Contains.Item("kal"));
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestGetProperty()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            var values = p2.GetPropertyValues(ageType).ToList();
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values, Contains.Item("graham"));
            Assert.That(values, Contains.Item("kal"));
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestSetPropertyDataObject()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            var p2 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var classificationType = store.MakeDataObject("http://www.np.com/classification");
            p1.SetProperty(classificationType, p1);
            p1.SetProperty(classificationType, p2);

            store.SaveChanges();

            var p3 = store.GetDataObject(p1.Identity);
            var v = p3.GetPropertyValue(classificationType) as IDataObject;
            Assert.That(v, Is.Not.Null);
            Assert.That(v.Identity, Is.EqualTo(p2.Identity));

        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestGetProperties()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();

            store = context.OpenStore(storeId);

            var p2 = store.GetDataObject(p1.Identity);

            var propValues = p2.GetPropertyValues("http://www.np.com/label").Cast<string>();
            Assert.IsNotNull(propValues);
            Assert.AreEqual(2, propValues.Count());
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestRemoveSpecificValueProperty()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();

            store = context.OpenStore(storeId);

            var p2 = store.GetDataObject(p1.Identity);

            var propValues = p2.GetPropertyValues("http://www.np.com/label").Cast<string>();
            Assert.IsNotNull(propValues);
            Assert.AreEqual(2, propValues.Count());

            // remove it
            p2.RemoveProperty(ageType, "kal");
            store.SaveChanges();

            store = context.OpenStore(storeId);

            var p3 = store.GetDataObject(p1.Identity);
            var label = p3.GetPropertyValue(ageType);
            Assert.IsNotNull(label);
            Assert.AreEqual("graham", label);
        }


        [Test]
        [Category("RemoteProxyTests")]
        public void TestGetRelatedProxies()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId, new Dictionary<string, string>() { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var p1 = store.MakeDataObject().AddProperty("rdfs:label", "networkedplanet");
            var p2 = store.MakeDataObject().AddProperty("rdfs:label", "gra").AddProperty("ont:worksfor", p1);
            store.SaveChanges();

            store = context.OpenStore(storeId, new Dictionary<string, string>() { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var p3 = store.GetDataObject(p2.Identity);
            Assert.IsNotNull(p3);

            var related = p3.GetPropertyValues("ont:worksfor").OfType<IDataObject>();
            Assert.AreEqual(1, related.Count());

            var np = related.FirstOrDefault();
            Assert.IsNotNull(np);
            Assert.AreEqual(p1.Identity, np.Identity);
        }

        [Test]
        [Category("RemoteProxyTests")]
        public void TestGetRelatedProxiesWithSafeCurie()
        {
            IDataObjectContext context = new RestDataObjectContext(_connectionString);
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId, new Dictionary<string, string> { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var p1 = store.MakeDataObject().AddProperty("rdfs:label", "networkedplanet");
            var p2 = store.MakeDataObject().AddProperty("rdfs:label", "gra").AddProperty("ont:worksfor", p1);
            store.SaveChanges();

            store = context.OpenStore(storeId, new Dictionary<string, string> { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var p3 = store.GetDataObject(p2.Identity);
            Assert.IsNotNull(p3);

            var related = p3.GetPropertyValues("[ont:worksfor]").OfType<IDataObject>();
            Assert.AreEqual(1, related.Count());

            var np = related.FirstOrDefault();
            Assert.IsNotNull(np);
            Assert.AreEqual(p1.Identity, np.Identity);
        }

        [Test]
        public void TestPreconditionsFailedException()
        {
            IDataObjectContext context =
                new RestDataObjectContext(
                    new ConnectionString("type=rest;optimisticLocking=true;endpoint=http://localhost:8090/brightstar"));
            var storeName = Guid.NewGuid().ToString();
            var store1 = context.CreateStore(storeName);
            var store1Alice = store1.MakeDataObject("http://example.org/alice");
            store1Alice.SetProperty("http://example.org/age", 21);
            store1.SaveChanges();

            var store2 = context.OpenStore(storeName);
            var store2Alice = store2.GetDataObject(store1Alice.Identity);
            Assert.AreEqual(21, store2Alice.GetPropertyValue("http://example.org/age"));
            store2Alice.SetProperty("http://example.org/age", 22);
            store2.SaveChanges();

            store1Alice.SetProperty("http://example.org/age", 20);
            try
            {
                store1.SaveChanges();
                Assert.Fail("Expected a TransactionPreconditionsFailed exception");
            }
            catch (TransactionPreconditionsFailedException ex)
            {
                // Expected
                Assert.AreEqual(1, ex.InvalidSubjects.Count());
                Assert.AreEqual("http://example.org/alice", ex.InvalidSubjects.First());
            }
        }
    }
}
