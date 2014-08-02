using System;
using System.IO;
using System.Threading;
using BrightstarDB.Caching;
using BrightstarDB.Client;
using BrightstarDB.Client.RestSecurity;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
{
    [TestFixture]
    public class ClientCacheTests : ClientTestBase
    {
        private static IBrightstarService GetClient(ICache queryCache = null)
        {
            return BrightstarService.GetRestClient("http://localhost:8090/brightstar/", new PassthroughRequestAuthenticator(), queryCache);
        }

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
        public void TestQueryCacheInsertAndInvalidation()
        {
            var testCache = new MemoryCache(8192, new LruCacheEvictionPolicy());
            var client = GetClient(testCache);
            var storeName = "Client.TestQueryCacheInvalidation_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);
            const string query = "SELECT ?s WHERE { ?s ?p ?o }";
            client.ExecuteQuery(storeName, query);
            var cacheKey = storeName + "_" + query.GetHashCode() + "_" + SparqlResultsFormat.Xml;
            Assert.IsTrue(testCache.ContainsKey(cacheKey));
            testCache.Insert(cacheKey, new CachedQueryResult(DateTime.UtcNow, "This is a test"), CachePriority.Normal);
            var resultStream = client.ExecuteQuery(storeName, query);
            string result;
            using (var resultReader = new StreamReader(resultStream))
            {
                result = resultReader.ReadToEnd();
            }
            Assert.AreEqual("This is a test", result);

            Thread.Sleep(1000); // Allow for resolution of Last-Modified header

            client.ExecuteTransaction(storeName,
                                      new UpdateTransactionData
                                          {
                                              InsertData =
                                                  "<http://example.org/s> <http://example.org/p> <http://example.org/o> ."
                                          });

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

    }
}
