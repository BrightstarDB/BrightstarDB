using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Client;
using NUnit.Framework;
using VDS.RDF;
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Update;

namespace BrightstarDB.InternalTests
{
    

    [TestFixture]
    public class DotNetRdfStoreTests
    {
        private IGraph _config;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _config = ConfigurationLoader.LoadConfiguration(Path.Combine(TestContext.CurrentContext.TestDirectory, TestConfiguration.DataLocation , "dataObjectStoreConfig.ttl"));
            ConfigurationLoader.PathResolver = new LocalPathResolver();
        }

        [Test]
        public void TestPeopleStoreConfiguration()
        {
            var store = GetConfiguredObject<ITripleStore>("http://www.brightstardb.com/tests#peopleStore");
            Assert.That(store, Is.Not.Null);
            Assert.That(store.HasGraph(new Uri("http://example.org/people")));

            // Check that we can retrieve some data for Alice through the query interface
            var query =
                GetConfiguredObject<ISparqlQueryProcessor>("http://www.brightstardb.com/tests#peopleStoreQuery");
            var parser = new SparqlQueryParser();
            var sparql = parser.ParseFromString("SELECT ?p ?o ?g FROM NAMED <http://example.org/people> WHERE { GRAPH ?g { <http://example.org/alice> ?p ?o } }");
            var results = query.ProcessQuery(sparql) as SparqlResultSet;
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Results.Count, Is.EqualTo(4));
        }

        [Test]
        public void TestDataObjectRetrieval()
        {
            var query =
                GetConfiguredObject<ISparqlQueryProcessor>("http://www.brightstardb.com/tests#peopleStoreQuery");
            var update =
                GetConfiguredObject<ISparqlUpdateProcessor>("http://www.brightstardb.com/tests#peopleStoreUpdate");

            var namespaceMappings = new Dictionary<string, string>
                {
                    {"foaf", "http://xmlns.com/foaf/0.1/"},
                    {"ex", "http://example.org/"}
                };

            IDataObjectStore doStore = new SparqlDataObjectStore(query, update, namespaceMappings, false, "http://example.org/people", null, null);

            var alice = doStore.GetDataObject("ex:alice");
            Assert.That(alice, Is.Not.Null);
            var name = alice.GetPropertyValue("foaf:name");
            Assert.That(name, Is.Not.Null);
        }

        [Test]
        public void TestDataObjectUpdate()
        {
            // Setup
            var query =
                GetConfiguredObject<ISparqlQueryProcessor>("http://www.brightstardb.com/tests#peopleStoreQuery");
            var update =
                GetConfiguredObject<ISparqlUpdateProcessor>("http://www.brightstardb.com/tests#peopleStoreUpdate");
            var namespaceMappings = new Dictionary<string, string>
                {
                    {"foaf", "http://xmlns.com/foaf/0.1/"},
                    {"ex", "http://example.org/"}
                };
            const string updateGraph = "http://example.org/people";
            //var dataset = new[] {updateGraph};
            string[] dataset = null;
            IDataObjectStore doStore = new SparqlDataObjectStore(query, update, namespaceMappings, false, 
                updateGraph, dataset, null);
            var bob = doStore.GetDataObject("ex:bob");
            
            // Execute
            bob.SetProperty("foaf:age", 40);
            doStore.SaveChanges();

            // Validate
            doStore = new SparqlDataObjectStore(query, update, namespaceMappings, false, updateGraph, dataset, null);
            bob = doStore.GetDataObject("ex:bob");
            Assert.That(bob, Is.Not.Null);
            Assert.That(bob.GetPropertyValue("foaf:age"), Is.EqualTo(40));

            // Execute a second update
            bob.SetProperty("foaf:age", 41);
            doStore.SaveChanges();

            // Validate
            doStore = new SparqlDataObjectStore(query, update, namespaceMappings, false, updateGraph, dataset, null);
            bob = doStore.GetDataObject("ex:bob");
            Assert.That(bob, Is.Not.Null);
            Assert.That(bob.GetPropertyValue("foaf:age"), Is.EqualTo(41));
        }

        [Test]
        public void TestUpdateIntoSeparateGraph()
        {
            // Setup
            var tripleStore = GetConfiguredObject<ITripleStore>("http://www.brightstardb.com/tests#peopleStore");
            var query =
                GetConfiguredObject<ISparqlQueryProcessor>("http://www.brightstardb.com/tests#peopleStoreQuery");
            var update =
                GetConfiguredObject<ISparqlUpdateProcessor>("http://www.brightstardb.com/tests#peopleStoreUpdate");
            var namespaceMappings = new Dictionary<string, string>
                {
                    {"foaf", "http://xmlns.com/foaf/0.1/"},
                    {"ex", "http://example.org/"}
                };
            const string updateGraph = "http://example.org/addGraph";
            const string baseGraph = "http://example.org/people";
            var dataset = new[] {baseGraph};

            IDataObjectStore doStore = new SparqlDataObjectStore(query, update, namespaceMappings, false,
                                                                 updateGraph, dataset, null);

            // Check that we can read properties from the base graph
            var bob = doStore.GetDataObject("ex:bob");
            var mbox = bob.GetPropertyValue("foaf:mbox");
            Assert.That(mbox, Is.Not.Null);
            Assert.That(mbox, Is.EqualTo("bob@example.org"));

            // Write a property
            bob.SetProperty("foaf:mbox_sha1", "ABCDE");
            doStore.SaveChanges();

            // Check that the property was written into the update graph
            Assert.That(tripleStore.HasGraph(new Uri(updateGraph)));
            var g = tripleStore.Graphs[new Uri(updateGraph)];
            var bobNode = g.CreateUriNode(new Uri("http://example.org/bob"));
            var mboxsha1Node = g.CreateUriNode(new Uri("http://xmlns.com/foaf/0.1/mbox_sha1"));
            var triples = g.GetTriplesWithSubjectPredicate(bobNode, mboxsha1Node).ToList();
            Assert.That(triples, Has.Count.EqualTo(1));
            var litNode = triples[0].Object as ILiteralNode;
            Assert.That(litNode, Is.Not.Null);
            Assert.That(litNode.Value, Is.EqualTo("ABCDE"));
        }

        private T GetConfiguredObject<T>(string id) where T : class
        {
            INode configNode = _config.CreateUriNode(new Uri(id));
            var tmp = ConfigurationLoader.LoadObject(_config, configNode);
            if (!(tmp is T)) throw new Exception(String.Format("Could not load object of type '{0}' for identifier '{1}'",
                typeof(T).FullName, id));
            return tmp as T;
        }
    }

    public class LocalPathResolver : IPathResolver
    {
        public string ResolvePath(string path)
        {
            return Path.Combine(TestContext.CurrentContext.TestDirectory, TestConfiguration.DataLocation + path);
        }
    }
}
