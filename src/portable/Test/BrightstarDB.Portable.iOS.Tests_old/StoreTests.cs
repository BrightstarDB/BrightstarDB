using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Storage;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using NUnit.Framework;
using Path = BrightstarDB.Portable.Compatibility.Path;

namespace BrightstarDB.Portable.iOS.Tests
{
    [TestFixture]
    public class StoreTests
    {
        private readonly string _runId = DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
        private readonly IPersistenceManager _pm = new PersistenceManager();

        [Test]
        public void TestCreateAndDeleteStore()
        {
            var client = GetEmbeddedClient();
            var storeName = "TestCreateStore_" + _runId;
            var storePath = Path.Combine(TestConfiguration.StoreLocation, storeName);
            var dataPath = Path.Combine(storePath, "data.bs");
            client.CreateStore(storeName);


            Assert.IsTrue(client.DoesStoreExist(storeName), "Expected True from DoesStoreExist after store created.");
            Assert.IsTrue(_pm.DirectoryExists(TestConfiguration.StoreLocation), "Expected stores directory at {0}", TestConfiguration.StoreLocation);
            Assert.IsTrue(_pm.DirectoryExists(storePath), "Expected store directory at {0}", storePath);
            Assert.IsTrue(_pm.FileExists(dataPath), "Expected B* data file at {0}", dataPath);

            client.DeleteStore(storeName);

            Task.Delay(50).Wait(); // Wait to allow store to shutdown

            Assert.IsTrue(_pm.DirectoryExists(TestConfiguration.StoreLocation), "Expected stores directory at {0} after store deleted.", TestConfiguration.StoreLocation);
            Assert.IsFalse(_pm.FileExists(dataPath), "Expected data file to be deleted from {0}.", dataPath);
            Assert.IsFalse(_pm.DirectoryExists(storePath), "Expected store directory to be deleted from {0}.", storePath);
            Assert.IsFalse(client.DoesStoreExist(storeName), "Expected False from DoesStoreExist after store deleted.");
        }

        [Test]
        public void TestRdfImportExport()
        {
            var client = GetEmbeddedClient();
            var storeName = "TestRdfImportExport_" + _runId;
            var importPath = Path.Combine(TestConfiguration.StoreLocation, "import");
            if (!Directory.Exists(importPath)) Directory.CreateDirectory(importPath);

            _pm.CopyFile(Path.Combine(Path.Combine(NSBundle.MainBundle.BundlePath, "TestData"), "simple.txt"), Path.Combine(importPath, "simple.txt"), true);

            client.CreateStore(storeName);

            // RDF import
            var job = client.StartImport(storeName, "simple.txt");
            AssertJobSuccessful(client, storeName, job);

            // RDF export
            job = client.StartExport(storeName, "simple.export.nt");
            AssertJobSuccessful(client, storeName, job);

            var exportFilePath = Path.Combine(importPath, "simple.export.nt");
            Assert.IsTrue(_pm.FileExists(exportFilePath));
        }

        [Test]
        public void TestExecuteTransaction()
        {
            var client = GetEmbeddedClient();
            var storeName = "TestExecuteTransaction_" + _runId;

            client.CreateStore(storeName);

            // Test a simple addition of triples
            var insertData = new StringBuilder();
            insertData.AppendLine(@"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"".");
            insertData.AppendLine(
                @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/mbox> ""alice@example.org"".");
            var job = client.ExecuteTransaction(storeName, null, null, insertData.ToString());
            AssertJobSuccessful(client, storeName, job);

            // Test an update with a precondition which is met
            const string tripleToDelete = @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/mbox> ""alice@example.org"".";
            const string tripleToInsert = @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/mbox_sha1sum> ""FAKESHA1""";
            job = client.ExecuteTransaction(storeName, tripleToDelete, tripleToDelete, tripleToInsert);
            AssertJobSuccessful(client, storeName, job);

            // Test an update with a precondition which is not met
            job = client.ExecuteTransaction(storeName, tripleToDelete, tripleToDelete, tripleToInsert);
            while (!(job.JobCompletedOk || job.JobCompletedWithErrors))
            {
                Task.Delay(3).Wait();
                job = client.GetJobInfo(storeName, job.JobId);
            }
            Assert.IsTrue(job.JobCompletedWithErrors);
        }

        [Test]
        public void TestQuery()
        {
            var client = GetEmbeddedClient();
            var storeName = "TestQuery_" + _runId;
            client.CreateStore(storeName);

            var insertData = new StringBuilder();
            insertData.AppendLine(@"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/name> ""Alice"".");
            insertData.AppendLine(
                @"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/knows> <http://example.org/people/bob> .");
            insertData.AppendLine(@"<http://example.org/people/alice> <http://xmlns.com/foaf/0.1/knows> <http://example.org/people/carol> .");
            insertData.AppendLine(@"<http://example.org/people/bob> <http://xmlns.com/foaf/0.1/name> ""Bob"" .");
            insertData.AppendLine(@"<http://example.org/people/carol> <http://xmlns.com/foaf/0.1/name> ""Carol"" .");

            var job = client.ExecuteTransaction(storeName, null, null, insertData.ToString());
            AssertJobSuccessful(client, storeName, job);

            const string query = "PREFIX foaf: <http://xmlns.com/foaf/0.1/> SELECT ?p ?n WHERE { <http://example.org/people/alice> foaf:knows ?p . ?p foaf:name ?n }";
            var resultStream = client.ExecuteQuery(storeName, query);
            var doc = XDocument.Load(resultStream);
            Assert.IsNotNull(doc.Root);
            var rows = doc.SparqlResultRows().ToList();
            Assert.AreEqual(2, rows.Count);

            foreach (var row in rows)
            {
                Assert.IsNotNull(row.GetColumnValue("p"));
                Assert.IsNotNull(row.GetColumnValue("n"));
            }
        }


        private void AssertJobSuccessful(IBrightstarService client, string storeName, IJobInfo job)
        {
            while (!(job.JobCompletedOk || job.JobCompletedWithErrors))
            {
                Task.Delay(3).Wait();
                job = client.GetJobInfo(storeName, job.JobId);
            }
            Assert.IsTrue(job.JobCompletedOk, "Job failed with message: {0} : {1}", job.StatusMessage, job.ExceptionInfo);
        }

        private IBrightstarService GetEmbeddedClient()
        {
            return BrightstarService.GetClient("type=embedded;storesDirectory=" + TestConfiguration.StoreLocation);
        }
    }
}