using System;
using System.Collections.Generic;
using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BTreeStore;
using NUnit.Framework;
using ITransactionInfo = BrightstarDB.Storage.ITransactionInfo;

namespace BrightstarDB.InternalTests
{
    /*
     * KA 08/11/2011
     * This was a test to introspect backwards through a broken store. 
     * I've left it in the source tree as an outline of how to do this with the current APIs
     * Note that it relies heavily on internals.
     */
    
    [TestFixture]
    [Ignore]
    public class BrokenStoreTests
    {
        private const string TestQuery =
    "SELECT ?x003Cgeneratedx003Ex005Fx0030 WHERE {?x003Cgeneratedx003Ex005Fx0030 a <http://brightstardb.com/namespaces/default/User> .}";

        [Test]
        public void FindWorkingTransaction()
        {
           Store store = new Store("c:\\brightstar\\twitteringtest\\", false);
            FileStoreManager fsm = new FileStoreManager(StoreConfiguration.DefaultStoreConfiguration);
            int txnCount = 0;
            foreach (var cp in store.GetCommitPoints())
            {
                var oldStore = fsm.OpenStore("c:\\brightstar\\twitteringtest\\", cp.LocationOffset);
                try
                {
                    oldStore.ExecuteSparqlQuery(TestQuery, SparqlResultsFormat.Xml);
                    Console.WriteLine("Query worked for commit point : {0} @ {1}", cp.LocationOffset, cp.CommitTime);
                    break;
                }
                catch (Exception)
                {
                    Console.WriteLine("Query failed for commit point : {0} @ {1}", cp.LocationOffset, cp.CommitTime);
                    txnCount++;
                }
            }
            var txnLog = fsm.GetTransactionLog("c:\\brightstar\\twitteringtest");
            var txnList = txnLog.GetTransactionList();
            for(int i = 0 ; i<= txnCount;i++)
            {
                txnList.MoveNext();
                var txnInfo = txnList.Current;
                Console.WriteLine("Transaction #{0}: Start: {1}, Status: {2}, JobId: {3}", i, txnInfo.TransactionStartTime, txnInfo.TransactionStatus, txnInfo.JobId);
            }

            // Going back to last known good
            store.RevertToCommitPoint(new CommitPoint(242472899 , 0, DateTime.UtcNow, Guid.Empty));

            var toReplay = new List<ITransactionInfo>();
            txnList = txnLog.GetTransactionList();
            for(int i = 0; i < 10 ; i++)
            {
                txnList.MoveNext();
                toReplay.Add(txnList.Current);
            }

            var storeWorker = new StoreWorker("c:\\brightstar","twitteringtest");
            for(int i = 9; i >= 0; i--)
            {
                Console.WriteLine("Applying transaction : {0}", toReplay[i].JobId);
                txnLog.GetTransactionData(toReplay[i].DataStartPosition);

                var jobId = Guid.NewGuid();
                var updateJob = new UpdateTransaction(jobId, null, storeWorker);
                updateJob.ReadTransactionDataFromStream(txnLog.GetTransactionData(toReplay[i].DataStartPosition));
                updateJob.Run();

                var readStore = storeWorker.ReadStore as Store;
                var resource = readStore.Resolve(1518601251);
                Assert.IsNotNull(resource);
                try
                {
                    var query = StoreExtensions.ParseSparql(TestQuery);
                    using (var resultStream = new MemoryStream())
                    {
                        storeWorker.Query(query, SparqlResultsFormat.Xml, resultStream,
                                          new[] {Constants.DefaultGraphUri});
                    }
                    Console.WriteLine("Query succeeded");
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Query failed: " + ex.Message);
                    Assert.Fail();
                }
            }
        }
    }
}
