using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Caching;
using BrightstarDB.Client;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class ClientCacheTests : ClientTestBase
    {
        private static IBrightstarService GetClient(ICache queryCache = null)
        {
            return BrightstarService.GetHttpClient(new Uri("http://localhost:8090/brightstar"), queryCache);
        }
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
        public void TestQueryCacheInsertAndInvalidation()
        {
            var testCache = new MemoryCache(8192, new LruCacheEvictionPolicy());
            var client = GetClient(testCache);
            var storeName = "Client.TestQueryCacheInvalidation_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);
            const string query = "SELECT ?s WHERE { ?s ?p ?o }";
            client.ExecuteQuery(storeName, query);
            var cacheKey = storeName + "_" + query.GetHashCode();
            Assert.IsTrue(testCache.ContainsKey(cacheKey));
            testCache.Insert(cacheKey, new CachedQueryResult(DateTime.UtcNow, "This is a test"), CachePriority.Normal);
            var resultStream = client.ExecuteQuery(storeName, query);
            string result;
            using (var resultReader = new StreamReader(resultStream))
            {
                result = resultReader.ReadToEnd();
            }
            Assert.AreEqual("This is a test", result);

            client.ExecuteTransaction(storeName, null, null,
                                      "<http://example.org/s> <http://example.org/p> <http://example.org/o> .", true);
            resultStream = client.ExecuteQuery(storeName, query);
            using (var resultReader = new StreamReader(resultStream))
            {
                result = resultReader.ReadToEnd();
            }
            // We should not get the test result we inserted into the cache this time.
            Assert.AreNotEqual("This is a test", result);

            // The cache should have been updated with the result received from the server.
            var cacheResult = testCache.Lookup<CachedQueryResult>(cacheKey);
            Assert.AreEqual(result, cacheResult.Result);
        }

        private void CheckTriples(string storeName, int startId, int endId)
        {
            var store = StoreManagerFactory.GetStoreManager().OpenStore("C:\\brightstar\\" + storeName, true);
            List<int> missingIds = new List<int>();
            for (int i = startId; i < endId; i++)
            {
                var matchCount = store.Match("http://www.example.org/resource/" + i, null, null, graphs: null).Count();
                if (matchCount == 0) missingIds.Add(i);
            }
            if (missingIds.Count > 0)
            {
                Assert.Fail("Could match the following IDs: " + String.Join(", ", missingIds));
            }
        }


    }
}
