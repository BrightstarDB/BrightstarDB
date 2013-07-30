using System;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class OptimiserTests
    {
        private readonly IStoreManager _storeManager;
        private readonly IStore _docTagStore;

        public OptimiserTests()
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
            store.Commit(Guid.NewGuid());
            return store;
        }

        [Test]
        public void TestVariableEqualsOptimiser()
        {
            const string sparql = @"SELECT ?d WHERE { ?d <http://www.bs.com/name> ?v . FILTER (?v = 'Foo') . }";
            _docTagStore.ExecuteSparqlQuery(sparql, SparqlResultsFormat.Xml);
        }
    }
}
