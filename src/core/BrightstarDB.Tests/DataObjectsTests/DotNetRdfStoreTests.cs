using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using NUnit.Framework;
using VDS.RDF;
using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Update;

namespace BrightstarDB.Tests.DataObjectsTests
{
    [TestFixture]
    public class DotNetRdfStoreTests
    {
        private IGraph _config;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _config = ConfigurationLoader.LoadConfiguration(Configuration.DataLocation + "dataObjectStoreConfig.ttl");
            ConfigurationLoader.PathResolver = new LocalPathResolver();
        }


        [Test]
        public void TestDataObjectRetrieval()
        {
            ITripleStore store = GetTripleStore("http://www.brightstardb.com/tests#peopleStore");
            var query =
                GetConfiguredObject<ISparqlQueryProcessor>("http://www.brightstardb.com/tests#peopleStoreQuery");
            var update =
                GetConfiguredObject<ISparqlUpdateProcessor>("http://www.brightstardb.com/tests#peopleStoreUpdate");

            var namespaceMappings = new Dictionary<string, string>
                {
                    {"foaf", "http://xmlns.com/foaf/0.1/"},
                    {"ex", "http://example.org/"}
                };
            //IDataObjectStore doStore = new SparqlDataObjectStore(query, update, namespaceMappings, false, null, null, null);

            //var alice = doStore.GetDataObject("ex:alice");
            //Assert.That(alice, Is.Not.Null);
        }

        private ITripleStore GetTripleStore(string storeId)
        {
            INode tripleStoreNode = _config.CreateUriNode(new Uri(storeId));
            Object tmp = ConfigurationLoader.LoadObject(_config, tripleStoreNode);
            if (tmp is ITripleStore)
            {
                return tmp as ITripleStore;
            }
            Assert.Fail("Could not load ITripleStore for identifier '{0}'", storeId);
            return null;
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
            return System.IO.Path.GetFullPath(Configuration.DataLocation + path);
        }
    }
}
