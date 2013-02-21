using System;
using System.Threading;
using BrightstarDB;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RestClientTester
{
    public class ImportTest : RestClientTest
    {
        private BrightstarRestClient _client;
        private string _storeName;
        public ImportTest(ConnectionString connectionString) : base(connectionString)
        {
            _client = BrightstarService.GetClient(connectionString) as BrightstarRestClient;
            _storeName = connectionString.StoreName;
        }

        public void TestImportFromUri()
        {
            //MonitorJob(_client.StartImport(_storeName, "http://www.networkedplanet.com/datasets/homepages_en.nt"));

        }

        public void TestGZipedImport()
        {
            MonitorJob(_client.StartImport(_storeName, "http://www.networkedplanet.com/datasets/category_labels_en.nq.gz", true));
        }

        public void TestImportFromBlobStore()
        {
            // TODO : Need to upload some test data for this
        }

        private void MonitorJob(IJobInfo job)
        {
            while(!(job.JobCompletedOk || job.JobCompletedWithErrors))
            {
                Console.WriteLine(job.StatusMessage);
                Thread.Sleep(3000);
                job = _client.GetJobInfo(_storeName, job.JobId);
            }
            Assert.IsTrue(job.JobCompletedOk, "Expected job to complete successfully");
        }

    }
}
