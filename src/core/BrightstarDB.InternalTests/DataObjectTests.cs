using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.EntityFramework;
using BrightstarDB.Storage;
using BrightstarDB.Tests;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class DataObjectTests : ClientTestBase
    {
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

        private IDataObjectContext GetContext(string type)
        {
            switch(type)
            {
                case "http":
                    return BrightstarService.GetDataObjectContext(
                        "Type=http;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid());
                case "embedded":
                    return BrightstarService.GetDataObjectContext(
                        "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=DataObjectTests" + Guid.NewGuid());
            }
            return null;
        }

        [Test]
        public void TestCreateDataObjectContext()
        {
            var connectionString =
                new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\");
            IDataObjectContext context = new EmbeddedDataObjectContext(connectionString);
            Assert.IsNotNull(context);
        }

        [Test]
        public void TestGetHttpDataContextByConnectionString()
        {
            var connectionString = "Type=http;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            Assert.AreEqual(typeof(HttpDataObjectContext), context.GetType());
        }
       
        [Test]
        public void TestGetEmbeddedDataContextByConnectionString()
        {
            var connectionString = "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            Assert.AreEqual(typeof(EmbeddedDataObjectContext), context.GetType());
        }

        [Test]
        public void TestGetHttpDataContextCreateStore()
        {
            var connectionString = "Type=http;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            var store = context.CreateStore(cs.StoreName);
            Assert.IsNotNull(store);
        }

        [Test]
        [ExpectedException(typeof(BrightstarClientException))]
        public void TestGetHttpDataContextDeleteStore()
        {
            var context = GetContext("http");
            Assert.IsNotNull(context);
            var storeId = "todelete_" + Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);
            Assert.IsNotNull(store);
            context.DeleteStore(storeId);
            Thread.Sleep(1000); // Slight delay to allow time for the shutdown to be processed
            context.OpenStore(storeId);
        }

        [Test]
        public void TestGetEmbeddedDataContextCreateStore()
        {
            var connectionString = "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context, "context is null");
            var store = context.CreateStore(cs.StoreName);
            Assert.IsNotNull(store, "store is null");
        }

        [Test]
        public void TestGetHttpDataContextOpenStore()
        {
            var connectionString = "Type=http;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            var store = context.CreateStore(cs.StoreName);
            Assert.IsNotNull(store);
            store = context.OpenStore(cs.StoreName);
            Assert.IsNotNull(store);
        }
        [Test]
        public void TestGetEmbeddedDataContextOpenStore()
        {
            var connectionString = "Type=embedded;StoresDirectory=c:\\brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            var store = context.CreateStore(cs.StoreName);
            Assert.IsNotNull(store);
            store = context.OpenStore(cs.StoreName);
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestCreateDataObjectStore()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestOpenDataObjectStore()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName);
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestOpenDataObjectStoreWithNamespaceMappings()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string> { {"people", "http://www.networkedplanet.com/people/"}});
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestOpenDataObjectStoreWithNamespaceMappingsHttp()
        {
            var context = GetContext("http");
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string> { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestOpenDataObjectStoreWithNamespaceMappingsEmbedded()
        {
            var context = GetContext("embedded");
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string> { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestCreateDataObjectWithUri()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObjectWithUriHttp()
        {
            var context = GetContext("http");
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObjectWithUriEmbedded()
        {
            var context = GetContext("embedded");
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObjectWithString()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObjectWithStringHttp()
        {
            var context = GetContext("http");
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObjectWithStringEmbedded()
        {
            var context = GetContext("embedded");
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObject()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
        }

        [Test]
        public void TestCreateDataObjectWithIdentity()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/staff/jen");
            Assert.IsNotNull(p1);
            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
            Assert.IsNotNull(p1.Identity);
            Assert.AreEqual("http://www.networkedplanet.com/staff/jen", p1.Identity);
        }

        [Test]
        public void TestCreateDataObjectWithCurie()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string> { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);

            var p1 = store.MakeDataObject("people:gra");
            Assert.IsNotNull(p1);
            Assert.AreEqual("http://www.networkedplanet.com/people/gra", p1.Identity);
        }

        [Test]
        public void TestRetrieveNonExistantDataObject()
        {
            var context = GetContext("http");
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);
            var p1 = store.GetDataObject("http://some.random.uri/thing");
            Assert.IsNotNull(p1);
            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
        }

        [Test]
        public void TestSaveAndFetchDataObject()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            
            p1.SetProperty(store.MakeDataObject("http://www.np.com/label"), "graham");

            store.SaveChanges();

            store = context.OpenStore(storeName);

            var p2 = store.GetDataObject(p1.Identity);
            Assert.IsNotNull(p2);
            Assert.AreEqual(p1.Identity, p2.Identity);
            
        }

        [Test]
        public void TestSavedDataObjectPropertyIsSameAfterSave()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);

            var labelType = store.MakeDataObject("http://www.np.com/label"); 
            p1.SetProperty(labelType, "graham");

            store.SaveChanges();

            store = context.OpenStore(storeName);
            var p2 = store.GetDataObject(p1.Identity);
            Assert.IsNotNull(p2);
            Assert.AreEqual(p1.Identity, p2.Identity);

            var label = p2.GetPropertyValue(labelType);
            Assert.AreEqual("graham", label);
        }

        [Test]
        public void TestLocalStateAfterSetProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "graham");

            var propValue = p1.GetPropertyValue(ageType);

            Assert.AreEqual(propValue, "graham");
        }

        [Test]
        public void TestSetSamePropertyResultsInOnlyOneProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var labelType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(labelType, "graham");
            p1.SetProperty(labelType, "kal");

            var propValue = p1.GetPropertyValue(labelType);

            Assert.AreEqual(propValue, "kal");

            Assert.AreEqual(1, ((DataObject)p1).Triples.Count());
            Assert.AreEqual(1, ((EmbeddedDataObjectStore)store).AddTriples.Count());
        }

        [Test]
        public void TestRemoveProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            p1.RemovePropertiesOfType(ageType);

            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
            Assert.AreEqual(0, ((EmbeddedDataObjectStore)store).AddTriples.Count());
        }


        [Test]
        public void TestRemovePropertyPersisted()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.CreateStore(Guid.NewGuid().ToString());

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            p1.RemovePropertiesOfType(ageType);

            Assert.AreEqual(0, ((DataObject)p1).Triples.Count());
            Assert.AreEqual(0, ((EmbeddedDataObjectStore)store).AddTriples.Count());

            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(0, ((DataObject)p2).Triples.Count());

        }

        [Test]
        public void TestAddAndRemovePropertyPersisted()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.SetProperty(ageType, "kal");

            var propValue = p1.GetPropertyValue(ageType);
            Assert.AreEqual(propValue, "kal");

            store.SaveChanges();
            store = context.OpenStore(storeName);

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(1, ((DataObject)p2).Triples.Count());

            p2.RemovePropertiesOfType(ageType);

            Assert.AreEqual(0, ((DataObject)p2).Triples.Count());
            Assert.AreEqual(0, ((EmbeddedDataObjectStore)store).AddTriples.Count());           
        }


        [Test]
        public void TestSetSamePropertyResultsInOnlyOnePropertyAfterSave()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
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

        [Test]
        public void TestDataObjectFluentApi()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId, new Dictionary<string, string>
                                                       { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var gra = store.MakeDataObject()
                        .SetType(store.MakeDataObject("http://www.networkedplanet.com/types/person"))
                        .SetProperty(store.MakeDataObject("http://www.networkedplanet.com/types/age"), 23)
                        .SetProperty(store.MakeDataObject("http://www.networkedplanet.com/types/worksfor"), 
                                        store.MakeDataObject("http://www.networkedplanet.com/companies/np")
                                     );
            Assert.IsNotNull(gra);

            var kal = store.MakeDataObject()
                        .SetType(store.MakeDataObject("http://www.networkedplanet.com/types/person"))
                        .SetProperty("ont:age", 23)
                        .SetProperty("rdfs:label", "Kal")
                        .SetProperty("ont:worksfor",
                                        store.MakeDataObject("http://www.networkedplanet.com/companies/np")
                                            .SetProperty("rdfs:label", "Networked Planet")
                                     )
                        .SetProperty("ont:email", store.MakeListDataObject(new List<string> { "kal@networkedplanet.com"}));
            Assert.IsNotNull(kal);

            store.SaveChanges();
        }

        [Test]
        public void TestDataObjectList()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId, new Dictionary<string, string>
                                                       { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var list = store.MakeListDataObject(new[] {"bob", "gra"});

            Assert.AreEqual("bob", list.GetPropertyValue("rdf:first"));
            Assert.IsInstanceOfType(typeof(IDataObject), list.GetPropertyValue("rdf:rest"));
            var dataobj = list.GetPropertyValue("rdf:rest") as IDataObject;
            Assert.IsNotNull(dataobj);
            Assert.AreEqual("gra", dataobj.GetPropertyValue("rdf:first"));

            store.SaveChanges();
        }

        [Test]
        public void TestAddProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var ageType = store.MakeDataObject("http://www.np.com/label");
            p1.AddProperty(ageType, "graham");
            p1.AddProperty(ageType, "kal");

            store.SaveChanges();
            store = context.OpenStore(storeName);

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(2, ((DataObject)p2).Triples.Count());
        }

        [Test]
        public void TestGetProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
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

        [Test]
        public void TestCurieObjectGetProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId,
                                      new Dictionary<string, string> { { "np", "http://www.np.com/" } });

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var labelType = store.MakeDataObject("np:label");
            p1.AddProperty(labelType, "graham");
            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(1, ((DataObject)p2).Triples.Count());
            var label = p2.GetPropertyValue(labelType);
            Assert.IsNotNull(label);
            Assert.AreEqual("graham", label);
        }

        [Test]
        public void TestTypedObjectGetProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId,
                                      new Dictionary<string, string> { { "np", "http://www.np.com/" } });

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);

            //object type
            var productType = store.MakeDataObject("http://www.networkedplanet.com/schemas/product");
            p1.SetType(productType);

            var labelType = store.MakeDataObject("np:label");
            p1.AddProperty(labelType, "graham");
            store.SaveChanges();

            var p2 = store.GetDataObject(p1.Identity);
            var label = p2.GetPropertyValue(labelType);
            Assert.IsNotNull(label);
            Assert.AreEqual("graham", label);
        }
        
            

        [Test]
        public void TestCurieObjectGetPropertyPersisted()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId,
                                      new Dictionary<string, string> { { "np", "http://www.np.com/" } });

            var p1 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            
            //object type
            var productType = store.MakeDataObject("http://www.networkedplanet.com/schemas/product");
            p1.SetType(productType);

            var labelType = store.MakeDataObject("np:label");
            p1.AddProperty(labelType, "graham");
            store.SaveChanges();

            store = context.OpenStore(storeId,
                                      new Dictionary<string, string> { { "np", "http://www.np.com/" } });

            var p2 = store.GetDataObject(p1.Identity);
            var label = p2.GetPropertyValue(labelType);
            Assert.IsNotNull(label);
            Assert.AreEqual("graham", label);

            var label2 = p2.GetPropertyValue("np:label");
            Assert.IsNotNull(label2);
            Assert.AreEqual("graham", label2);
        }

        [Test]
        public void TestSetPropertyDataObject()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
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

        [Test]
        public void TestGetProperties()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
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

        [Test]
        public void TestRemoveSpecificValueProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
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


        [Test]
        public void TestGetRelatedProxies()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId, new Dictionary<string, string>
                                                         { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var p1 = store.MakeDataObject().AddProperty("rdfs:label", "networkedplanet");
            var p2 = store.MakeDataObject().AddProperty("rdfs:label", "gra").AddProperty("ont:worksfor", p1);
            store.SaveChanges();

            store = context.OpenStore(storeId, new Dictionary<string, string>
                                                   { { "ont", "http://www.networkedplanet.com/types/" }, 
                                                                               { "rdfs", "http://www.w3.org/2000/01/rdf-schema#" },
                                                                               { "rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"} });

            var p3 = store.GetDataObject(p2.Identity);
            Assert.IsNotNull(p3);

            var related = p3.GetPropertyValues("ont:worksfor").OfType<IDataObject>().ToList();
            Assert.AreEqual(1, related.Count());

            var np = related.FirstOrDefault();
            Assert.IsNotNull(np);
            Assert.AreEqual(p1.Identity, np.Identity);
        }

        [Test]
        public void TestGetRelatedProxiesWithSafeCurie()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId, new Dictionary<string, string>
                                                         {
                                                             {"ont", "http://www.networkedplanet.com/types/"},
                                                             {"rdfs", "http://www.w3.org/2000/01/rdf-schema#"},
                                                             {"rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"}
                                                         });

            var p1 = store.MakeDataObject().AddProperty("rdfs:label", "networkedplanet");
            var p2 = store.MakeDataObject().AddProperty("rdfs:label", "gra").AddProperty("ont:worksfor", p1);
            store.SaveChanges();

            store = context.OpenStore(storeId, new Dictionary<string, string>
                                                   {
                                                       {"ont", "http://www.networkedplanet.com/types/"},
                                                       {"rdfs", "http://www.w3.org/2000/01/rdf-schema#"},
                                                       {"rdf", "http://www.w3.org/1999/02/22-rdf-syntax-ns#"}
                                                   });

            var p3 = store.GetDataObject(p2.Identity);
            Assert.IsNotNull(p3);

            var related = p3.GetPropertyValues("[ont:worksfor]").OfType<IDataObject>().ToList();
            Assert.AreEqual(1, related.Count());

            var np = related.FirstOrDefault();
            Assert.IsNotNull(np);
            Assert.AreEqual(p1.Identity, np.Identity);
        }

        [Test]
        public void TestAnonymousDataObjects()
        {
            // a resource with an anon data object which links to two other objects
            const string data = @"<http://www.np.com/objects/1> <http://www.np.com/types/p1> _:anon1 . 
                         _:anon1 <http://www.np.com/types/p2> <http://www.np.com/objects/2> . 
                         _:anon1 <http://www.np.com/types/p2> <http://www.np.com/objects/3> . ";

            var storeId = Guid.NewGuid().ToString();
            InitializeStore(storeId, data);

            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.OpenStore(storeId);

            var obj = store.GetDataObject("http://www.np.com/objects/1");


            var bnode = obj.GetPropertyValue("http://www.np.com/types/p1") as IDataObject;

            Assert.IsNotNull(bnode);
            Assert.IsTrue(bnode.Identity.StartsWith("http://www.brightstardb.com/.well-known/genid/"));
        }

        private static void InitializeStore(string storeId, string data)
        {
            var dataStream = new MemoryStream(Encoding.Default.GetBytes(data));
            using (var rawStore = StoreManagerFactory.GetStoreManager().CreateStore(Configuration.StoreLocation + "\\" + storeId))
            {
                rawStore.Import(Guid.Empty, dataStream);
                rawStore.Commit(Guid.Empty);
            }
        }

        [Test]
        public void TestRemoveProperty2()
        {
            var storeId = Guid.NewGuid().ToString();
            const string data = @"<http://www.np.com/objects/1> <http://www.np.com/types/p1> <http://www.np.com/objects/2> .
                  <http://www.np.com/objects/3> <http://www.np.com/types/p1> <http://www.np.com/objects/4> .";
            InitializeStore(storeId, data);
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.OpenStore(storeId);
            var obj1 = store.GetDataObject("http://www.np.com/objects/1");
            var obj1Related = obj1.GetPropertyValues("http://www.np.com/types/p1").OfType<IDataObject>().ToList();
            var obj3 = store.GetDataObject("http://www.np.com/objects/3");
            var obj3Related = obj3.GetPropertyValues("http://www.np.com/types/p1").OfType<IDataObject>().ToList();
            Assert.AreEqual(1, obj1Related.Count());
            Assert.IsTrue(obj1Related.Any(r=>r.Identity.Equals("http://www.np.com/objects/2")));
            Assert.AreEqual(1, obj3Related.Count());
            Assert.IsTrue(obj3Related.Any(r => r.Identity.Equals("http://www.np.com/objects/4")));


            obj3.AddProperty("http://www.np.com/types/p1", store.MakeDataObject("http://www.np.com/objects/5"));
            obj1.RemovePropertiesOfType("http://www.np.com/types/p1");
            obj1Related = obj1.GetPropertyValues("http://www.np.com/types/p1").OfType<IDataObject>().ToList();
            obj3Related = obj3.GetPropertyValues("http://www.np.com/types/p1").OfType<IDataObject>().ToList();
            Assert.AreEqual(0, obj1Related.Count());
            Assert.AreEqual(2, obj3Related.Count());
            Assert.IsTrue(obj3Related.Any(r => r.Identity.Equals("http://www.np.com/objects/4")));
            Assert.IsTrue(obj3Related.Any(r => r.Identity.Equals("http://www.np.com/objects/5")));

            store.SaveChanges();

            obj1 = store.GetDataObject("http://www.np.com/objects/1");
            obj1Related = obj1.GetPropertyValues("http://www.np.com/types/p1").OfType<IDataObject>().ToList();
            obj3 = store.GetDataObject("http://www.np.com/objects/3");
            obj3Related = obj3.GetPropertyValues("http://www.np.com/types/p1").OfType<IDataObject>().ToList();
            Assert.AreEqual(0, obj1Related.Count());
            Assert.AreEqual(2, obj3Related.Count());
            Assert.IsTrue(obj3Related.Any(r => r.Identity.Equals("http://www.np.com/objects/4")));
            Assert.IsTrue(obj3Related.Any(r => r.Identity.Equals("http://www.np.com/objects/5")));

        }

        [Test]
        [ExpectedException(typeof(TransactionPreconditionsFailedException))]
        public void TestOptimisticLocking()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId, optimisticLockingEnabled: true);

            var p1 = store.MakeDataObject();
            var p2 = store.MakeDataObject();
            Assert.IsNotNull(p1);
            var classificationType = store.MakeDataObject("http://www.np.com/classification");
            p1.SetProperty(classificationType, p1);
            p1.SetProperty(classificationType, p2);

            store.SaveChanges();

            var p3 = store.GetDataObject(p1.Identity);
            Assert.AreEqual(2, ((DataObject)p3).Triples.Count());

            var store1 = context.OpenStore(storeId, optimisticLockingEnabled: true);
            var e1 = store1.GetDataObject(p1.Identity);

            var store2 = context.OpenStore(storeId, optimisticLockingEnabled: true);
            var e2 = store2.GetDataObject(p1.Identity);

            e1.SetProperty("http://www.np.com/types/label", "gra");

            store1.SaveChanges();

            e2.SetProperty("http://www.np.com/types/label", "gra");
            store2.SaveChanges();
        }

        [Test]
        public void TestDataObjectProperties()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);
            
            //products
            var productType = store.MakeDataObject("http://www.networkedplanet.com/schemas/product");

            var brightstarDb = store.MakeDataObject("http://www.networkedplanet.com/products/brightstar");
            brightstarDb.SetType(productType);

            //properties
            var name = store.MakeDataObject("http://www.networkedplanet.com/schemas/product/name");
            brightstarDb.SetProperty(name, "Brightstar DB");

            store.SaveChanges();
            store = context.OpenStore(storeId);

            brightstarDb = store.GetDataObject("http://www.networkedplanet.com/products/brightstar");
            Assert.IsNotNull(brightstarDb);
            Assert.IsNotNull(brightstarDb.GetPropertyValue(name));

        }

        [Test]
        public void TestDataObjectPropertiesUsingMappings()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            context.CreateStore(storeId);
            var store = context.OpenStore(storeId, new Dictionary<string, string> { { "products", "http://www.networkedplanet.com/products/" } });

            //products
            var productType = store.MakeDataObject("http://www.networkedplanet.com/schemas/product");

            var brightstarDb = store.MakeDataObject("products:brightstar");
            brightstarDb.SetType(productType);

            //properties
            var name = store.MakeDataObject("http://www.networkedplanet.com/schemas/product/name");
            brightstarDb.SetProperty(name, "Brightstar DB");

            store.SaveChanges();
            store = context.OpenStore(storeId, new Dictionary<string, string> { { "products", "http://www.networkedplanet.com/products/" }});

            var getObjectByFullIdentity = store.GetDataObject("http://www.networkedplanet.com/products/brightstar");
            Assert.AreEqual(2, ((DataObject)getObjectByFullIdentity).Triples.Count());
            Assert.IsNotNull(getObjectByFullIdentity.GetPropertyValue(name), "Name property is null");
            Assert.AreEqual("Brightstar DB", getObjectByFullIdentity.GetPropertyValue(name));

            var getObjectByCurie = store.GetDataObject("products:brightstar");
            Assert.AreEqual(2, ((DataObject)getObjectByCurie).Triples.Count());
            Assert.IsNotNull(getObjectByCurie.GetPropertyValue(name), "Name property is null");
            Assert.AreEqual("Brightstar DB", getObjectByCurie.GetPropertyValue(name));
            

        }

        [Test]
        public void TestDataObjectDeleteObjectsUsedInProperties()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);

            //Categories
            var categoryType = store.MakeDataObject("http://www.networkedplanet.com/schemas/category");

            var nosql = store.MakeDataObject("http://www.networkedplanet.com/categories/nosql");
            nosql.SetType(categoryType);
            var dotnet = store.MakeDataObject("http://www.networkedplanet.com/categories/.net");
            dotnet.SetType(categoryType);
            var rdf = store.MakeDataObject("http://www.networkedplanet.com/categories/rdf");
            rdf.SetType(categoryType);
            var topicmaps = store.MakeDataObject("http://www.networkedplanet.com/categories/topicmaps");
            topicmaps.SetType(categoryType);
            
            store.SaveChanges();
            store = context.OpenStore(storeId);

            
            var allCategories = store.BindDataObjectsWithSparql("SELECT ?cat WHERE {?cat a <http://www.networkedplanet.com/schemas/category>}").ToList();
            Assert.IsNotNull(allCategories);
            Assert.AreEqual(4, allCategories.Count);
            foreach (var c in allCategories)
            {
                c.Delete();
            }
            store.SaveChanges();
            store = context.OpenStore(storeId);

            allCategories = store.BindDataObjectsWithSparql("SELECT ?cat WHERE {?cat a <http://www.networkedplanet.com/schemas/category>}").ToList();
            Assert.IsNotNull(allCategories);
            Assert.AreEqual(0, allCategories.Count);  // all categories have been deleted


            nosql = store.GetDataObject("http://www.networkedplanet.com/categories/nosql");

            Assert.AreEqual(0, ((DataObject)nosql).Triples.Count());
            

        }

        [Test]
        public void TestRemoveLiteralProperty()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);

            var p1 = store.MakeDataObject();
            var p2 = store.MakeDataObject();

            var o1 = store.MakeDataObject();
            o1.AddProperty(p1, "graham");
            o1.AddProperty(p2, 23);

            var o2 = store.MakeDataObject();
            o2.AddProperty(p1, "bob");
            o2.AddProperty(p2, 24);

            o2.RemoveProperty(p1, "bob");

            store.SaveChanges();
            store = context.OpenStore(storeId);

            var o3 = store.GetDataObject(o1.Identity);
            var o4 = store.GetDataObject(o2.Identity);

            Assert.AreEqual("graham", o3.GetPropertyValue(p1));
            Assert.AreEqual(23, o3.GetPropertyValue(p2));
            Assert.AreEqual(24, o4.GetPropertyValue(p2));
            Assert.IsNull(o4.GetPropertyValue(p1));
        }

        [Test]
        public void TestQueryLessThan()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            context.CreateStore(storeName);

            Dictionary<string, string> namespaceMappings;
            SetUpData(context, storeName, out namespaceMappings);

            var store = context.OpenStore(storeName, namespaceMappings);
            const string lowpay = "SELECT ?p ?s WHERE { ?p a <http://example.org/schema/person> . ?p <http://example.org/schema/salary> ?s . FILTER (?s < 50000)  }";

            var sparqlResult = store.ExecuteSparql(lowpay);

            Assert.IsNotNull(sparqlResult);
            var result = sparqlResult.ResultDocument;
            Assert.AreEqual(4, result.SparqlResultRows().Count());

        }

        [Test]
        public void TestQueryGreaterThan()
        {
            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            context.CreateStore(storeName);

            Dictionary<string, string> namespaceMappings;
            SetUpData(context, storeName, out namespaceMappings);

            var store = context.OpenStore(storeName, namespaceMappings);
            const string highPay = "SELECT ?p ?s WHERE { ?p a <http://example.org/schema/person> . ?p <http://example.org/schema/salary> ?s . FILTER (?s>50000)  }";

            var sparqlResult = store.ExecuteSparql(highPay);

            Assert.IsNotNull(sparqlResult);
            var result = sparqlResult.ResultDocument;
            Assert.AreEqual(5, result.SparqlResultRows().Count());
        }

        [Test]
        public void TestPreconditionsFailedException()
        {
            IDataObjectContext context =
                new EmbeddedDataObjectContext(
                    new ConnectionString("type=embedded;optimisticLocking=true;storesDirectory=" +
                                         Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            var store1 = context.CreateStore(storeName);
            var store1Alice = store1.MakeDataObject("http://example.org/alice");
            store1Alice.SetProperty("http://example.org/age", 21);
            store1.SaveChanges();

            var store2 = context.OpenStore(storeName);
            var store2Alice = store2.GetDataObject("http://example.org/alice");
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

        [Test]
        public void TestRefreshSingleClientWins()
        {
            IDataObjectContext context =
                new EmbeddedDataObjectContext(
                    new ConnectionString("type=embedded;optimisticLocking=true;storesDirectory=" +
                                         Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            var store1 = context.CreateStore(storeName);
            var store1Alice = store1.MakeDataObject("http://example.org/alice");
            store1Alice.SetProperty("http://example.org/age", 21);
            store1.SaveChanges();

            var store2 = context.OpenStore(storeName);
            var store2Alice = store2.GetDataObject("http://example.org/alice");
            store2Alice.SetProperty("http://example.org/age", 22);
            store2.SaveChanges();

            store1Alice.SetProperty("http://example.org/age", 20);
            try
            {
                store1.SaveChanges();
                Assert.Fail("Expected a TransactionPreconditionsFailed exception");
            }
            catch (TransactionPreconditionsFailedException)
            {
                // Expected
                store1.Refresh(RefreshMode.ClientWins, store1Alice);
                store1.SaveChanges();

                // Should have forced the update through
                var store3 = context.OpenStore(storeName);
                var store3Alice = store3.GetDataObject(store1Alice.Identity);
                Assert.AreEqual(20, store3Alice.GetPropertyValue("http://example.org/age"));
            }
        }

        [Test]
        public void TestRefreshSingleStoreWins()
        {
            IDataObjectContext context =
                new EmbeddedDataObjectContext(
                    new ConnectionString("type=embedded;optimisticLocking=true;storesDirectory=" +
                                         Configuration.StoreLocation + "\\"));
            var storeName = Guid.NewGuid().ToString();
            var store1 = context.CreateStore(storeName);
            var store1Alice = store1.MakeDataObject("http://example.org/alice");
            store1Alice.SetProperty("http://example.org/age", 21);
            store1.SaveChanges();

            var store2 = context.OpenStore(storeName);
            var store2Alice = store2.GetDataObject("http://example.org/alice");
            store2Alice.SetProperty("http://example.org/age", 22);
            store2.SaveChanges();

            store1Alice.SetProperty("http://example.org/age", 20);
            try
            {
                store1.SaveChanges();
                Assert.Fail("Expected a TransactionPreconditionsFailed exception");
            }
            catch (TransactionPreconditionsFailedException)
            {
                // Expected
                store1.Refresh(RefreshMode.StoreWins, store1Alice);
                Assert.AreEqual(22, store1Alice.GetPropertyValue("http://example.org/age"));
                store1.SaveChanges();

                // Should have forced the update through
                var store3 = context.OpenStore(storeName);
                var store3Alice = store3.GetDataObject(store1Alice.Identity);
                Assert.AreEqual(22, store3Alice.GetPropertyValue("http://example.org/age"));
            }
        }

        [Test]
        public void TestUpdateGraphTargetting()
        {
            IDataObjectContext context = MakeDataObjectContext();
            var storeName = "TestUpdateGraphTargetting_" + DateTime.Now.Ticks;
            var prefixes = new Dictionary<string, string>
                {
                    {"foaf", "http://xmlns.com/foaf/0.1/"},
                    {"resource", "http://example.org/resource/"}
                };

            // Create a resource with some initial properties in the default graph
            var store1 = context.CreateStore(storeName, prefixes);
            var personType = store1.MakeDataObject("foaf:Person");
            var firstName = store1.MakeDataObject("foaf:givenName");
            var surname = store1.MakeDataObject("foaf:surname");
            var fullname = store1.MakeDataObject("foaf:name");
            const string inferredGraphUri = "http://example.org/graphs/inferred";
            var johnSmith = store1.MakeDataObject("resource:John_Smith");
            johnSmith.SetType(personType);
            johnSmith.SetProperty(firstName, "John");
            johnSmith.SetProperty(surname, "Smith");
            store1.SaveChanges();

            // Create a context that updates a new "inferred" graph and add a property
            var store2 = context.OpenStore(storeName, prefixes, updateGraph:inferredGraphUri ,
                                           defaultDataSet: new[] {Constants.DefaultGraphUri});
            johnSmith = store2.GetDataObject("resource:John_Smith");
            fullname = store2.GetDataObject("foaf:name");
            Assert.IsNotNull(johnSmith, "Could not find base data object in store2");
            var gn = johnSmith.GetPropertyValue("foaf:givenName") as string;
            Assert.IsNotNull(gn, "Could not find foaf:givenName property of base data object in store2");
            var sn = johnSmith.GetPropertyValue("foaf:surname") as string;
            Assert.IsNotNull(sn, "Could not find foaf:surname property of base data object in store2");
            johnSmith.SetProperty(fullname, gn + " " + sn);
            store2.SaveChanges();

            // Create a context that reads from both the default and inferred graphs
            var store3 = context.OpenStore(storeName, prefixes, updateGraph: Constants.DefaultGraphUri,
                                           defaultDataSet: new string[] {Constants.DefaultGraphUri, inferredGraphUri});
            johnSmith = store3.GetDataObject("resource:John_Smith");
            Assert.IsNotNull(johnSmith, "Could not find base data object in store3");
            var fn = johnSmith.GetPropertyValue("foaf:name");
            Assert.IsNotNull(fn, "Could not find name property on base data object in store3");
            Assert.AreEqual("John Smith", fn);

            // Create a context that reads only from the inferred graph
            var store4 = context.OpenStore(storeName, prefixes, updateGraph: inferredGraphUri, defaultDataSet:new string[]{inferredGraphUri});
            johnSmith = store4.GetDataObject("resource:John_Smith");
            Assert.IsNotNull(johnSmith);
            fn = johnSmith.GetPropertyValue("foaf:name");
            Assert.IsNotNull(fn);
            // foaf:givenName and foaf:surname should not be found
            gn = johnSmith.GetPropertyValue("foaf:givenName") as string;
            Assert.IsNull(gn);
            sn = johnSmith.GetPropertyValue("foaf:surname") as string;
            Assert.IsNull(sn);

            // Verify the quads are as expected.
            var client = MakeRdfClient();
            var query = @"SELECT ?p ?o ?g FROM NAMED <" + Constants.DefaultGraphUri + "> " +
                        " FROM NAMED <" + inferredGraphUri + "> WHERE {" +
                        "  GRAPH ?g { <http://example.org/resource/John_Smith> ?p ?o } }";
            var resultStream = client.ExecuteQuery(storeName, query);
            var results = XDocument.Load(resultStream);
            foreach (var row in results.SparqlResultRows())
            {
                var p = row.GetColumnValue("p").ToString();
                var g = row.GetColumnValue("g").ToString();
                if (p.Equals("http://xmlns.com/foaf/0.1/givenName") ||
                    p.Equals("http://xmlns.com/foaf/0.1/surname") ||
                    p.Equals("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"))
                {
                    Assert.AreEqual(Constants.DefaultGraphUri, g, "Triple with predicate {0} is not in the expected graph", p);
                }
                else if (p.Equals("http://xmlns.com/foaf/0.1/name"))
                {
                    Assert.AreEqual(inferredGraphUri, g, "Triple with predicate {0} is not in the expected graph", p);
                }
                else
                {
                    Assert.Fail("Found a statement with an unexpected predicate: {0}", p);
                }
            }

        }

        [Test]
        public void TestDeleteObjectFromGraph()
        {
            IDataObjectContext context = MakeDataObjectContext();
            var storeName = "TestDeleteObjectFromGraph_" + DateTime.Now.Ticks;

            var prefixes = new Dictionary<string, string>
            {
                {"foaf", "http://xmlns.com/foaf/0.1/"},
                {"resource", "http://example.org/resource/"}
            };

            // Create an object with some properties in the default graph
            var store1 = context.CreateStore(storeName, prefixes);
            var baseDataObject = store1.MakeDataObject("resource:Alice");
            baseDataObject.SetProperty("foaf:name", "Alice Test");
            baseDataObject.SetProperty("foaf:mbox", "alice@example.org");
            store1.SaveChanges();

            // Add a new property in a separate graph
            var graph1 = "http://example.org/graphs/graph1";
            var store2 = context.OpenStore(storeName, prefixes, updateGraph: graph1);
            var updateDataObject = store2.GetDataObject("resource:Alice");
            Assert.IsNotNull(updateDataObject);
            updateDataObject.SetProperty("foaf:mbox_sha1", "ABCD1234");
            store2.SaveChanges();

            // Check access to properties in both graphs then delete the object from one of the graphs
            var store3 = context.OpenStore(storeName, prefixes, updateGraph: graph1);
            updateDataObject = store3.GetDataObject("resource:Alice");
            Assert.IsNotNull(updateDataObject);
            Assert.AreEqual("ABCD1234", updateDataObject.GetPropertyValue("foaf:mbox_sha1").ToString());
            Assert.AreEqual("Alice Test", updateDataObject.GetPropertyValue("foaf:name").ToString());
            Assert.AreEqual("alice@example.org", updateDataObject.GetPropertyValue("foaf:mbox").ToString());
            updateDataObject.Delete();
            store3.SaveChanges();

            // Check that the object and properties are still accessible through the default graph
            var store4 = context.OpenStore(storeName, prefixes, updateGraph: graph1);
            updateDataObject = store4.GetDataObject("resource:Alice");
            Assert.IsNotNull(updateDataObject);
            Assert.AreEqual("Alice Test", updateDataObject.GetPropertyValue("foaf:name").ToString());
            Assert.AreEqual("alice@example.org", updateDataObject.GetPropertyValue("foaf:mbox").ToString());
            
        }

        [Test]
        public void TestVersioningGraph()
        {
            var context = MakeDataObjectContext(true);
            var storeName = "TestVersioningGraph_" + DateTime.Now.Ticks;
            const string versionGraph = "http://example.org/graphs/versioning";

            var store1 = context.CreateStore(storeName, versionTrackingGraph:versionGraph);
            var store1Alice = store1.MakeDataObject("http://example.org/alice");
            store1Alice.SetProperty("http://example.org/age", 21);
            store1.SaveChanges();

            var store2 = context.OpenStore(storeName, versionTrackingGraph:versionGraph);
            var store2Alice = store2.GetDataObject("http://example.org/alice");
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

            // Check that the versioning info has been managed in the correct graph
            var client = MakeRdfClient();
            var resultsStream = client.ExecuteQuery(storeName, "SELECT ?s ?p ?o FROM <" + versionGraph + "> WHERE { ?s ?p ?o }");
            var results = XDocument.Load(resultsStream);
            var rows = results.SparqlResultRows().ToList();
            Assert.AreEqual(1, rows.Count);
            var row = rows[0];
            Assert.AreEqual("http://example.org/alice", row.GetColumnValue("s").ToString());
            Assert.AreEqual(Constants.VersionPredicateUri, row.GetColumnValue("p").ToString());
            Assert.AreEqual(2, row.GetColumnValue("o"));
        }

        private static IDataObjectContext MakeDataObjectContext(bool optimisticLockingEnabled = false)
        {
            var connectionString = "type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\";
            if (optimisticLockingEnabled) connectionString += ";optimisticLocking=true";

            return new EmbeddedDataObjectContext(new ConnectionString(connectionString));
        }

        private static IBrightstarService MakeRdfClient()
        {
            return
                BrightstarService.GetClient(
                    new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
        }

        private void SetUpData(IDataObjectContext context, string storeName, out Dictionary<string,string> namespaceMappings)
        {
            namespaceMappings = new Dictionary<string, string>
                                    {
                                            {"people", "http://example.org/people/"},
                                            {"skills", "http://example.org/skills/"},
                                            {"schema", "http://example.org/schema/"}
                                        };

            var store = context.OpenStore(storeName, namespaceMappings);

            var personType = store.MakeDataObject("schema:person");
            var salary = store.MakeDataObject("schema:salary");
            //add 10 people
            // salaries = 10000, 20000, ... 100000
            for(var i = 0; i<10; i++)
            {
                var p = store.MakeDataObject("people:personname" + i);
                p.SetType(personType);
                var pay = (i + 1)*10000;
                p.SetProperty(salary, pay);
            }
            store.SaveChanges();

            store = context.OpenStore(storeName, namespaceMappings);
            const string getpeopleQuery = "SELECT ?p WHERE { ?p a <http://example.org/schema/person> }";
            var people = store.BindDataObjectsWithSparql(getpeopleQuery);

            Assert.AreEqual(10, people.Count());
        }
       
    }
}
