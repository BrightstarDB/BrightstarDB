using System.Threading.Tasks;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB
{
    internal static class TestHelper
    {
        public static void AssertJobCompletesSuccessfully(IBrightstarService client, string storeName, IJobInfo job)
        {
            while (!job.JobCompletedOk && !job.JobCompletedWithErrors)
            {
                Task.Delay(10).Wait();
                job = client.GetJobInfo(storeName, job.JobId);
            }
            Assert.IsTrue(job.JobCompletedOk, "Expected job to complete successfully, but it failed with message '{0}' : {1}", job.StatusMessage, job.ExceptionInfo);
        }
    }
}
