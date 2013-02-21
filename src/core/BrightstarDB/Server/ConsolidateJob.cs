using System;

namespace BrightstarDB.Server
{
    internal class ConsolidateJob : Job
    {
        public ConsolidateJob(Guid jobId, StoreWorker storeWorker) : base(jobId, storeWorker)
        {
        }

        public override void Run()
        {
            StoreWorker.Consolidate(JobId);
        }

    }
}
