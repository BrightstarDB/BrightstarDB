using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class GraphTests
    {
        [TestMethod]
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

        [TestMethod]
        public void TestImportIntoGraph()
        {
            var storeName = "TestImportIntoGraph_" + DateTime.Now.Ticks;
            var client = BrightstarService.GetClient("type=embedded;storesdirectory=C:\\brightstar");
            client.CreateStore(storeName);

            var importFile = new FileInfo("graph_triples.nt");
            var targetDir = new DirectoryInfo("c:\\brightstar\\import");
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            importFile.CopyTo("C:\\brightstar\\import\\graph_triples.nt", true);

            var job = client.StartImport(storeName, "graph_triples.nt", "http://np.com/g2");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Thread.Sleep(10);
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

        [TestMethod]
        public void TestExportGraphs()
        {
            var storeName = "TestExportGraphs_" + DateTime.Now.Ticks;
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=C:\\brightstar");
            client.CreateStore(storeName);

            FileInfo importFile = new FileInfo("graph_triples.nt");
            var targetDir = new DirectoryInfo("c:\\brightstar\\import");
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            importFile.CopyTo("C:\\brightstar\\import\\graph_triples.nt", true);

            var job = client.StartImport(storeName, "graph_triples.nt", "http://np.com/g2");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Thread.Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

            var exportFileName = "C:\\brightstar\\import\\graph_triples_out.nt";
            if (File.Exists(exportFileName)) File.Delete(exportFileName);

            job = client.StartExport(storeName, "graph_triples_out.nt");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Thread.Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }

            using (var sr = new StreamReader(exportFileName))
            {
                var content = sr.ReadToEnd();
                Assert.AreEqual(2, content.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Count());
                sr.Close();
            }

            exportFileName = "C:\\brightstar\\import\\graph_triples_out_2.nt";
            if (File.Exists(exportFileName)) File.Delete(exportFileName);
            job = client.StartExport(storeName, "graph_triples_out_2.nt", "http://np.com/g1");
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Thread.Sleep(10);
                job = client.GetJobInfo(storeName, job.JobId);
            }
            using (var sr = new StreamReader(exportFileName))
            {
                var content = sr.ReadToEnd();
                Assert.AreEqual(1, content.Split(new string[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries).Count());
                sr.Close();
            }
        }

        [TestMethod]
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
    }
}
