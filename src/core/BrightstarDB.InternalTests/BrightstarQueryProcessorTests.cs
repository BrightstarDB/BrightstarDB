using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    [Ignore("")]
    public class BrightstarQueryProcessorTests
    {
        private readonly IStoreManager _storeManager;
        private readonly IStore _docTagStore;

        public BrightstarQueryProcessorTests()
        {
            _storeManager = StoreManagerFactory.GetStoreManager();
            _docTagStore = InitializeDocTagStore();
        }

        private IStore InitializeDocTagStore()
        {
            var storeLoc = Configuration.StoreLocation + "\\doctagstore";
            IStore store;
            if (_storeManager.DoesStoreExist(storeLoc))
            {
                store = _storeManager.OpenStore(storeLoc);
                const string sparql = "SELECT ?doc ?cat WHERE { ?doc <http://www.bs.com/tag> <http://www.bs.com/category/1> . ?doc <http://www.bs.com/tag> ?cat . }";
                store.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
                return store;
            }
            store = _storeManager.CreateStore(storeLoc);
            var rand = new Random();
            for (int i = 0; i < 1000000; i++)
            {
                store.InsertTriple("http://www.bs.com/doc/" + i,
                    "http://www.bs.com/tag",
                    "http://www.bs.com/category/" + rand.Next(50), false, null, null, Constants.DefaultGraphUri);
                store.InsertTriple("http://www.bs.com/doc/" + i,
                    "http://www.bs.com/tag",
                    "http://www.bs.com/category/" + rand.Next(50), false, null, null, Constants.DefaultGraphUri);
            }
            store.Commit(Guid.Empty);
            return store;
        }

        [Test]
        public void TestBrutalJoin()
        {
            var timer = new Stopwatch();
            const string sparql = "SELECT ?doc ?cat WHERE { ?doc <http://www.bs.com/tag> <http://www.bs.com/category/1> . ?doc <http://www.bs.com/tag> ?cat . }";
            _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Start();
            var results = _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Stop();
            XDocument resultsDoc = XDocument.Parse(results);
            Console.WriteLine("Returned {0} rows in {1}ms",resultsDoc.SparqlResultRows().Count(), timer.ElapsedMilliseconds);

            results =
                _docTagStore.ExecuteSparqlQuery(
                    "SELECT ?doc WHERE { ?doc <http://www.bs.com/tag> <http://www.bs.com/category/1> .}", SparqlResultsFormat.Xml);
            resultsDoc = XDocument.Parse(results);
            Console.WriteLine("Returned {0} rows in {1}ms", resultsDoc.SparqlResultRows().Count(), timer.ElapsedMilliseconds);
        }

        [Test]
        public void TestAllResourceProperties()
        {
            var timer = new Stopwatch();
            const string sparql = "SELECT ?p ?o WHERE { <http://www.bs.com/doc/123> ?p ?o . }";
            timer.Start();
            var results = _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Stop();
            XDocument resultsDoc = XDocument.Parse(results);
            Console.WriteLine("Returned {0} rows in {1}ms", resultsDoc.SparqlResultRows().Count(), timer.ElapsedMilliseconds);
        }

        [Test]
        public void TestTwoHop()
        {
            var timer = new Stopwatch();
            const string sparql = @"SELECT ?relatedDoc ?cat WHERE { <http://www.bs.com/doc/123> <http://www.bs.com/tag> ?cat. ?relatedDoc <http://www.bs.com/tag> ?cat . }";
            _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Start();
            var results = _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Stop();
            XDocument resultsDoc = XDocument.Parse(results);
            Console.WriteLine("Returned {0} rows in {1}ms", resultsDoc.SparqlResultRows().Count(), timer.ElapsedMilliseconds);
        }

        [Test]
        public void TestInternalSort()
        {
            var timer = new Stopwatch();
            const string sparql = @"SELECT ?d ?c WHERE { <http://www.bs.com/doc/123> ?p ?c . ?d <http://www.bs.com/tag> ?cat . }";
            _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Start();
            var results = _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
            timer.Stop();
            XDocument resultsDoc = XDocument.Parse(results);
            Console.WriteLine("Returned {0} rows in {1}ms", resultsDoc.SparqlResultRows().Count(), timer.ElapsedMilliseconds);
        }

    }
}
