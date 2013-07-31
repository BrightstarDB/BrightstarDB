using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Portable.Compatibility;
using BrightstarDB.Storage;
using NUnit.Framework;
using VDS.RDF;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class GraphTests
    {
#if PORTABLE
        private IPersistenceManager _persistenceManager;
#endif

        [TestFixtureSetUp]
        public void SetUp()
        {
#if PORTABLE
        _persistenceManager = new Storage.Persistence.Adaptation.PersistenceManager();
#endif
            CopyTestDataToImportFolder("graph_triples.nt");
        }

        private void CopyTestDataToImportFolder(string testDataFileName, string targetFileName = null)
        {
#if PORTABLE
            using (var srcStream = _persistenceManager.GetInputStream(Configuration.DataLocation + testDataFileName))
            {
                var targetDir = Configuration.StoreLocation + "\\import";
                var targetPath = targetDir + targetFileName ?? testDataFileName;
                if (!_persistenceManager.DirectoryExists(targetDir)) _persistenceManager.CreateDirectory(targetDir);
                if (_persistenceManager.FileExists(targetPath)) _persistenceManager.DeleteFile(targetPath);
                _persistenceManager.CreateFile(targetPath);
                using (var targetStream = _persistenceManager.GetOutputStream(targetPath, FileMode.CreateNew))
                {
                    srcStream.CopyTo(targetStream);
                }
            }
#else
            var importFile = new FileInfo(Configuration.DataLocation+testDataFileName);
            var targetDir = new DirectoryInfo(Configuration.StoreLocation + "\\import");
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            importFile.CopyTo(Configuration.StoreLocation + "import\\" + targetFileName ?? testDataFileName, true);
#endif
        }

        [Test]
        public void TestAddQuads()
        {
            var storeName = "TestAddQuads_" + DateTime.Now.Ticks;
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=C:\\brightstar");
            client.CreateStore(storeName);
            client.ExecuteTransaction(storeName, null, null,
                                      @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .
<http://np.com/s> <http://np.com/p> <http://np.com/o> .
");
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
            var client = BrightstarService.GetClient("type=embedded;storesdirectory=" + Configuration.StoreLocation);
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
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=C:\\brightstar");
            client.CreateStore(storeName);


            var job = client.StartImport(storeName, "graph_triples.nt", "http://np.com/g2");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

            var exportFileName = "C:\\brightstar\\import\\graph_triples_out.nt";
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
            using (var sr = new StreamReader(exportFileName))
#endif
            {
                var content = sr.ReadToEnd();
                Assert.AreEqual(2, content.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Count());
                sr.Close();
            }

            exportFileName = "C:\\brightstar\\import\\graph_triples_out_2.nt";
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
            using (var sr = new StreamReader(exportFileName))
#endif
            {
                var content = sr.ReadToEnd();
                Assert.AreEqual(1, content.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Count());
                sr.Close();
            }
        }

        private void Sleep(int ms)
        {
#if PORTABLE
            Task.Delay(ms).RunSynchronously();
#else
            Thread.Sleep(ms);
#endif
        }

        [Test]
        public void TestDeleteFromGraph()
        {
            var storeName = "TestDeleteFromGraph_" + DateTime.Now.Ticks;
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=C:\\brightstar");
            client.CreateStore(storeName);
            client.ExecuteTransaction(storeName, null, null,
                                      @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .
<http://np.com/s> <http://np.com/p> <http://np.com/o> .
");
            var result = client.ExecuteQuery(storeName,
                                "SELECT ?o FROM <http://np.com/g1> WHERE { <http://np.com/s> <http://np.com/p> ?o }");
            Assert.IsNotNull(result);
            XDocument resultDoc = XDocument.Load(result);
            Assert.AreEqual(1, resultDoc.SparqlResultRows().Count());
            var resultRow = resultDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://np.com/o2"), resultRow.GetColumnValue("o"));

            client.ExecuteTransaction(storeName, @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .",
                                      @"<http://np.com/s> <http://np.com/p> <http://np.com/o2> <http://np.com/g1> .",
                                      null);
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
