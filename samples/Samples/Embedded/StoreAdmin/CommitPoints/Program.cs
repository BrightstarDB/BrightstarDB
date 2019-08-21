using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.Samples.StoreAdministration.CommitPoints
{
    /// <summary>
    /// This sample appliction demonstrates how to programmatically access, query and revert to previous versions of a store.
    /// </summary>
    class Program
    {
        private static void Main()
        {
            // Register the license we are using for BrightstarDB
            SamplesConfiguration.Register();

            // Create a client - the connection string used is configured in the App.config file.
            var client =
                BrightstarService.GetClient("type=embedded;storesDirectory=" + SamplesConfiguration.StoresDirectory);

            // Create a test store and populate it with data using several transactions
            var storeName = CreateTestStore(client);

            Console.WriteLine("Initial commit points:");
            List<ICommitPointInfo> commitPoints = ListCommitPoints(client, storeName);

            // Revert to the last-but-one commit point
            Console.WriteLine("Reverting store to commit point #1");
            client.RevertToCommitPoint(storeName, commitPoints[1]);

            Console.WriteLine("Commit points after revert:");
            ListCommitPoints(client, storeName);

            // Revert back to the commit point before the first revert
            Console.WriteLine("Re-reverting store");
            client.RevertToCommitPoint(storeName, commitPoints[0]);

            Console.WriteLine("Commit points after re-revert:");
            ListCommitPoints(client, storeName);

            Console.WriteLine("Coalescing store to single commit point");
            var consolidateJob = client.ConsolidateStore(storeName);
            while (!(consolidateJob.JobCompletedOk || consolidateJob.JobCompletedWithErrors))
            {
                System.Threading.Thread.Sleep(500);
                consolidateJob = client.GetJobInfo(storeName, consolidateJob.JobId);
            }
            if (consolidateJob.JobCompletedOk)
            {
                Console.WriteLine("Consolidate job completed. Listing commit points after consolidate:");
                ListCommitPoints(client, storeName);
            }
            else
            {
                Console.WriteLine("Consolidate job failed.");
            }

            // Shutdown Brightstar processing threads
            BrightstarService.Shutdown();

            Console.WriteLine();
            Console.WriteLine("Finished. Press the Return key to exit.");
            Console.ReadLine();
        }

        private static string CreateTestStore(IBrightstarService client)
        {
            var storeName = "CommitPointsTest_"  +DateTime.Now.ToString("yyyyMMdd_HHmmss");
            client.CreateStore(storeName);

            // These are the different triple sets to add
            const string addSet1 = "<http://example.org/people/alice> <http://www.w3.org/2000/01/rdf-schema#label> \"Alice\".";
            const string addSet2 = "<http://example.org/people/bob> <http://www.w3.org/2000/01/rdf-schema#label> \"Bob\".";
            const string addSet3 = "<http://example.org/people/carol> <http://www.w3.org/2000/01/rdf-schema#label> \"Carol\".";

            // Transaction 1: Add Alice
            client.ExecuteTransaction(storeName, null, null, addSet1);

            // Transaction 2 : Add Bob
            client.ExecuteTransaction(storeName, null, null, addSet2);

            // Transaction 3 : Add Carol
            client.ExecuteTransaction(storeName, null, null, addSet3);

            return storeName;
        }

        private static List<ICommitPointInfo> ListCommitPoints(IBrightstarService client, string storeName)
        {
            var ret = new List<ICommitPointInfo>();
            int commitPointNumber = 0;
            // Retrieve 10 most recent commit point info from the store
            foreach(var commitPointInfo in client.GetCommitPoints(storeName, 0, 10))
            {
                ret.Add(commitPointInfo);
                Console.WriteLine("#{0}: ID: {1} Commited at: {2}", commitPointNumber++, commitPointInfo.Id, commitPointInfo.CommitTime);
                // Query this commit point
                QueryCommitPoint(client, commitPointInfo);
            }
            return ret;
        }

        private static void QueryCommitPoint(IBrightstarService client, ICommitPointInfo commitPointInfo)
        {
            const string sparqlQuery =
                "SELECT ?l WHERE { ?s <http://www.w3.org/2000/01/rdf-schema#label> ?l } ORDER BY ?l";
            var resultsStream = client.ExecuteQuery(commitPointInfo, sparqlQuery);
            var resultsDoc = XDocument.Load(resultsStream);
            var labels = new List<string>();
            foreach(var row in resultsDoc.SparqlResultRows())
            {
                labels.Add(row.GetColumnValue("l").ToString());
            }
            Console.WriteLine("Query returns: {0}", string.Join(", ",labels));
        }
    }
}
