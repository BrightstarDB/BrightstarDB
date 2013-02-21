using System.Xml.Linq;
using BrightstarDB;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RestClientTester
{
    public class StoreOperations : RestClientTest
    {
        private readonly IBrightstarService _client;
        private readonly string _storeName;

        public StoreOperations(ConnectionString connectionString) : base(connectionString)
        {
            _client = BrightstarService.GetClient(connectionString);
            _storeName = connectionString.StoreName;
        }

        /*
        public void TestDoesStoreExist()
        {
            Assert.IsTrue(_client.DoesStoreExist(_storeName), "Expected DoesStoreExist to return true after store created");
            Assert.IsFalse(_client.DoesStoreExist("foo" + _storeName), "Expected DoesStoreExist to return false for invalid name");
        }
        */
        public void TestTransactionJob()
        {
            var jobInfo = _client.ExecuteTransaction(_storeName, "", "",
                                       "<http://example.org/np> <http://example.org/employs> <http://example.org/kal>.");
            Assert.IsNotNull(jobInfo);
            Assert.IsTrue(jobInfo.JobCompletedOk);
            Assert.IsNotNull(jobInfo.StatusMessage);
        }

        public void TestQuery()
        {
            XDocument responseDoc = XDocument.Load(_client.ExecuteQuery(_storeName, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }"));
            Assert.IsNotNull(responseDoc);
            Assert.IsNotNull(responseDoc.Root);
            Assert.AreEqual("http://www.w3.org/2005/sparql-results#", responseDoc.Root.Name.Namespace);
            Assert.AreEqual("sparql", responseDoc.Root.Name.LocalName);
        }
    }
}
