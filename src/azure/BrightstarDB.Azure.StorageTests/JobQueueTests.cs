using System;
using System.Threading;
using BrightstarDB.Azure.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Azure.StorageTests
{
    [TestClass]
    public class JobQueueTests
    {
        // This test uses a connection to the local BrightstarManagement database
        private const string ConnectionString =
            "Data Source=localhost;Initial Catalog=BrighstarManagement;Integrated Security=True;Pooling=False";

        [TestMethod]
        public void TestSimpleLifecycle()
        {
            var jobQueue = new SqlJobQueue(ConnectionString, "TestWorker");
            var worker2Queue = new SqlJobQueue(ConnectionString, "TestWorker2");
            jobQueue.ClearAll();
            var jobId = jobQueue.QueueJob("TestStore", JobType.Transaction, "This is some transaction data", null);
            Assert.IsNotNull(jobId);

            // Pull first job off the queue 
            var job = jobQueue.NextJob(null);
            Assert.IsNotNull(job);
            Assert.AreEqual("TestStore", job.StoreId);
            Assert.AreEqual(jobId, job.Id);
            Assert.AreEqual(JobType.Transaction, job.JobType);
            Assert.AreEqual(JobStatus.Started, job.Status);
            Assert.AreEqual("This is some transaction data", job.Data);

            // Shouldn't be a second job if looking from a different worker - not even if we ask by store id
            var nextJob = worker2Queue.NextJob(null);
            Assert.IsNull(nextJob);
            nextJob = worker2Queue.NextJob("TestStore");
            Assert.IsNull(nextJob);

            // But if we ask from the same queue, for the same store we should get the one we should be working on
            nextJob = jobQueue.NextJob("TestStore");
            Assert.IsNotNull(nextJob);
            Assert.AreEqual(jobId, job.Id);

            // Update status
            jobQueue.UpdateStatus(job.Id, "I'm working");

            // if someone else looks at the job they should see the updated status message.
            var readJob = jobQueue.GetJob("TestStore", jobId);
            Assert.IsNotNull(readJob);
            Assert.AreEqual("TestStore", readJob.StoreId);
            Assert.AreEqual(jobId, readJob.Id);
            Assert.AreEqual(JobType.Transaction, readJob.JobType);
            Assert.AreEqual(JobStatus.Started, readJob.Status);
            Assert.IsNull(readJob.Data); // input data is not exposed through this method
            Assert.AreEqual("I'm working", readJob.StatusMessage);

            // Complete job
            jobQueue.CompleteJob(job.Id, JobStatus.CompletedOk, "Transaction processed in 1000ms");

            // if someone else looks at the job they should see the updated status message.
            readJob = jobQueue.GetJob("TestStore", jobId);
            Assert.IsNotNull(readJob);
            Assert.AreEqual("TestStore", readJob.StoreId);
            Assert.AreEqual(jobId, readJob.Id);
            Assert.AreEqual(JobType.Transaction, readJob.JobType);
            Assert.AreEqual(JobStatus.CompletedOk, readJob.Status);
            Assert.IsNull(readJob.Data);
            Assert.AreEqual("Transaction processed in 1000ms", readJob.StatusMessage);

            // And now if we try to get the next job to work on, there should be no more jobs left
            nextJob = jobQueue.NextJob(null);
            Assert.IsNull(nextJob);
            nextJob = jobQueue.NextJob("TestStore");
            Assert.IsNull(nextJob);
        }

        [TestMethod]
        public void TestJobSequencing()
        {
            var gatewayQueue = new SqlJobQueue(ConnectionString, "Gateway");
            var worker1Queue = new SqlJobQueue(ConnectionString, "TestWorker1");
            var worker2Queue = new SqlJobQueue(ConnectionString, "TestWorker2");
            gatewayQueue.ClearAll();

            gatewayQueue.QueueJob("A", JobType.Transaction, "StoreA Job1", null);
            Thread.Sleep(200);
            gatewayQueue.QueueJob("A", JobType.Transaction, "StoreA Job2", null);
            Thread.Sleep(200);
            gatewayQueue.QueueJob("B", JobType.Transaction, "StoreB Job1", null);

            var worker1Job = worker1Queue.NextJob(null);
            var worker2Job = worker2Queue.NextJob(null);

            Assert.IsNotNull(worker1Job);
            Assert.AreEqual("A", worker1Job.StoreId);
            Assert.AreEqual("StoreA Job1", worker1Job.Data);

            Assert.IsNotNull(worker2Job);
            Assert.AreEqual("B", worker2Job.StoreId);
            Assert.AreEqual("StoreB Job1", worker2Job.Data);

            worker1Queue.CompleteJob(worker1Job.Id, JobStatus.CompletedOk, "All done");
            worker1Job = worker1Queue.NextJob(null);
            Assert.IsNotNull(worker1Job);
            Assert.AreEqual("A", worker1Job.StoreId);
            Assert.AreEqual("StoreA Job2", worker1Job.Data);

            worker2Queue.CompleteJob(worker2Job.Id, JobStatus.CompletedOk, "All done");
            worker2Job = worker2Queue.NextJob(null);
            Assert.IsNull(worker2Job);
        }

        [TestMethod]
        public void TestJobRetries()
        {
            var queue = new SqlJobQueue(ConnectionString, "TestWorker");
            var storeId = "TestStore_" + Guid.NewGuid();
            queue.QueueJob(storeId, JobType.Transaction, "Some job data", null);
            var job = queue.NextJob(storeId);
            Assert.IsNotNull(job);
            queue.FailWithException(job.Id, "Oopsy", new Exception("Something bad happened"));
            var nextJob = queue.NextJob(storeId);
            Assert.IsNotNull(nextJob);
            Assert.AreEqual(job.Id, nextJob.Id);
            Assert.AreEqual(1, nextJob.RetryCount);
        }
    }
}
