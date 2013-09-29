using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Server.Modules.Model;
using NUnit.Framework;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class JobResponseObjectSpec
    {
        [Test]
        public void TestBooleanFlagsFromJobStatus()
        {
            var status = new JobResponseObject {JobStatus = "Pending"};
            Assert.That(status.JobPending);
            Assert.That(!(status.JobCompletedOk || status.JobCompletedWithErrors || status.JobStarted || status.InvalidJob));

            status.JobStatus = "Started";
            Assert.That(status.JobStarted);
            Assert.That(!(status.JobCompletedOk || status.JobCompletedWithErrors || status.JobPending || status.InvalidJob));

            status.JobStatus = "CompletedOk";
            Assert.That(status.JobCompletedOk);
            Assert.That(!(status.JobStarted || status.JobCompletedWithErrors || status.JobPending || status.InvalidJob));

            status.JobStatus = "TransactionError";
            Assert.That(status.JobCompletedWithErrors);
            Assert.That(!(status.JobStarted || status.JobCompletedOk || status.JobPending || status.InvalidJob));

            status.JobStatus = "NotRegistered";
            Assert.That(status.InvalidJob);
            Assert.That(!(status.JobStarted || status.JobCompletedOk || status.JobPending || status.JobCompletedWithErrors));

            status.JobStatus = "Unknown";
            Assert.That(status.JobCompletedWithErrors);
            Assert.That(!(status.JobStarted || status.JobCompletedOk || status.JobPending || status.InvalidJob));
        }
    }
}
