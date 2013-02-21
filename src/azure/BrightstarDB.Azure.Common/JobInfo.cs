using System;

namespace BrightstarDB.Azure.Common
{
    public class JobInfo
    {
        public string Id { get; private set; }
        public string StoreId { get; private set; }
        public JobType JobType { get; internal set; }
        public JobStatus Status { get; internal set; }
        public string StatusMessage { get; internal set; }
        public string Data { get; internal set; }
        public int RetryCount { get; internal set; }
        public DateTime? ScheduledRunTime { get; internal set; }
        public DateTime? StartTime { get; internal set; }
        public DateTime? ProcessingCompleted { get; internal set; }
        public string ProcessorId { get; internal set; }
        public string ProcessingException { get; internal set; }
        internal JobInfo(string id, string storeId)
        {
            Id = id;
            StoreId = storeId;
        }
    }
}