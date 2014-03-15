using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Dto;
using BrightstarDB.Rdf;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class StoreWorkerTests
    {
        private readonly IStoreManager _storeManager = StoreManagerFactory.GetStoreManager();
        private string CreateStore()
        {
            var sid = Guid.NewGuid().ToString();
            using (_storeManager.CreateStore(Configuration.StoreLocation + Path.DirectorySeparatorChar + sid))
            {
                return sid;
            }
        }

        [Test]
        public void TestTransaction()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus status = storeWorker.GetJobStatus(jobId.ToString());
            while (status.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                status = storeWorker.GetJobStatus(jobId.ToString());
            }            
        }

        [Test]
        public void TestExportJob()
        {
            var sid = Guid.NewGuid().ToString();
            using (var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple("http://www.example.org/alice", "http://xmlns.org/foaf/0.1/knows",
                                   "http://www.example.org/bob", false, null, null, Constants.DefaultGraphUri);
                store.InsertTriple("http://www.example.org/bob", "http://xmlns.org/foaf/0.1/knows",
                                   "http://www.example.org/alice", false, null, null, Constants.DefaultGraphUri);
                store.Commit(Guid.NewGuid());
            }

            var storeWorker = new StoreWorker(Configuration.StoreLocation , sid);
            storeWorker.Start();
            var jobId = storeWorker.Export(sid + "_export.nt", null, RdfFormat.NQuads);
            JobExecutionStatus status = storeWorker.GetJobStatus(jobId.ToString());
            while (status.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                status = storeWorker.GetJobStatus(jobId.ToString());
                if (status.JobStatus == JobStatus.TransactionError)
                {
                    Assert.Fail("Export job failed with a transaction error. Message={0}. Exception Detail={1}", status.Information, status.ExceptionDetail);
                }
            }
        }

        [Test]
        public void TestStatsJob()
        {
            var sid = "StatsJob_" + DateTime.Now.Ticks;
            using (var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid))
            {
                store.InsertTriple("http://www.example.org/alice", "http://xmlns.org/foaf/0.1/knows",
                                   "http://www.example.org/bob", false, null, null, Constants.DefaultGraphUri);
                store.InsertTriple("http://www.example.org/alice", "http://xmlns.org/foaf/0.1/name", "Alice", true,
                                   RdfDatatypes.String, null, Constants.DefaultGraphUri);
                store.InsertTriple("http://www.example.org/bob", "http://xmlns.org/foaf/0.1/knows",
                                   "http://www.example.org/alice", false, null, null, Constants.DefaultGraphUri);
                store.Commit(Guid.NewGuid());
            }

            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();
            var jobId = storeWorker.UpdateStatistics();
            var status = storeWorker.GetJobStatus(jobId.ToString());
            while (status.JobStatus != JobStatus.CompletedOk && status.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                status = storeWorker.GetJobStatus(jobId.ToString());
            }
            Assert.AreEqual(JobStatus.CompletedOk, status.JobStatus, "Expected UpdateStatsJob to complete OK");
            var latestStats = storeWorker.StoreStatistics.GetStatistics().FirstOrDefault();
            Assert.IsNotNull(latestStats);
            Assert.AreEqual(3, latestStats.TripleCount);
            Assert.AreEqual(2, latestStats.PredicateTripleCounts.Count);
            Assert.IsTrue(latestStats.PredicateTripleCounts.ContainsKey("http://xmlns.org/foaf/0.1/knows"));
            Assert.AreEqual(2, latestStats.PredicateTripleCounts["http://xmlns.org/foaf/0.1/knows"]);
            Assert.IsTrue(latestStats.PredicateTripleCounts.ContainsKey("http://xmlns.org/foaf/0.1/name"));
            Assert.AreEqual(1, latestStats.PredicateTripleCounts["http://xmlns.org/foaf/0.1/name"]);
        }

        [Test]
        public void TestTransactionWithPreconditionFails()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string preconds = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/core/version> ""1""^^<http://www.w3.org/2000/01/rdf-schema#integer>";

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction(preconds, "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            Assert.AreEqual(jobStatus.JobStatus, JobStatus.TransactionError);
        }

        [Test]
        public void TestTransactionWithPrecondition()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            var data =
                @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np> .\n
                  <http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/core/version> ""1""^^<http://www.w3.org/2000/01/rdf-schema#integer> .";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk && jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }
            Assert.IsTrue(jobStatus.JobStatus == JobStatus.CompletedOk, "Initial insert failed: {0} : {1}", jobStatus.Information, jobStatus.ExceptionDetail);

            // now test precondition
            const string preconds = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/core/version> ""1""^^<http://www.w3.org/2000/01/rdf-schema#integer>";
            data =
                @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            jobId = storeWorker.ProcessTransaction(preconds, "", data, Constants.DefaultGraphUri, "nt");
            jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk && jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }
            Assert.IsTrue(jobStatus.JobStatus == JobStatus.CompletedOk, "Transaction execution failed: {0} : {1}", jobStatus.Information, jobStatus.ExceptionDetail);
        }

        [Test]
        public void TestReadTransactionList()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            var data =
                @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");

            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }            

            var transactionLog = storeWorker.TransactionLog;
            var transactionList = transactionLog.GetTransactionList();

            var i = 0;
            while (transactionList.MoveNext())
            {
                i++;
            }

            Assert.AreEqual(1, i);

            data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";
            jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");

            jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }            

            transactionList.Reset();

            i = 0;
            while (transactionList.MoveNext())
            {
                i++;
            }

            Assert.AreEqual(2, i);
        }

        [Test]
        public void TestRecoverTransactionData()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");

            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            var transactionLog = storeWorker.TransactionLog;
            var transactionList = transactionLog.GetTransactionList();

            var i = 0;
            while (transactionList.MoveNext())
            {
                i++;
            }

            Assert.AreEqual(1, i);

            // now get txn data
            var txnList = storeWorker.TransactionLog.GetTransactionList();
            txnList.MoveNext();
            var tinfo = txnList.Current;
            Assert.IsNotNull(tinfo);
            Assert.AreEqual(TransactionType.UpdateTransaction, tinfo.TransactionType);
            Assert.AreEqual(TransactionStatus.CompletedOk, tinfo.TransactionStatus);
            Assert.IsTrue(tinfo.TransactionStartTime < DateTime.UtcNow);

            var job = new UpdateTransaction(Guid.NewGuid(), null, storeWorker);
            using (var tdStream = storeWorker.TransactionLog.GetTransactionData(tinfo.DataStartPosition))
            {
                job.ReadTransactionDataFromStream(tdStream);
            }
            Assert.IsNotNull(job);
            Assert.AreEqual(data, job.InsertData);
            
        }


        [Test]
        public void TestFailedTransactionAppearsInTransactionList()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transaction with bad data
            var data =
                @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk && jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            var transactionLog = storeWorker.TransactionLog;
            var transactionList = transactionLog.GetTransactionList();

            var i = 0;
            while (transactionList.MoveNext())
            {
                i++;
            }

            Assert.AreEqual(1, i);

            var txnList = storeWorker.TransactionLog.GetTransactionList();
            txnList.MoveNext();
            var tinfo = txnList.Current;
            Assert.IsNotNull(tinfo);
            Assert.AreEqual(TransactionType.UpdateTransaction, tinfo.TransactionType);
            Assert.AreEqual(TransactionStatus.Failed, tinfo.TransactionStatus);

            data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";
            jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");

            jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            transactionList.Reset();

            i = 0;
            while (transactionList.MoveNext())
            {
                i++;
            }

            Assert.AreEqual(2, i);
        }

        [Test]
        public void TestGetErrorMessage()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");

            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            Assert.IsTrue(jobStatus.Information.Contains("Job Error"), "Unexpected job message: '{0}'", jobStatus.Information);
            Assert.IsTrue(jobStatus.ExceptionDetail.Message.Contains("Syntax error in triples to add."), "Unexpected job message: '{0}'", jobStatus.ExceptionDetail.Message);
        }

        [Test]
        public void TestDeleteStoreAfterQuery()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            storeWorker.Query("select * where { ?s ?p ?o }", SparqlResultsFormat.Xml, new string[]{Constants.DefaultGraphUri});
            storeWorker.Shutdown(true, () => _storeManager.DeleteStore(Configuration.StoreLocation + "\\"+ sid));            
        }

        [Test]
        public void TestDeleteStoreAfterUpdate()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            // var queryResult = storeWorker.Query("select * where { ?s ?p ?o }");
            storeWorker.Shutdown(true, () => _storeManager.DeleteStore(Configuration.StoreLocation +"\\" + sid));
            
        }

        [Test]
        [Ignore]
        public void TestConsolidateStore()
        {
            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            var data =
                @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            jobId = Guid.NewGuid();
            storeWorker.QueueJob(new ConsolidateJob(jobId, null, storeWorker));
            jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk && jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            Assert.AreEqual(JobStatus.CompletedOk, jobStatus.JobStatus);

            // open store and find all triples
            var results = storeWorker.Query("select * where { ?a ?b ?c }", SparqlResultsFormat.Xml, new string[] { Constants.DefaultGraphUri });
            var doc = XDocument.Parse(results);
            XNamespace sparqlNs = "http://www.w3.org/2005/sparql-results#";
            Assert.AreEqual(1, doc.Descendants(sparqlNs + "result").Count());

            data =
                @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            jobId = storeWorker.ProcessTransaction("", data, "", Constants.DefaultGraphUri, "nt");
            jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            Assert.AreEqual(JobStatus.CompletedOk, jobStatus.JobStatus);

            // consolidate again
            jobId = Guid.NewGuid();
            storeWorker.QueueJob(new ConsolidateJob(jobId, null, storeWorker));
            jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk && jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }

            Assert.AreEqual(JobStatus.CompletedOk, jobStatus.JobStatus);

            results = storeWorker.Query("select * where { ?a ?b ?c }", SparqlResultsFormat.Xml, new string[] { Constants.DefaultGraphUri });
            doc = XDocument.Parse(results);
            Assert.AreEqual(0, doc.Descendants(sparqlNs + "result").Count());
        }

        [Test]
        public void TestQueryCaching()
        {
            Configuration.EnableQueryCache = true;

            // create a store
            var sid = CreateStore();

            // initialise and start the store worker
            var storeWorker = new StoreWorker(Configuration.StoreLocation, sid);
            storeWorker.Start();

            // execute transactions
            const string data = @"<http://www.networkedplanet.com/people/gra> <http://www.networkedplanet.com/types/worksfor> <http://www.networkedplanet.com/companies/np>";

            var jobId = storeWorker.ProcessTransaction("", "", data, Constants.DefaultGraphUri, "nt");
            JobExecutionStatus jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            while (jobStatus.JobStatus != JobStatus.CompletedOk && jobStatus.JobStatus != JobStatus.TransactionError)
            {
                Thread.Sleep(1000);
                jobStatus = storeWorker.GetJobStatus(jobId.ToString());
            }
            Assert.AreEqual(JobStatus.CompletedOk, jobStatus.JobStatus, "Import failed: {0} : {1}", jobStatus.Information, jobStatus.ExceptionDetail);

            var sw = new Stopwatch();
            sw.Start();
            var queryResult = storeWorker.Query("select * where { ?a ?b ?c }", SparqlResultsFormat.Xml, new string[] { Constants.DefaultGraphUri });
            sw.Stop();
            Console.WriteLine("initial query took : " + sw.ElapsedMilliseconds);
            var initTime = sw.ElapsedMilliseconds;

            sw = new Stopwatch();
            sw.Start();
            var cachedResult = storeWorker.Query("select * where { ?a ?b ?c }", SparqlResultsFormat.Xml, new string[] { Constants.DefaultGraphUri });
            sw.Stop();
            Console.WriteLine("warm query took : " + sw.ElapsedMilliseconds);

            Thread.Sleep(1000);

            sw = new Stopwatch();
            sw.Start();
            cachedResult = storeWorker.Query("select * where { ?a ?b ?c }", SparqlResultsFormat.Xml, new string[] { Constants.DefaultGraphUri });
            sw.Stop();
            Console.WriteLine("cached query took : " + sw.ElapsedMilliseconds);
            var cachedTime = sw.ElapsedMilliseconds;

            Assert.AreEqual(queryResult, cachedResult);
            if (cachedTime >= initTime)
            {
                Assert.Inconclusive(
                    "Expected time to read from cache ({0}ms) to be less than time to execute query ({1}ms).",
                    cachedTime, initTime);
            }

            Configuration.EnableQueryCache = false;
        }
    }
}
