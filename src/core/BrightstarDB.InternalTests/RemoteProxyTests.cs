using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class RemoteProxyTests : ClientTestBase
    {
        private readonly ConnectionString _connectionString = new ConnectionString("type=http;endpoint=http://localhost:8090/brightstar");

        [ClassInitialize]
        public static void SetUp(TestContext context)
        {
            StartService();
        }

        [ClassCleanup]
        public static void TearDown()
        {
            CloseService();
        }


        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestCreateDataObjectContext()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestCreateDataObjectStore()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestOpenDataObjectStore()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName);
            Assert.IsNotNull(store);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestOpenDataObjectStoreWithNamespaceMappings()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string>() { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestCreateDataObjectWithUri()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestCreateDataObjectWithString()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestCreateDataObject()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestCreateDataObjectWithCurie()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestSaveAndFetchDataObject()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestSavedDataObjectPropertyIsSameAfterSave()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestLocalStateAfterSetProperty()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "graham");

            var propValue = p1.GetPropertyValue(ageType);

            Assert.AreEqual(propValue, "graham");
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestSetSamePropertyResultsInOnlyOneProperty()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "graham");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);

            Assert.AreEqual(propValue, "kal");

            Assert.AreEqual(1, ((DataObject)p1).Triples.Count());
            Assert.AreEqual(1, ((RemoteDataObjectStore)store).AddTriples.Count());
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestRemoveProperty()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            p1.RemovePropertiesOfType(ageType);

            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
            Assert.AreEqual(0, ((RemoteDataObjectStore)store).AddTriples.Count());
        }


        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestRemovePropertyPersisted()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            p1.RemovePropertiesOfType(ageType);

            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
            Assert.AreEqual(0, ((RemoteDataObjectStore)store).AddTriples.Count());

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(0, ((DataObject)p2).Triples.Count());

        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestAddAndRemovePropertyPersisted()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(1, ((DataObject)p2).Triples.Count());

            p2.RemovePropertiesOfType(ageType);

            Assert.AreEqual(0, ((DataObject)p2).Triples.Count());
            Assert.AreEqual(0, ((RemoteDataObjectStore)store).AddTriples.Count());
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestSetSamePropertyResultsInOnlyOnePropertyAfterSave()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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
            Assert.AreEqual(1, ((DataObject)p2).Triples.Count());
        }

        [TestMethod]
        [Ignore]
        [TestCategory("RemoteProxyTests")]
        public void TestDataObjectFluids()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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
                        .SetProperty("ont:email", new List<string> { "kal@networkedplanet.com" });

            store.SaveChanges();
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestAddProperty()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(2, ((DataObject)p2).Triples.Count());
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestGetProperty()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(2, ((DataObject)p2).Triples.Count());
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestSetPropertyDataObject()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            var p2 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var classificationType = store.MakeDataObject("http://www.np.com/classification");
            p1.SetProperty(classificationType, p1);
            p1.SetProperty(classificationType, p2);

            store.SaveChanges();

            var p3 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(1, ((DataObject)p3).Triples.Count());
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestGetProperties()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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
            Assert.AreEqual(2, ((DataObject)p2).Triples.Count());

            var propValues = p2.GetPropertyValues("http://www.np.com/label").Cast<string>();
            Assert.IsNotNull(propValues);
            Assert.AreEqual(2, propValues.Count());
        }

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestRemoveSpecificValueProperty()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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
            Assert.AreEqual(2, ((DataObject)p2).Triples.Count());

            var propValues = p2.GetPropertyValues("http://www.np.com/label").Cast<string>();
            Assert.IsNotNull(propValues);
            Assert.AreEqual(2, propValues.Count());

            // remove it
            p2.RemoveProperty(ageType, "kal");
            store.SaveChanges();

            Assert.AreEqual(1, ((DataObject)p2).Triples.Count());
            store = context.OpenStore(storeId);

            var p3 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(1, ((DataObject)p3).Triples.Count());

            var label = p3.GetPropertyValue(ageType);
            Assert.IsNotNull(label);
            Assert.AreEqual("graham", label);
        }


        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestGetRelatedProxies()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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

        [TestMethod]
        [TestCategory("RemoteProxyTests")]
        public void TestGetRelatedProxiesWithSafeCurie()
        {
            IDataObjectContext context = new HttpDataObjectContext(_connectionString);
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

        [TestMethod]
        public void TestPreconditionsFailedException()
        {
            IDataObjectContext context =
                new HttpDataObjectContext(
                    new ConnectionString("type=http;optimisticLocking=true;endpoint=http://localhost:8090/brightstar"));
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
