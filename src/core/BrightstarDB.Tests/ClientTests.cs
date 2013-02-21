using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Caching;
using BrightstarDB.Client;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class ClientTests : ClientTestBase
    {

        private static IBrightstarService GetClient()
        {
            return BrightstarService.GetClient("type=http;endpoint=http://localhost:8090/brightstar");
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
        public void TestCreateStore()
        {
            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);
        }

        [TestMethod]
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

        [TestMethod]
        public void TestIfStoreExistsFalseWhenNoStoreCreated()
        {
            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            var exists = bc.DoesStoreExist(sid);
            Assert.IsFalse(exists);
        }

        [TestMethod]
        public void TestIfStoreExistsTrueAfterStoreCreated()
        {
            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);
            var exists = bc.DoesStoreExist(sid);
            Assert.IsTrue(exists);
        }

        [TestMethod]
        [ExpectedException(typeof(System.ServiceModel.FaultException<ExceptionDetail>))]
        public void TestCreateDuplicateStoreFails()
        {

            var bc = GetClient();
            var sid = Guid.NewGuid().ToString();
            bc.CreateStore(sid);
            bc.CreateStore(sid);
        }

        [TestMethod]
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

        [TestMethod]
        public void TestListStores()
        {
            var bc = GetClient();
            var stores = bc.ListStores();
            Assert.IsTrue(stores.Count() > 0);
        }

        [TestMethod]
        public void TestQuery()
        {
            var client = GetClient();
            var storeName  = "Client.TestQuery_" + DateTime.Now.Ticks;
            client.CreateStore(storeName);
            client.ExecuteQuery(storeName, "SELECT ?s WHERE { ?s ?p ?o }");
        }

        [TestMethod]
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
                Assert.AreEqual(typeof (BrightstarStoreNotModifiedException).FullName,
                                clientException.InnerException.Type);
                Assert.AreEqual("Store not modified", clientException.Message);
            }
        }


        public void TestLargeQueryResult()
        {
            
        }


        // Tel: 240439

        /*
        [TestMethod]
        public void TestGetStoreData()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triples = bc.GetStoreData(storeName);
            var memoryStream = new MemoryStream();
            triples.CopyTo(memoryStream);
            Assert.AreEqual(0, memoryStream.Length);
        }
        */

        [TestMethod]
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

            //var triples = bc.GetStoreData(storeName);
            //var memoryStream = new MemoryStream();
            //triples.CopyTo(memoryStream);
            //memoryStream.Flush();
            //Assert.IsTrue(0 < memoryStream.Length);
        }

        [TestMethod] public void TestTransactiondDeleteStatements()
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

            //var triples = bc.GetStoreData(storeName);
            //var memoryStream = new MemoryStream();
            //triples.CopyTo(memoryStream);
            //Assert.IsTrue(0 < memoryStream.Length);

            var deletePatterns = @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2>.";

            jobInfo = bc.ExecuteTransaction(storeName, "", deletePatterns, "");

            while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                jobInfo = bc.GetJobInfo(storeName, jobInfo.JobId);
            }

            //triples = bc.GetStoreData(storeName);
            //memoryStream = new MemoryStream();
            //triples.CopyTo(memoryStream);
            //Assert.AreEqual(0, memoryStream.Length);
        }

        [TestMethod]
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

        [TestMethod]
        public void TestSparqlXDocumentExtensions()
        {
            var bc = GetClient();
            var storeName = Guid.NewGuid().ToString();
            bc.CreateStore(storeName);
            var triplesToAdd =
                    @"<http://example.org/resource13> <http://example.org/property> <http://example.org/resource2> .
                      <http://example.org/resource14> <http://example.org/property1> ""30""^^<http://www.w3.org/2001/XMLSchema#integer> . ";

            var jobInfo = bc.ExecuteTransaction(storeName, "", "", triplesToAdd);

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
                Assert.IsInstanceOfType(o, typeof(Int32));
            }
        }

        [TestMethod]
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

        [TestMethod]
        public void TestBadDataGetsUsefulErrorMessage()
        {
            string sid = null;
            try
            {
                var bc = GetClient();
                sid = Guid.NewGuid().ToString();
                bc.CreateStore(sid);
                bc.CreateStore(sid);
            } catch (FaultException<ExceptionDetail> ex)
            {
                var detail = ex.Detail;
                Assert.AreEqual("Error creating store " + sid + ". Store already exists", detail.Message);
            }
        }

        [TestMethod]
        public void TestEmbeddedClient()
        {
            var storeName = Guid.NewGuid().ToString();
            var client =
                BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar;storeName=" + storeName);

            var tripleData =
                "<http://www.networkedplanet.com/people/gra> <<http://www.networkedplanet.com/type/worksfor> <http://www.networkedplanet.com/companies/networkedplanet> .";

            client.ExecuteTransaction(storeName,null, null, tripleData);
        }


        [TestMethod]
        public void TestEmbeddedClientDeleteCreatePattern()
        {
            var storeName = Guid.NewGuid().ToString();

            // create store
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar");
            client.CreateStore(storeName);

            if (client.DoesStoreExist(storeName))
            {
                // delete
                client.DeleteStore(storeName);

                //recreate
                client.CreateStore(storeName);
            }

            var tripleData =
                "<http://www.networkedplanet.com/people/gra> <<http://www.networkedplanet.com/type/worksfor> <http://www.networkedplanet.com/companies/networkedplanet> .";

            client.ExecuteTransaction(storeName, null, null, tripleData);
        }


        [TestMethod]
        public void TestExportWhileWriting()
        {
            var storeName = Guid.NewGuid().ToString();
            var client = GetClient();
            client.CreateStore(storeName);
            var batch1 = MakeTriples(0, 50000);
            var batch2 = MakeTriples(50000, 51000);
            var batch3 = MakeTriples(51000, 52000);
            var batch4 = MakeTriples(52000, 53000);

            // Verify batch size
            var p = new NTriplesParser();
            var counterSink = new CounterTripleSink();
            p.Parse(new StringReader(batch1), counterSink, Constants.DefaultGraphUri);
            Assert.AreEqual(50000, counterSink.Count);

            var jobInfo = client.ExecuteTransaction(storeName, String.Empty, String.Empty, batch1);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);

            // Second export with parallel store writes
            var exportJobInfo = client.StartExport(storeName, storeName + "_export.nt");
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch2);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);
            exportJobInfo = client.GetJobInfo(storeName, exportJobInfo.JobId);
            Assert.IsTrue(exportJobInfo.JobStarted, "Test inconclusive - export job completed before end of first concurrent import job."); // This is just to check that the export is still running while at least one commit occurs
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch3);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);
            jobInfo = client.ExecuteTransaction(storeName, null, null, batch4);
            Assert.AreEqual(true, jobInfo.JobCompletedOk);
            while (!exportJobInfo.JobCompletedOk)
            {
                Assert.IsFalse(exportJobInfo.JobCompletedWithErrors);
                Thread.Sleep(1000);
                exportJobInfo = client.GetJobInfo(storeName, exportJobInfo.JobId);
            }

            var exportFile = new FileInfo("c:\\brightstar\\import\\" + storeName + "_export.nt");
            Assert.IsTrue(exportFile.Exists);
            var lineCount = File.ReadAllLines(exportFile.FullName).Where(x => !String.IsNullOrEmpty(x)).Count();
            Assert.AreEqual(50000, lineCount);
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

        [TestMethod]
        public void TestSpecialCharsInIdentities()
        {
            var importDir = Path.Combine(Configuration.StoreLocation, "import");
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

            IDataObjectContext context = new EmbeddedDataObjectContext(new ConnectionString("type=embedded;storesDirectory=" + Configuration.StoreLocation + "\\"));
            var store = context.OpenStore(storeName);

            var test = store.BindDataObjectsWithSparql("SELECT ?p WHERE {?p a <http://xmlns.com/foaf/0.1/Person>} LIMIT 30").ToList();
            Assert.IsNotNull(test);

            foreach (var testDo in test)
            {
                Assert.IsNotNull(testDo);

                var propValues = testDo.GetPropertyValues("http://xmlns.com/foaf/0.1/name").Cast<string>();
                Assert.IsNotNull(propValues);
                Assert.IsTrue(propValues.Count() > 0);

            }
        }

        [TestMethod]
        public void TestConsolidateEmptyStore()
        {
            var storeName = "ConsolidateEmptyStore_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            var job = client.ConsolidateStore(storeName);
            var cycleCount = 0;
            while(!job.JobCompletedOk && !job.JobCompletedWithErrors && cycleCount < 100)
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

        [TestMethod]
        public void TestConsolidatePopulatedStore()
        {
            var storeName = "ConsolidatePopulatedStore_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            const string addSet1 = "<http://example.org/people/alice> <http://www.w3.org/2000/01/rdf-schema#label> \"Alice\".";
            const string addSet2 = "<http://example.org/people/bob> <http://www.w3.org/2000/01/rdf-schema#label> \"Bob\".";
            const string addSet3 = "<http://example.org/people/carol> <http://www.w3.org/2000/01/rdf-schema#label> \"Carol\".";
            var result = client.ExecuteTransaction(storeName, null, null, addSet1);
            Assert.IsTrue(result.JobCompletedOk);
            result = client.ExecuteTransaction(storeName, null, null, addSet2);
            Assert.IsTrue(result.JobCompletedOk);
            result = client.ExecuteTransaction(storeName, null, null, addSet3);
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

        [TestMethod]
        public void TestConsolidatePopulatedStoreAfterQuery()
        {
            var storeName = "ConsolidatePopulatedStore_" + DateTime.Now.Ticks;
            var client = GetClient();
            client.CreateStore(storeName);
            const string addSet1 = "<http://example.org/people/alice> <http://www.w3.org/2000/01/rdf-schema#label> \"Alice\".";
            const string addSet2 = "<http://example.org/people/bob> <http://www.w3.org/2000/01/rdf-schema#label> \"Bob\".";
            const string addSet3 = "<http://example.org/people/carol> <http://www.w3.org/2000/01/rdf-schema#label> \"Carol\".";
            var result = client.ExecuteTransaction(storeName, null, null, addSet1);
            Assert.IsTrue(result.JobCompletedOk);
            result = client.ExecuteTransaction(storeName, null, null, addSet2);
            Assert.IsTrue(result.JobCompletedOk);
            result = client.ExecuteTransaction(storeName, null, null, addSet3);
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
    }
}
