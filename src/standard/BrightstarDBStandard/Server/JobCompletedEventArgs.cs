using System;

namespace BrightstarDB.Server
{
    internal class JobCompletedEventArgs : EventArgs
    {
        public string StoreId { get; private set; }
        public Job CompletedJob { get; private set; }
        public JobCompletedEventArgs(string storeId, Job completedJob)
        {
            StoreId = storeId;
            CompletedJob = completedJob;
        }
    }
}