using System;

namespace BrightstarDB.Server
{
    internal class ConsolidateJob : Job
    {
        public ConsolidateJob(Guid jobId, string label, StoreWorker storeWorker) : base(jobId, label, storeWorker)
        {
        }

        public override void Run()
        {
            StoreWorker.Consolidate(JobId);
        }

    }
}
