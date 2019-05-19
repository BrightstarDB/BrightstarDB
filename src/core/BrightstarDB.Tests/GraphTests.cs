using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Client;
using NUnit.Framework;
using System.Threading;
using BrightstarDB.Utils;
namespace BrightstarDB.Tests
{
    [TestFixture]
    public class GraphTests
    {
#if PORTABLE
        private IPersistenceManager _persistenceManager;
#endif

        [SetUp]
        public void SetUp()
        {
#if PORTABLE
        _persistenceManager = new PersistenceManager();
#endif
            CopyTestDataToImportFolder("graph_triples.nt");
        }

        private void CopyTestDataToImportFolder(string testDataFileName, string targetFileName = null)
        {
#if PORTABLE
            using (var srcStream = _persistenceManager.GetInputStream(Configuration.DataLocation + testDataFileName))
            {
                var targetDir = Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.StoreLocation, "import");
                var targetPath = Path.Combine(targetDir, (targetFileName ?? testDataFileName));
                if (!_persistenceManager.DirectoryExists(targetDir)) _persistenceManager.CreateDirectory(targetDir);
                if (_persistenceManager.FileExists(targetPath)) _persistenceManager.DeleteFile(targetPath);
                using (var targetStream = _persistenceManager.GetOutputStream(targetPath, FileMode.CreateNew))
                {
                    srcStream.CopyTo(targetStream);
                }
            }
#else
            var importFile = new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation, testDataFileName));
            var targetDir = new DirectoryInfo(Path.Combine(Configuration.StoreLocation,"import"));
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            importFile.CopyTo(Path.Combine(targetDir.FullName, targetFileName ?? testDataFileName), true);
#endif
        }

        private IBrightstarService GetEmbeddedClient()
        {
            return BrightstarService.GetClient("type=embedded;storesDirectory=" + Configuration.StoreLocation);
        }

        [Test]
        public void TestAddQuads()
        {
            var storeName = "TestAddQuads_" + DateTime.Now.Ticks;
            var client = GetEmbeddedClient();
            client.CreateStore(storeName);
            var job = client.ExecuteTransaction(storeName, new UpdateTransactionData
                {
                    InsertData = @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .
<http://np.com/s> <http://np.com/p> <http://np.com/o> .
"
                });
            TestHelper.AssertJobCompletesSuccessfully(client, storeName, job);

            var result = client.ExecuteQuery(storeName,
                                "SELECT ?o FROM <http://np.com/g1> WHERE { <http://np.com/s> <http://np.com/p> ?o }");
            Assert.IsNotNull(result);
            XDocument resultDoc = XDocument.Load(result);
            Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
            var resultRow = resultDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o2"), resultRow.GetColumnValue("o"));

            result = client.ExecuteQuery(storeName, "SELECT ?o WHERE { <http://np.com/s> <http://np.com/p> ?o }");
            resultDoc = XDocument.Load(result);
            Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
            resultRow = resultDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o"), resultRow.GetColumnValue("o"));

            result = client.ExecuteQuery(storeName,
                                         "SELECT ?o WHERE { GRAPH <http://np.com/g1> { <http://np.com/s> <http://np.com/p> ?o } }");
            resultDoc = XDocument.Load(result);
            Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
            resultRow = resultDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o2"), resultRow.GetColumnValue("o"));

            result = client.ExecuteQuery(storeName,
                             "SELECT ?o FROM <http://np.com/g1> FROM <" + Constants.DefaultGraphUri + "> WHERE { <http://np.com/s> <http://np.com/p> ?o }");
            resultDoc = XDocument.Load(result);
            Assert.AreEqual(2, resultDoc.SparqlResultRows().Count());
            //resultRow = resultDoc.SparqlResultRows().First();
            //Assert.AreEqual(new Uri("http://np.com/o2"), resultRow.GetColumnValue("o"));

        }

        [Test]
        public void TestImportIntoGraph()
        {
            var storeName = "TestImportIntoGraph_" + DateTime.Now.Ticks;
            var client = GetEmbeddedClient();
            client.CreateStore(storeName);

            var job = client.StartImport(storeName, "graph_triples.nt", "http://np.com/g2");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

            var query = "SELECT ?o WHERE { <http://np.com/s1> <http://np.com/p> ?o }";
            var results = client.ExecuteQuery(storeName, query);
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(0, resultsDoc.SparqlResultRows().Count());

            query = "SELECT ?o FROM <http://np.com/g1> WHERE { <http://np.com/s1> <http://np.com/p> ?o }";
            results = client.ExecuteQuery(storeName, query);
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            var resultRow = resultsDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o2"), resultRow.GetColumnValue("o"));

            query = "SELECT ?o FROM <http://np.com/g2> WHERE { <http://np.com/s1> <http://np.com/p> ?o }";
            results = client.ExecuteQuery(storeName, query);
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            resultRow = resultsDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o1"), resultRow.GetColumnValue("o"));

        }

        

        [Test]
        public void TestExportGraphs()
        {
            var storeName = "TestExportGraphs_" + DateTime.Now.Ticks;
            var client = GetEmbeddedClient();
            client.CreateStore(storeName);


            var job = client.StartImport(storeName, "graph_triples.nt", "http://np.com/g2");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

            var exportFileName = Path.Combine(Configuration.StoreLocation, "import", "graph_triples_out.nt");
            EnsureFileDoesNotExist(exportFileName);

            job = client.StartExport(storeName, "graph_triples_out.nt");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

#if PORTABLE
            using (var sr = new StreamReader(_persistenceManager.GetInputStream(exportFileName)))
#else
            using (var sr = new StreamReader(File.OpenRead(exportFileName)))
#endif
            {
                var content = sr.ReadToEnd();
                Assert.AreEqual(2, content.Split(new [] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Count());
                sr.Close();
            }

            exportFileName = Path.Combine(Configuration.StoreLocation, "import", "graph_triples_out_2.nt");
            EnsureFileDoesNotExist(exportFileName);
            job = client.StartExport(storeName, "graph_triples_out_2.nt", "http://np.com/g1");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

#if PORTABLE
            using(var sr = new StreamReader(_persistenceManager.GetInputStream(exportFileName)))
#else
            using (var sr = new StreamReader(File.OpenRead(exportFileName)))
#endif
            {
                var content = sr.ReadToEnd();
                Assert.AreEqual(1, content.Split(new [] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Count());
                sr.Close();
            }
        }

        private void Sleep(int ms)
        {
#if PORTABLE
            Task.Delay(ms).Wait();
#else
            Thread.Sleep(ms);
#endif
        }

        [Test]
        public void TestDeleteFromGraph()
        {
            var storeName = "TestDeleteFromGraph_" + DateTime.Now.Ticks;
            var client = GetEmbeddedClient();
            client.CreateStore(storeName);
            var job = client.ExecuteTransaction(storeName, new UpdateTransactionData
                {
                    InsertData = @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .
<http://np.com/s> <http://np.com/p> <http://np.com/o> .
"
                });
            TestHelper.AssertJobCompletesSuccessfully(client, storeName, job);

            var result = client.ExecuteQuery(storeName,
                                "SELECT ?o FROM <http://np.com/g1> WHERE { <http://np.com/s> <http://np.com/p> ?o }");
            Assert.IsNotNull(result);
            XDocument resultDoc = XDocument.Load(result);
            Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
            var resultRow = resultDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o2"), resultRow.GetColumnValue("o"));

            job = client.ExecuteTransaction(storeName, new UpdateTransactionData
                {
                    ExistencePreconditions =
                        @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .",
                    DeletePatterns = @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> ."
                });
            TestHelper.AssertJobCompletesSuccessfully(client, storeName, job);
            result = client.ExecuteQuery(storeName,
                                "SELECT ?o FROM <http://np.com/g1> WHERE { <http://np.com/s> <http://np.com/p> ?o }");
            Assert.IsNotNull(result);
            resultDoc = XDocument.Load(result);
            Assert.AreEqual(0, resultDoc.SparqlResultRows().Count());

        }

        private void EnsureFileDoesNotExist(string path)
        {
#if PORTABLE
            if (_persistenceManager.FileExists(path)) _persistenceManager.DeleteFile(path);
#else
            if (File.Exists(path)) File.Delete(path);
#endif
        }
    }
}
