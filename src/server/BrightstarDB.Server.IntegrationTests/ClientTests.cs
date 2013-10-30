
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
{
    [TestFixture]
    public class ClientTests : ClientTestBase
    {
        private readonly string _storeLocation;
        public ClientTests()
        {
#if PORTABLE
            var appSettings = new System.Configuration.AppSettingsReader();
            _storeLocation = appSettings.GetValue("BrightstarDB.StoreLocation", typeof (string)) as string;
#else
            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            _storeLocation = appSettings.Get("BrightstarDB.StoreLocation");
#endif
        }
        private static IBrightstarService GetClient()
        {
            return BrightstarService.GetClient("type=rest;endpoint=http://localhost:8090/brightstar");
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
        public void TestCreateStore()
        {
            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);
        }

        [Test]
        public void TestInvalidStoreNames()
        {
            var bc = GetClient();
            try
            {
                bc.CreateStore(null);
                Assert.Fail("Expected ArgumentNullException");
            } catch(ArgumentNullException)
            {
                // Expected
            }

            try
            {
                bc.CreateStore(String.Empty);
                Assert.Fail("Expected ArgumentException (empty string)");
            } catch(ArgumentException)
            {
                // Expected
            }

            try
            {
                bc.CreateStore("This is\\an invalid\\store name");
                Assert.Fail("Expected ArgumentException (backslash in name)");
            }catch(ArgumentException)
            {
                //Expected
            }

            try
            {
                bc.CreateStore("This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.This is an invalid store name because it is too long.");
                Assert.Fail("Expected ArgumentException (name too long)");
            } catch(ArgumentException)
            {
                // Expected
            }
        }

        [Test]
        public void TestIfStoreExistsFalseWhenNoStoreCreated()
        {
            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            var exists = bc.DoesStoreExist(sid);
            Assert.IsFalse(exists);
        }

        [Test]
        public void TestIfStoreExistsTrueAfterStoreCreated()
        {
            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);
            var exists = bc.DoesStoreExist(sid);
            Assert.IsTrue(exists);
        }

        [Test]
        [ExpectedException(typeof(BrightstarClientException))]
        public void TestCreateDuplicateStoreFails()
        {

            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);
            bc.CreateStore(sid);
        }

        [Test]
        public void TestDeleteStore()
        {
            var bc = GetClient();

            // create store
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);

            // check it is there
            var stores = bc.ListStores();
            Assert.AreEqual(1, stores.Where(s => s.Equals(sid)).Count());

            // delete store
            bc.DeleteStore(sid);

            // check it is gone
            stores = bc.ListStores();
            Assert.AreEqual(0, stores.Where(s => s.Equals(sid)).Count());
        }

        [Test]
        public void TestListStores()
        {
            var bc = GetClient();
            var stores = bc.ListStores();
            Assert.IsTrue(stores.Count() > 0);
        }

        [Test]
        public void TestQuery()
        {
            var client = GetClient();
            var storeName  = "Client.TestQuery_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);
            client.ExecuteQuery(storeName, "SELECT ?s WHERE { ?s ?p ?o }");
        }

        [Test]
        public void TestQueryIfNotModifiedSince()
        {
            var client = GetClient();
            var storeName = "Client.TestQueryIfNotModifiedSince_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);
            client.ExecuteQuery(storeName, "SELECT ?s WHERE { ?s ?p ?o }");
            var lastResponseTime = client.LastResponseTimestamp;
            Assert.IsNotNull(lastResponseTime);
            try
            {
                client.ExecuteQuery(storeName, "SELECT ?s WHERE {?s ?p ?o}", lastResponseTime);
                Assert.Fail("Expected a BrightstarClientException");
            }
            catch (BrightstarClientException clientException)
            {
                //Assert.AreEqual(typeof (BrightstarStoreNotModifiedException).FullName,
                //                clientException.InnerException.Type);
                Assert.AreEqual("Store not modified", clientException.Message);
            }
        }

        [Test]
        public void TestListJobs()
        {
            var client = GetClient();
            var storeName = "ListJobs_" + DateTime.Now.Ticks;
            var batch1 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource1> .";
            var batch2 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource2> .";
            var batch3 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource3> .";


            // Prepare store with three consecutive commits
            client.CreateStore(storeName);
            var jobInfo = client.ExecuteTransaction(storeName, null, null, batch1);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch2);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch3);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);

            var jobs = client.GetJobInfo(storeName, 0, 10).ToList();
            Assert.That(jobs, Is.Not.Null);
            Assert.That(jobs, Has.Count.EqualTo(3));

            jobs = client.GetJobInfo(storeName, 1, 10).ToList();
            Assert.That(jobs, Is.Not.Null);
            Assert.That(jobs, Has.Count.EqualTo(2));

            jobs = client.GetJobInfo(storeName, 1, 1).ToList();
            Assert.That(jobs, Is.Not.Null);
            Assert.That(jobs, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestQueryCommitPoint()
        {
            var client = GetClient();
            var storeName = "QueryCommitPoint_" + DateTime.Now.Ticks;
            var batch1 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource1> .";
            var batch2 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource2> .";
            var batch3 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource3> .";


            // Prepare store with three consecutive commits
            client.CreateStore(storeName);
            var jobInfo = client.ExecuteTransaction(storeName, null, null, batch1);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch2);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch3);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);

            // Retrieve commit points - there should be one initial one plus one for each transaction we executed
            var commits = client.GetCommitPoints(storeName, 0, 10).ToList();
            Assert.That(commits, Has.Count.EqualTo(4));

            // Each commit added one match to this query, so each commit point we step back through should
            // return one less result row.
            for (int i = 0; i < 4; i++)
            {
                var resultsStream = client.ExecuteQuery(commits[i],
                                    "SELECT ?p WHERE { <http://example.org/resource> <http://example.org/property> ?p }");
                var resultsDoc = XDocument.Load(resultsStream);
                Assert.That(resultsDoc.SparqlResultRows().ToList(), Has.Count.EqualTo(3-i));
            }
        }

        const string Batch1 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource1> .";
        const string Batch2 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource2> .";
        const string Batch3 = @"<http://example.org/resource> <http://example.org/property> <http://example.org/resource3> .";

        private static void PreloadStore(IBrightstarService client, string storeName, params string[] insertBatches)
        {
            foreach (var insertBatch in insertBatches)
            {
                var jobInfo = client.ExecuteTransaction(storeName, null, null, insertBatch);
                jobInfo = WaitForJob(jobInfo, client, storeName);
                Assert.That(jobInfo.JobCompletedOk);
            }
        }

        [Test]
        public void TestRevertCommit()
        {
            var client = GetClient();
            var storeName = "QueryCommitPoint_" + DateTime.Now.Ticks;

            // Prepare store with three consecutive commits
            client.CreateStore(storeName);
            PreloadStore(client, storeName, Batch1, Batch2, Batch3);

            // Retrieve commit points - there should be one initial one plus one for each transaction we executed
            var commits = client.GetCommitPoints(storeName, 0, 10).ToList();
            Assert.That(commits, Has.Count.EqualTo(4));

            // Before the revert we should have the last of the three patterns in the store
            AssertTriplePatternInDefaultGraph(client, storeName, Batch3);

            // Attempt to revert to the last-but-one commit
            client.RevertToCommitPoint(storeName, commits[1]);

            // After the revert the last of the three patterns should no longer be in the store
            AssertTriplePatternNotInDefaultGraph(client, storeName, Batch3);
            // But the first and second of the three should
            AssertTriplePatternInDefaultGraph(client, storeName, Batch1);
            AssertTriplePatternInDefaultGraph(client, storeName, Batch2);
        }

        [Test]
        [Ignore("Need to fix this in embedded client first")]
        public void TestRepeatTransaction()
        {
            var client = GetClient();
            var storeName = "RepeatTransaction_" + DateTime.Now.Ticks;

            client.CreateStore(storeName);
            PreloadStore(client, storeName, Batch1, Batch2, Batch3);

            // Retrieve commit points
            var commits = client.GetCommitPoints(storeName, 0, 10).ToList();
            Assert.That(commits, Has.Count.EqualTo(4));

            AssertTriplePatternInDefaultGraph(client, storeName, Batch3);

            // Reverting back to the point where we only had Batch1
            client.RevertToCommitPoint(storeName, commits[2]);

            // Check the revert worked as expected
            AssertTriplePatternInDefaultGraph(client, storeName, Batch1);
            AssertTriplePatternNotInDefaultGraph(client, storeName, Batch3);
            AssertTriplePatternNotInDefaultGraph(client, storeName, Batch2);

            // Repeat the transaction that added batch3
            var txn = client.GetTransaction(storeName, commits[0].JobId);
            Assert.That(txn, Is.Not.Null);
            var job = client.ReExecuteTransaction(storeName, txn);
            job = WaitForJob(job, client, storeName);
            Assert.That(job.JobCompletedOk, "Re-execute transaction failed: " + job.StatusMessage);

            // Check that after re-execution Batch3 is now in the store, but Batch2 still isn't
            AssertTriplePatternInDefaultGraph(client, storeName, Batch1);
            AssertTriplePatternInDefaultGraph(client, storeName, Batch3);
            AssertTriplePatternNotInDefaultGraph(client, storeName, Batch2);
        }

        [Test]
        public void TestListTransactionsAndGetTransaction()
        {
            var client = GetClient();
            var storeName = "ListTransactions_" + DateTime.Now.Ticks;

            client.CreateStore(storeName);
            PreloadStore(client, storeName, Batch1, Batch2, Batch3);

            var transactions = client.GetTransactions(storeName, 0, 10).ToList();
            Assert.That(transactions, Has.Count.EqualTo(3));

            transactions = client.GetTransactions(storeName, 1, 10).ToList();
            Assert.That(transactions, Has.Count.EqualTo(2));

            transactions = client.GetTransactions(storeName, 1, 1).ToList();
            Assert.That(transactions, Has.Count.EqualTo(1));

            // Try using the API to get a single transaction
            var txn = client.GetTransaction(storeName, transactions[0].JobId);
            Assert.That(txn, Is.Not.Null);
            Assert.That(txn.Id, Is.EqualTo(transactions[0].Id));

            txn = client.GetTransaction(storeName, Guid.NewGuid());
            Assert.That(txn, Is.Null);
        }

        [Test]
        public void TestTransactionAddStatements()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triplesToAdd =
                @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2> .";

            var jobInfo = bc.ExecuteTransaction(storeName,"", "", triplesToAdd);

            Assert.IsNotNull(jobInfo);

            while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }

            AssertTriplePatternInDefaultGraph(bc, storeName, triplesToAdd);
        }

        [Test] public void TestTransactiondDeleteStatements()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triplesToAdd =
                    @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.";
            
            var jobInfo = bc.ExecuteTransaction(storeName,"", "", triplesToAdd);

            Assert.IsNotNull(jobInfo);

            while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }

            AssertTriplePatternInDefaultGraph(bc, storeName, triplesToAdd);

            var deletePatterns = @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.";

            jobInfo = bc.ExecuteTransaction(storeName, "", deletePatterns, "");

            while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }

            AssertTriplePatternNotInDefaultGraph(bc, storeName, triplesToAdd);
        }

        [Test]
        public void TestSparqlQuery()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triplesToAdd =
                    @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.";

            var jobInfo = bc.ExecuteTransaction(storeName, "", "", triplesToAdd);

            Assert.IsNotNull(jobInfo);

            while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }

            //var triples = bc.GetStoreData(storeName);
            //var memoryStream = new MemoryStream();
            //triples.CopyTo(memoryStream);
            //Assert.IsTrue(0 < memoryStream.Length);

            // do query
            var result = bc.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource13> ?p ?o }");
            Assert.IsNotNull(result);
        }

        [Test]
        public void TestSparqlQueryWithDefaultGraph()
        {
            var client = GetClient();
            var storeName = "SparqlQueryWithDefaultGraph_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            var triplesToAdd = new StringBuilder();
            triplesToAdd.AppendLine(@"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.");
            triplesToAdd.AppendLine(
                @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource3> <http://example.org/graph1> .");

            var jobInfo = client.ExecuteTransaction(storeName, "", "", triplesToAdd.ToString());
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.IsNotNull(jobInfo);
            Assert.IsTrue(jobInfo.JobCompletedOk);

            // do query
            var resultStream = client.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource13> ?p ?o }", "http://example.org/graph1");
            var result = XDocument.Load(resultStream);
            var rows = result.SparqlResultRows().ToList();
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(new Uri("http://example.org/resource3"), rows[0].GetColumnValue("o"));

            // Do a query over the normal default graph
            resultStream = client.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource13> ?p ?o }");
            result = XDocument.Load(resultStream);
            rows = result.SparqlResultRows().ToList();
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(new Uri("http://example.org/resource2"), rows[0].GetColumnValue("o"));
        }

        [Test]
        public void TestSparqlQueryWithDefaultGraphs()
        {
            var client = GetClient();
            var storeName = "SparqlQueryWithDefaultGraphs_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            var triplesToAdd = new StringBuilder();
            triplesToAdd.AppendLine(@"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.");
            triplesToAdd.AppendLine(
                @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource3> <http://example.org/graph1> .");
            triplesToAdd.AppendLine(
                @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource4> <http://example.org/graph2> .");

            var jobInfo = client.ExecuteTransaction(storeName, "", "", triplesToAdd.ToString());
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.IsNotNull(jobInfo);
            Assert.IsTrue(jobInfo.JobCompletedOk);

            // do query using graph1 and graph2 as the default
            var resultStream = client.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource13> ?p ?o }", 
                new[] {"http://example.org/graph1", "http://example.org/graph2"});
            var result = XDocument.Load(resultStream);
            var rows = result.SparqlResultRows().ToList();
            Assert.AreEqual(2, rows.Count);
            var expected = new[] {new Uri("http://example.org/resource3"), new Uri("http://example.org/resource4")};
            Assert.IsTrue(expected.Contains(rows[0].GetColumnValue("o")));
            Assert.IsTrue(expected.Contains(rows[1].GetColumnValue("o")));

            // Do a query over the normal default graph
            resultStream = client.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource13> ?p ?o }");
            result = XDocument.Load(resultStream);
            rows = result.SparqlResultRows().ToList();
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(new Uri("http://example.org/resource2"), rows[0].GetColumnValue("o"));
            
        }
        [Test]
        public void TestSparqlXDocumentExtensions()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triplesToAdd =
                    @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2> .
                      <http://example.org/resource14> <http://example.org/property1> ""30""^^<http://www.w3.org/2001/XMLSchema#integer> . ";

            var jobInfo = bc.ExecuteTransaction(storeName, "", "", triplesToAdd);
            jobInfo = WaitForJob(jobInfo, bc, storeName);
            Assert.IsNotNull(jobInfo);
            Assert.IsTrue(jobInfo.JobCompletedOk);

           // var triples = bc.GetStoreData(storeName);
            //var memoryStream = new MemoryStream();
            //triples.CopyTo(memoryStream);
            //Assert.IsTrue(0 < memoryStream.Length);

            // do query
            var result = bc.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource13> ?p ?o }");

            var doc = XDocument.Load(result);
            var resultRows = doc.SparqlResultRows();

            Assert.AreEqual(1, resultRows.Count());

            foreach (var row in resultRows)
            {
                var p = row.GetColumnValue("p");
                var o = row.GetColumnValue("o");

                Assert.AreEqual("http://example.org/property", p.ToString());
                Assert.AreEqual("http://example.org/resource2", o.ToString());
                Assert.IsNull(row.GetColumnValue("z"));
                Assert.IsFalse(row.IsLiteral("p"));
                Assert.IsFalse(row.IsLiteral("o"));
            }

            result = bc.ExecuteQuery(storeName, "select ?p ?o where { <http://example.org/resource14> ?p ?o }");
            doc = XDocument.Load(result);
            resultRows = doc.SparqlResultRows();

            Assert.AreEqual(1, resultRows.Count());

            foreach (var row in resultRows)
            {
                var p = row.GetColumnValue("p");
                var o = row.GetColumnValue("o");

                Assert.AreEqual("http://example.org/property1", p.ToString());
                Assert.AreEqual(30, o);
                Assert.AreEqual("http://www.w3.org/2001/XMLSchema#integer", row.GetLiteralDatatype("o"));
                Assert.IsNull(row.GetLiteralDatatype("p"));
                Assert.IsNull(row.GetColumnValue("z"));
                Assert.IsFalse(row.IsLiteral("p"));
                Assert.IsTrue(row.IsLiteral("o"));
                Assert.IsInstanceOf(typeof(Int32), o);
            }
        }

        [Test]
        public void TestSparqlUpdate()
        {
            var client = GetClient();
            var storeName = "SparqlUpdate_" + DateTime.Now.Ticks;
            const string initialTriples = @"<http://example.org/foo> <http://example.org/p> ""foo"".";
            const string updateExpression = @"DELETE { ?s ?p 'foo' }
INSERT { ?s ?p 'bar' }
WHERE { ?s ?p 'foo' }";

            client.CreateStore(storeName);
            var jobInfo = client.ExecuteTransaction(storeName, null, null, initialTriples);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);

            // Attempt update
            jobInfo = client.ExecuteUpdate(storeName, updateExpression);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.That(jobInfo.JobCompletedOk);

            // Verify results
            var resultsStream = client.ExecuteQuery(storeName, "SELECT ?v WHERE { <http://example.org/foo> <http://example.org/p> ?v }");
            var resultsDoc = XDocument.Load(resultsStream);
            var resultsRows = resultsDoc.SparqlResultRows().ToList();
            Assert.That(resultsRows, Has.Count.EqualTo(1));
            var row = resultsRows[0];
            var result = row.GetColumnValue("v");
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<PlainLiteral>());
            Assert.That((result as PlainLiteral).Value, Is.EqualTo("bar"));
        }
        [Test]
        public void TestPassingNullForData()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triplesToAdd =
                @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.";

            var jobInfo = bc.ExecuteTransaction(storeName, "", null, triplesToAdd);

            Assert.IsNotNull(jobInfo);

            while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }

            //var triples = bc.GetStoreData(storeName);
            //var memoryStream = new MemoryStream();
            //triples.CopyTo(memoryStream);
            //memoryStream.Flush();
            //Assert.IsTrue(0 < memoryStream.Length);
        }

        [Test]
        public void TestBadDataGetsUsefulErrorMessage()
        {
            string sid = null;
            try
            {
                var bc = GetClient();
                sid = Guid.NewGuid().ToString();
                bc.CreateStore(sid);
                bc.CreateStore(sid);
            } catch (BrightstarClientException ex)
            {
                var detail = ex.Message;
                Assert.That(detail, Is.EqualTo("Store name conflicts with the name of an existing store."));
            }
        }

        [Test]
        public void TestExportWhileWriting()
        {
            int firstBatchSize = 50000;
            var storeName = Guid.NewGuid().ToString();
            var client = GetClient();
            client.CreateStore(storeName);
            var batch1 = MakeTriples(0, firstBatchSize);
            var batch2 = MakeTriples(firstBatchSize, firstBatchSize+1000);
            var batch3 = MakeTriples(firstBatchSize+1000, firstBatchSize+2000);
            var batch4 = MakeTriples(firstBatchSize+2000, firstBatchSize+3000);

            // Verify batch size
            var p = new NTriplesParser();
            var counterSink = new CounterTripleSink();
            p.Parse(new StringReader(batch1), counterSink, Constants.DefaultGraphUri);
            Assert.AreEqual(firstBatchSize, counterSink.Count);

            var jobInfo = client.ExecuteTransaction(storeName, String.Empty, String.Empty, batch1);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);

            // Second export with parallel store writes
            var exportJobInfo = client.StartExport(storeName, storeName + "_export.nt");

            jobInfo = client.ExecuteTransaction(storeName, null, null, batch2);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);

            exportJobInfo = client.GetJobInfo(storeName, exportJobInfo.JobId);
            if (exportJobInfo.JobCompletedWithErrors)
            {
                Assert.Fail("Export job completed with errors: {0} : {1}", exportJobInfo.StatusMessage, exportJobInfo.ExceptionInfo);
            }
            if (exportJobInfo.JobCompletedOk)
            {
                Assert.Inconclusive("Export job completed before end of first concurrent import job.");
            }
            
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch3);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);

            jobInfo = client.ExecuteTransaction(storeName, null, null, batch4);
            jobInfo = WaitForJob(jobInfo, client, storeName);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);

            exportJobInfo = WaitForJob(exportJobInfo, client, storeName);
            Assert.That(exportJobInfo.JobCompletedOk);

            var exportFile = new FileInfo("c:\\brightstar\\import\\" + storeName + "_export.nt");
            Assert.IsTrue(exportFile.Exists);
            var lineCount = File.ReadAllLines(exportFile.FullName).Where(x => !String.IsNullOrEmpty(x)).Count();
            Assert.AreEqual(firstBatchSize, lineCount);
        }

        public class CounterTripleSink : ITripleSink
        {
            private int _count = 0;
            public int Count { get { return _count; } }

            #region Implementation of ITripleSink

            /// <summary>
            /// Handler method for an individual RDF statement
            /// </summary>
            /// <param name="subject">The statement subject resource URI</param>
            /// <param name="subjectIsBNode">True if the value of <paramref name="subject"/> is a BNode identifier</param>
            /// <param name="predicate">The predicate resource URI</param>
            /// <param name="predicateIsBNode">True if the value of <paramref name="predicate"/> is a BNode identifier.</param>
            /// <param name="obj">The object of the statement</param>
            /// <param name="objIsBNode">True if the value of <paramref name="obj"/> is a BNode identifier.</param>
            /// <param name="objIsLiteral">True if the value of <paramref name="obj"/> is a literal string</param>
            /// <param name="dataType">The datatype URI for the object literal or null if the object is not a literal</param>
            /// <param name="langCode">The language code for the object literal or null if the object is not a literal</param>
            /// <param name="graphUri">The graph URI for the statement</param>
            public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool objIsLiteral, string dataType, string langCode, string graphUri)
            {
                _count++;
            }

            #endregion
        }

        private string MakeTriples(int startId, int endId)
        {
            StringBuilder triples = new StringBuilder();
            for(int i = startId; i < endId; i++ )
            {
                triples.AppendFormat("<http://www.example.org/resource/{0}> <http://example.org/value> \"{0}\" .\n",i);
            }
            return triples.ToString();
        }

        [Test]
        public void TestSpecialCharsInIdentities()
        {
            var importDir = Path.Combine(_storeLocation, "import");
            if (!Directory.Exists(importDir))
            {
                Directory.CreateDirectory(importDir);
            }
            var testTarget = new FileInfo(importDir + Path.DirectorySeparatorChar + "persondata_en_subset.nt");
            if (!testTarget.Exists)
            {
                var testSource = new FileInfo("persondata_en_subset.nt");
                if (!testSource.Exists)
                {
                    Assert.Inconclusive("Could not locate test source file {0}. Test will not run", testSource.FullName);
                    return;
                }
                testSource.CopyTo(importDir + Path.DirectorySeparatorChar + "persondata_en_subset.nt");
            }

            var bc = BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var jobInfo = bc.StartImport(storeName, "persondata_en_subset.nt", null);
            while (!(jobInfo.JobCompletedOk || jobInfo.JobCompletedWithErrors))
            {
                Thread.Sleep(1000);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }
            Assert.IsTrue(jobInfo.JobCompletedOk, "Import job failed");

            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + _storeLocation + "\\"));
            var store = context.OpenStore(storeName);

            var test = store.BindDataObjectsWithSparql("SELECT ?p WHERE {?p a <http://xmlns.com/foaf/0.1/Person>} LIMIT 30").ToList();
            Assert.IsNotNull(test);

            foreach (var testDo in test)
            {
                Assert.IsNotNull(testDo);

                var propValues = testDo.GetPropertyValues("http://xmlns.com/foaf/0.1/name").OfType<PlainLiteral>();
                Assert.IsNotNull(propValues);
                Assert.IsTrue(propValues.Count() > 0);

            }
        }

        [Test]
        public void TestConsolidateEmptyStore()
        {
            var storeName = "ConsolidateEmptyStore_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            var job = client.ConsolidateStore(storeName);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk, "Job did not complete successfully: {0} : {1}", job.StatusMessage, job.ExceptionInfo);
        }

        private static IJobInfo WaitForJob(IJobInfo job, IBrightstarService client, string storeName)
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

        [Test]
        public void TestConsolidatePopulatedStore()
        {
            var storeName = "ConsolidatePopulatedStore_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            const string addSet1 = "<http://example.org/people/alice> <http://www.w3.org/2000/01/rdf-schema#label> \"Alice\".";
            const string addSet2 = "<http://example.org/people/bob> <http://www.w3.org/2000/01/rdf-schema#label> \"Bob\".";
            const string addSet3 = "<http://example.org/people/carol> <http://www.w3.org/2000/01/rdf-schema#label> \"Carol\".";
            
            var result = client.ExecuteTransaction(storeName, null, null, addSet1);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);
            
            result = client.ExecuteTransaction(storeName, null, null, addSet2);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            result = client.ExecuteTransaction(storeName, null, null, addSet3);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            var job = client.ConsolidateStore(storeName);
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
            Assert.IsTrue(job.JobCompletedOk, "Job did not complete successfully: {0} : {1}", job.StatusMessage, job.ExceptionInfo);

        }

        [Test]
        public void TestConsolidatePopulatedStoreAfterQuery()
        {
            var storeName = "ConsolidatePopulatedStore_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            const string addSet1 = "<http://example.org/people/alice> <http://www.w3.org/2000/01/rdf-schema#label> \"Alice\".";
            const string addSet2 = "<http://example.org/people/bob> <http://www.w3.org/2000/01/rdf-schema#label> \"Bob\".";
            const string addSet3 = "<http://example.org/people/carol> <http://www.w3.org/2000/01/rdf-schema#label> \"Carol\".";
            var result = client.ExecuteTransaction(storeName, null, null, addSet1);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            result = client.ExecuteTransaction(storeName, null, null, addSet2);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);
            
            result = client.ExecuteTransaction(storeName, null, null, addSet3);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            var resultsStream = client.ExecuteQuery(storeName, "SELECT * WHERE {?s ?p ?o}");
            resultsStream.Close();
            
            var job = client.ConsolidateStore(storeName);
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
            Assert.IsTrue(job.JobCompletedOk, "Job did not complete successfully: {0} : {1}", job.StatusMessage, job.ExceptionInfo);

        }

        [Test]
        public void TestInsertQuadsIntoDefaultGraph()
        {
            var client = GetClient();
            var storeName = "QuadsTransaction1_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            const string txn1Adds =
                @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .
<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .";
            var result = client.ExecuteTransaction(storeName, null, null, txn1Adds);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInDefaultGraph(client, storeName, @"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob""");
            AssertTriplePatternInGraph(client, storeName, @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice""",
                "http://example.org/graphs/alice");
        }

        [Test]
        public void TestInsertQuadsIntoNonDefaultGraph()
        {
            var client = GetClient();
            var storeName = "QuadsTransaction2_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            const string txn1Adds =
    @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .
<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .";
            var result = client.ExecuteTransaction(storeName, null, null, txn1Adds, "http://example.org/graphs/bob");
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInGraph(client, storeName, @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice""",
                "http://example.org/graphs/alice");
            AssertTriplePatternInGraph(client, storeName, @"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob""",
                "http://example.org/graphs/bob");
        }

        [Test]
        public void TestUpdateQuadsUsingDefaultGraph()
        {
            var client = GetClient();
            var storeName = "QuadsTransaction3_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            var txn1Adds = new StringBuilder();
            txn1Adds.AppendLine(
                @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .");
            txn1Adds.AppendLine(@"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .");
            var result = client.ExecuteTransaction(storeName, null, null, txn1Adds.ToString());
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInDefaultGraph(client, storeName, @"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob""");
            AssertTriplePatternInGraph(client, storeName, @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice""",
                "http://example.org/graphs/alice");

            var txn2Adds = new StringBuilder();
            txn2Adds.AppendLine(@"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice Arnold"" <http://example.org/graphs/alice> .");
            txn2Adds.AppendLine(@"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob Bobbins"" .");

            result = client.ExecuteTransaction(storeName, txn1Adds.ToString(), txn1Adds.ToString(), txn2Adds.ToString());
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInGraph(client, storeName,
                                       @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice Arnold""",
                                       "http://example.org/graphs/alice");
            AssertTriplePatternInDefaultGraph(client, storeName,
                                       @"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob Bobbins""");
        }

        [Test]
        public void TestUpdateQuadsUsingNonDefaultGraph()
        {
            var client = GetClient();
            var storeName = "QuadsTransaction4_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            const string txn1Adds =
                @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .
<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .";
            var result = client.ExecuteTransaction(storeName, null, null, txn1Adds, "http://example.org/graphs/bob");
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInGraph(client, storeName,
                                       @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice""",
                                       "http://example.org/graphs/alice");
            AssertTriplePatternInGraph(client, storeName,
                                       @"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob""",
                                       "http://example.org/graphs/bob");

            const string txn2Adds =
                @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice Arnold"" <http://example.org/graphs/alice> .
<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob Bobbins"" .";
            
            result = client.ExecuteTransaction(storeName, txn1Adds, txn1Adds, txn2Adds, "http://example.org/graphs/bob");
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInGraph(client, storeName,
                                       @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice Arnold""",
                                       "http://example.org/graphs/alice");
            AssertTriplePatternInGraph(client, storeName,
                                       @"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob Bobbins""",
                                       "http://example.org/graphs/bob");

        }


        [Test]
        public void TestTransactionWithWildcardGraph()
        {
            var client = GetClient();
            var storeName = "QuadsTransaction5_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            var txn1Adds = new StringBuilder();
            txn1Adds.AppendLine(@"<http://example.org/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .");
            txn1Adds.AppendLine(
                @"<http://example.org/alice> <http://xmlns.com/foaf/0.1/mbox> ""alice@example.org"" <http://example.org/graphs/alice> .");
            txn1Adds.AppendLine(@"<http://example.org/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .");
            txn1Adds.AppendLine(@"<http://example.org/bob> <http://xmlns.com/foaf/0.1/mbox> ""bob@example.org"" .");

            var result = client.ExecuteTransaction(storeName, null, null, txn1Adds.ToString());
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            AssertTriplePatternInGraph(client, storeName,
                                       @"<http://example.org/alice> <http://xmlns.com/foaf/0.1/name> ""Alice""",
                                       "http://example.org/graphs/alice");
            AssertTriplePatternInDefaultGraph(client, storeName,
                                       @"<http://example.org/bob> <http://xmlns.com/foaf/0.1/name> ""Bob""");

            var txn2Deletes = new StringBuilder();
            txn2Deletes.AppendFormat(@"<{0}> <http://xmlns.com/foaf/0.1/name> <{0}> <{0}> .", Constants.WildcardUri);
            result = client.ExecuteTransaction(storeName, null, txn2Deletes.ToString(), null);
            result = WaitForJob(result, client, storeName);
            Assert.That(result.JobCompletedOk);

            AssertTriplePatternNotInGraph(client, storeName,
                                       @"<http://example.org/alice> <http://xmlns.com/foaf/0.1/name> ""Alice""",
                                       "http://example.org/graphs/alice");
            AssertTriplePatternNotInDefaultGraph(client, storeName,
                                       @"<http://example.org/bob> <http://xmlns.com/foaf/0.1/name> ""Bob""");
            
        }

        [Test]
        public void TestGenerateAndRetrieveStats()
        {
            var client = GetClient();
            var storeName = "GenerateAndRetrieveStats_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);

            var txn1Adds = new StringBuilder();
            txn1Adds.AppendLine(@"<http://example.org/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"" <http://example.org/graphs/alice> .");
            txn1Adds.AppendLine(
                @"<http://example.org/alice> <http://xmlns.com/foaf/0.1/mbox> ""alice@example.org"" <http://example.org/graphs/alice> .");
            txn1Adds.AppendLine(@"<http://example.org/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .");
            txn1Adds.AppendLine(@"<http://example.org/bob> <http://xmlns.com/foaf/0.1/mbox> ""bob@example.org"" .");

            var result = client.ExecuteTransaction(storeName, null, null, txn1Adds.ToString());
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            var stats = client.GetStatistics(storeName);
            Assert.IsNull(stats);

            var job = client.UpdateStatistics(storeName);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk);

            stats = client.GetStatistics(storeName);
            Assert.IsNotNull(stats);
            Assert.AreEqual(4, stats.TotalTripleCount);
            Assert.AreEqual(2, stats.PredicateTripleCounts.Count);

            var commitPoint = client.GetCommitPoint(storeName, stats.CommitTimestamp.AddSeconds(1.0)); // added a second to allow for timestamp rounding
            Assert.That(commitPoint, Is.Not.Null);
            Assert.AreEqual(commitPoint.Id, stats.CommitId);
        }

        [Test]
        public void TestCreateSnapshot()
        {
            var storeName = "CreateSnapshot_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            const string addSet1 = "<http://example.org/people/alice> <http://www.w3.org/2000/01/rdf-schema#label> \"Alice\".";
            const string addSet2 = "<http://example.org/people/bob> <http://www.w3.org/2000/01/rdf-schema#label> \"Bob\".";
            const string addSet3 = "<http://example.org/people/carol> <http://www.w3.org/2000/01/rdf-schema#label> \"Carol\".";
            var result = client.ExecuteTransaction(storeName, null, null, addSet1);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);
            
            result = client.ExecuteTransaction(storeName, null, null, addSet2);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);
            
            result = client.ExecuteTransaction(storeName, null, null, addSet3);
            result = WaitForJob(result, client, storeName);
            Assert.IsTrue(result.JobCompletedOk);

            var resultsStream = client.ExecuteQuery(storeName, "SELECT * WHERE {?s ?p ?o}");
            resultsStream.Close();

            var commitPoints = client.GetCommitPoints(storeName, 0, 2).ToList();
            Assert.AreEqual(2, commitPoints.Count);

            // Append Only targets
            // Create from default (latest) commit
            var job = client.CreateSnapshot(storeName, storeName + "_snapshot1", PersistenceType.AppendOnly);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk, "Job failed: " + job.StatusMessage + " - " + job.ExceptionInfo);
            resultsStream = client.ExecuteQuery(storeName + "_snapshot1", "SELECT * WHERE { ?s ?p ?o }");
            var resultsDoc = XDocument.Load(resultsStream);
            Assert.AreEqual(3, resultsDoc.SparqlResultRows().Count());
            // Create from specific commit point
            job = client.CreateSnapshot(storeName, storeName + "_snapshot2", PersistenceType.AppendOnly, commitPoints[1]);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk);
            resultsStream = client.ExecuteQuery(storeName + "_snapshot2", "SELECT * WHERE {?s ?p ?o}");
            resultsDoc = XDocument.Load(resultsStream);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());

            // Rewrite targets
            // Create from default (latest) commit
            job = client.CreateSnapshot(storeName, storeName + "_snapshot3", PersistenceType.Rewrite);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk);
            resultsStream = client.ExecuteQuery(storeName + "_snapshot3", "SELECT * WHERE { ?s ?p ?o }");
            resultsDoc = XDocument.Load(resultsStream);
            Assert.AreEqual(3, resultsDoc.SparqlResultRows().Count());
            // Create from specific commit point
            job = client.CreateSnapshot(storeName, storeName + "_snapshot4", PersistenceType.Rewrite, commitPoints[1]);
            job = WaitForJob(job, client, storeName);
            Assert.IsTrue(job.JobCompletedOk);
            resultsStream = client.ExecuteQuery(storeName + "_snapshot4", "SELECT * WHERE {?s ?p ?o}");
            resultsDoc = XDocument.Load(resultsStream);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());

        }

        [Test]
        public void TestListCommitPoints()
        {
            var storeName = "ListCommitPoints_" + DateTime.Now.Ticks;
            var client = GetClient();

            client.CreateStore(storeName, PersistenceType.AppendOnly);

            var initialCommitPoints = client.GetCommitPoints(storeName, 0, 10).ToList();
            Assert.That(initialCommitPoints.Count, Is.EqualTo(1));
            Assert.That(initialCommitPoints[0].StoreName, Is.EqualTo(storeName));
            Assert.That(initialCommitPoints[0], Has.Property("Id"));
            Assert.That(initialCommitPoints[0], Has.Property("CommitTime"));
            Assert.That(initialCommitPoints[0], Has.Property("JobId"));

            var job = client.ExecuteTransaction(storeName, null, null,
                                      "<http://example.com/s> <http://example.com/p> <http://example.com/o>");
            job = WaitForJob(job, client, storeName);
            Assert.That(job.JobCompletedOk);

            var updatedCommitPoints = client.GetCommitPoints(storeName, 0, 10).ToList();
            Assert.That(updatedCommitPoints.Count, Is.EqualTo(2));
            Assert.That(updatedCommitPoints[1].Id, Is.EqualTo(initialCommitPoints[0].Id));

            updatedCommitPoints = client.GetCommitPoints(storeName, 0, 1).ToList();
            Assert.That(updatedCommitPoints, Has.Count.EqualTo(1));

            updatedCommitPoints = client.GetCommitPoints(
                storeName,
                // Need to allow for resolution of the date/time format
                DateTime.UtcNow.Add(TimeSpan.FromSeconds(1.0)),
                DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)), 0, 10)
                                        .ToList();
            Assert.That(updatedCommitPoints, Has.Count.EqualTo(2));
        }

        private static void AssertTriplePatternInGraph(IBrightstarService client, string storeName, string triplePattern,
                                              string graphUri)
        {
            var sparql = "ASK { GRAPH <" + graphUri + "> {" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsTrue(resultsDoc.SparqlBooleanResult());
        }

        private static void AssertTriplePatternInDefaultGraph(IBrightstarService client, string storeName,
                                                              string triplePattern)
        {
            var sparql = "ASK {{" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsTrue(resultsDoc.SparqlBooleanResult());
        }

        private static void AssertTriplePatternNotInGraph(IBrightstarService client, string storeName, string triplePattern,
                                      string graphUri)
        {
            var sparql = "ASK { GRAPH <" + graphUri + "> {" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsFalse(resultsDoc.SparqlBooleanResult());
        }

        private static void AssertTriplePatternNotInDefaultGraph(IBrightstarService client, string storeName,
                                                              string triplePattern)
        {
            var sparql = "ASK {{" + triplePattern + "}}";
            var resultsDoc = XDocument.Load(client.ExecuteQuery(storeName, sparql));
            Assert.IsFalse(resultsDoc.SparqlBooleanResult());
        }

//        private IJobInfo PollToCompletion(IBrightstarService client, string storeName, IJobInfo job)
//        {
//            while (true)
//            {
//                if (job.JobCompletedOk || job.JobCompletedWithErrors) return job;
//#if PORTABLE
//                var l = new ManualResetEvent(false);
//                l.WaitOne(250);
//#else
//            Thread.CurrentThread.Sleep(250);
//#endif
//                job = client.GetJobInfo(storeName, job.JobId);
//            }
//        }
    }
}

