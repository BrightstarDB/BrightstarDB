using System;
using System.Collections.Generic;
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
            _config = ConfigurationLoader.LoadConfiguration(TestConfiguration.DataLocation + "dataObjectStoreConfig.ttl");
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
            return System.IO.Path.GetFullPath(TestConfiguration.DataLocation + path);
        }
    }
}
