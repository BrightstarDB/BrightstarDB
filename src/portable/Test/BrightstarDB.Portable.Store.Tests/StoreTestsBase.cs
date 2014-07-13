using System.Threading.Tasks;
using BrightstarDB.Client;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace BrightstarDB.Portable.Tests
{
    public class StoreTestsBase : TestsBase
    {
        protected void AssertJobSuccessful(IBrightstarService client,string storeName, IJobInfo job)
        {
            while (!(job.JobCompletedOk || job.JobCompletedWithErrors))
            {
                Task.Delay(3).Wait();
                job = client.GetJobInfo(storeName, job.JobId);
            }
            Assert.IsTrue(job.JobCompletedOk, "Job failed with message: {0} : {1}", job.StatusMessage, job.ExceptionInfo);
        }

        protected IBrightstarService GetEmbeddedClient()
        {
            return BrightstarService.GetClient("type=embedded;storesDirectory=" + TestConfiguration.StoreLocation);
        }
    }
}