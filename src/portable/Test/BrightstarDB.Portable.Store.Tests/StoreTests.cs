using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Portable.Compatibility;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace BrightstarDB.Portable.Store.Tests
{
    [TestClass]
    public class StoreTests
    {
        private readonly string _runId = DateTime.Now.Ticks.ToString();
        private readonly IPersistenceManager _pm = new PersistenceManager();

        [TestMethod]
        public void TestCreateStore()
        {
            var client = GetEmbeddedClient();
            var storeName = "TestCreateStore_" + _runId;
            var storePath = Path.Combine(TestConfiguration.StoreLocation, storeName);
            var dataPath = Path.Combine(storePath, "data.bs");
            client.CreateStore(storeName);


            Assert.IsTrue(client.DoesStoreExist(storeName));
            Assert.IsTrue(_pm.DirectoryExists(TestConfiguration.StoreLocation));
            Assert.IsTrue(_pm.DirectoryExists(storePath));
            Assert.IsTrue(_pm.FileExists(dataPath));

            client.DeleteStore(storeName);

            Task.Delay(50).Wait(); // Wait to allow store to shutdown

            Assert.IsTrue(_pm.DirectoryExists(TestConfiguration.StoreLocation));
            Assert.IsFalse(_pm.DirectoryExists(storePath));
            Assert.IsFalse(_pm.FileExists(dataPath));
            Assert.IsFalse(client.DoesStoreExist(storeName));
        }

        [TestMethod]
        public void TestRdfImportExport()
        {
            var client = GetEmbeddedClient();
            var storeName = "TestRdfImportExport_" + _runId;
            var importPath = Path.Combine(TestConfiguration.StoreLocation, "import");

            TestHelper.CopyFile("TestData\\simple.txt", importPath, "simple.txt");
            client.CreateStore(storeName);

            var job = client.StartImport(storeName, "simple.txt");
            while (!(job.JobCompletedOk || job.JobCompletedWithErrors))
            {
                Task.Delay(3).Wait();
                job = client.GetJobInfo(storeName, job.JobId);
            }

            Assert.IsTrue(job.JobCompletedOk, "Import job failed with message: {0} : {1}", job.StatusMessage, job.ExceptionInfo);

            job = client.StartExport(storeName, "simple.export.nt");
            while (!(job.JobCompletedOk || job.JobCompletedWithErrors))
            {
                Task.Delay(3).Wait();
                job = client.GetJobInfo(storeName, job.JobId);
            }

            Assert.IsTrue(job.JobCompletedOk, "Export job failed with message: {0} : {1}", job.StatusMessage, job.ExceptionInfo);

            var exportFilePath = Path.Combine(importPath, "simple.export.nt");
            Assert.IsTrue(_pm.FileExists(exportFilePath));

        }

        private IBrightstarService GetEmbeddedClient()
        {
            return BrightstarService.GetClient("type=embedded;storesDirectory=" + TestConfiguration.StoreLocation);
        }
    }
}
