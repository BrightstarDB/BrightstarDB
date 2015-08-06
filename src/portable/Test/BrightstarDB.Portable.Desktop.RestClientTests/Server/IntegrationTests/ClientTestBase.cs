using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
{
    public class ClientTestBase
    {
        public void StartService()
        {
            // Service needs to be run in an external process
        }

        public void CloseService()
        {
            
        }

        public static void AssertTriplePatternInGraph(IBrightstarService client, string storeName, string triplePattern,
                                      string graphUri)
        {
            var sparql = "ASK { GRAPH <" + graphUri + "> {" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsTrue(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertTriplePatternInDefaultGraph(IBrightstarService client, string storeName,
                                                              string triplePattern)
        {
            var sparql = "ASK {{" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsTrue(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertTriplePatternNotInGraph(IBrightstarService client, string storeName, string triplePattern,
                                      string graphUri)
        {
            var sparql = "ASK { GRAPH <" + graphUri + "> {" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsFalse(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertTriplePatternNotInDefaultGraph(IBrightstarService client, string storeName,
                                                              string triplePattern)
        {
            var sparql = "ASK {{" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsFalse(resultsDoc.SparqlBooleanResult());
        }

        public static void AssertUpdateTransaction(IBrightstarService client, string storeName, UpdateTransactionData txnData)
        {
            var job = client.ExecuteTransaction(storeName, txnData);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk, "Expected update transaction to complete successfully");
        }

        public static IJobInfo WaitForJob(IJobInfo job, IBrightstarService client, string storeName)
        {
            var cycleCount = 0;
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors && cycleCount < 100)
            {
                Thread.Sleep(500);
                cycleCount++;
                job = client.GetJobInfo(storeName, job.JobId);
            }
            if (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Assert.Fail("Job did not complete in time.");
            }
            return job;
        }

        public static string GetStoreUri(string storeName)
        {
            return "http://localhost:8090/brightstar/" + storeName;
        }

    }
}