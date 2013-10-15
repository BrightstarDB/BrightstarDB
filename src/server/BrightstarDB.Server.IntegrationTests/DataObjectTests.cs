using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
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

        private IDataObjectContext GetContext()
        {
            return BrightstarService.GetDataObjectContext(
                "Type=rest;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid());
        }

        [Test]
        public void TestGetRestDataContextByConnectionString()
        {
            var connectionString = "Type=rest;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            Assert.AreEqual(typeof(RestDataObjectContext), context.GetType());
        }
       
        [Test]
        public void TestGetHttpDataContextCreateStore()
        {
            var storeName = "DataObjectTests" + Guid.NewGuid();
            var context = GetContext();
            Assert.IsNotNull(context);
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
        }

        [Test]
        [ExpectedException(typeof(BrightstarClientException))]
        public void TestGetHttpDataContextDeleteStore()
        {
            var context = GetContext();
            Assert.IsNotNull(context);
            var storeId = "todelete_" + Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);
            Assert.IsNotNull(store);
            context.DeleteStore(storeId);
            Thread.Sleep(1000); // Slight delay to allow time for the shutdown to be processed
            context.OpenStore(storeId);
        }

        [Test]
        public void TestGetHttpDataContextOpenStore()
        {
            var connectionString = "Type=rest;endpoint=http://localhost:8090/brightstar;StoreName=DataObjectTests" + Guid.NewGuid();
            var cs = new ConnectionString(connectionString);
            var context = BrightstarService.GetDataObjectContext(cs);
            Assert.IsNotNull(context);
            var store = context.CreateStore(cs.StoreName);
            Assert.IsNotNull(store);
            store = context.OpenStore(cs.StoreName);
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestOpenDataObjectStoreWithNamespaceMappingsHttp()
        {
            var context = GetContext();
            Assert.IsNotNull(context);
            var storeName = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeName);
            Assert.IsNotNull(store);
            store = context.OpenStore(storeName, new Dictionary<string, string> { { "people", "http://www.networkedplanet.com/people/" } });
            Assert.IsNotNull(store);
        }

        [Test]
        public void TestCreateDataObjectWithUriHttp()
        {
            var context = GetContext();
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestCreateDataObjectWithStringHttp()
        {
            var context = GetContext();
            Assert.IsNotNull(context);
            var store = context.CreateStore(Guid.NewGuid().ToString());
            Assert.IsNotNull(store);
            var p1 = store.MakeDataObject("http://www.networkedplanet.com/people/gra");
            Assert.IsNotNull(p1);
        }

        [Test]
        public void TestRetrieveNonExistantDataObject()
        {
            var context = GetContext();
            var storeId = Guid.NewGuid().ToString();
            var store = context.CreateStore(storeId);
            var p1 = store.GetDataObject("http://some.random.uri/thing");
            Assert.IsNotNull(p1);
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

            // Check access to properties in both graphs
            var store3 = context.OpenStore(storeName, prefixes, updateGraph: graph1);
            updateDataObject = store3.GetDataObject("resource:Alice");
            Assert.IsNotNull(updateDataObject);
            Assert.AreEqual("ABCD1234", updateDataObject.GetPropertyValue("foaf:mbox_sha1").ToString());
            Assert.AreEqual("Alice Test", updateDataObject.GetPropertyValue("foaf:name").ToString());
            Assert.AreEqual("alice@example.org", updateDataObject.GetPropertyValue("foaf:mbox").ToString());


            var store4 = context.OpenStore(storeName, prefixes, updateGraph: graph1,
                                           defaultDataSet: new string[] {graph1});
            updateDataObject = store4.GetDataObject("resource:Alice");
            Assert.IsNotNull(updateDataObject);
            updateDataObject.Delete();
            store4.SaveChanges();

            // Check that the object and properties are still accessible through the default graph
            var store5 = context.OpenStore(storeName, prefixes, updateGraph: graph1);
            updateDataObject = store5.GetDataObject("resource:Alice");
            Assert.IsNotNull(updateDataObject);
            Assert.IsNotNull(updateDataObject.GetPropertyValue("foaf:mbox"));
            Assert.IsNotNull(updateDataObject.GetPropertyValue("foaf:name"));
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
            var connectionString = "type=rest;endpoint=http://localhost:8090/brightstar/";
            if (optimisticLockingEnabled) connectionString += ";optimisticLocking=true";
            return BrightstarService.GetDataObjectContext(connectionString);
        }

        private static IBrightstarService MakeRdfClient()
        {
            return
                BrightstarService.GetClient(
                    new ConnectionString("type=rest;endpoint=http://localhost:8090/brightstar/"));
        }       
    }
}
