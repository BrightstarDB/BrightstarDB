using System;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.Common.Logging
{
    public class JobLogEntity : TableServiceEntity
    {
        public JobLogEntity(string storeId, string jobId)
        {
            PartitionKey = storeId;
            RowKey = jobId;
        }

        public JobLogEntity(JobInfo jobInfo)
        {
            PartitionKey = jobInfo.StoreId;
            RowKey = DateTime.UtcNow.Ticks + "_" + jobInfo.ProcessorId + "_" + jobInfo.Id;
            Scheduled = jobInfo.ScheduledRunTime;
            Started = jobInfo.StartTime;
            Completed = jobInfo.ProcessingCompleted;
            JobType = (int)jobInfo.JobType;
            TimeTaken = jobInfo.StartTime.HasValue && jobInfo.ProcessingCompleted.HasValue
                            ? jobInfo.ProcessingCompleted.Value.Subtract(jobInfo.StartTime.Value).TotalSeconds
                            : 0.0;
            ApparentTimeTaken = jobInfo.ScheduledRunTime.HasValue && jobInfo.ProcessingCompleted.HasValue
                                    ? jobInfo.ProcessingCompleted.Value.Subtract(jobInfo.ScheduledRunTime.Value).
                                          TotalSeconds
                                    : 0.0;
            TimeQueued = jobInfo.StartTime.HasValue && jobInfo.ScheduledRunTime.HasValue
                             ? jobInfo.StartTime.Value.Subtract(jobInfo.ScheduledRunTime.Value).TotalSeconds
                             : 0.0;
            JobCompletedSuccessfully = jobInfo.Status == JobStatus.CompletedOk;
            StatusMessage = jobInfo.StatusMessage;
            RetryCount = jobInfo.RetryCount;
        }
        public JobLogEntity(){}

        /// <summary>
        /// Date/Time when the job was scheduled to start processing
        /// </summary>
        public DateTime? Scheduled { get; set; }

        /// <summary>
        /// Date/Time when the job actually started processing
        /// </summary>
        public DateTime? Started { get; set; }

        /// <summary>
        /// Date/Time when the job completed processing
        /// </summary>
        public DateTime? Completed { get; set; }

        /// <summary>
        /// The type of job (JobType enumeration cast as int)
        /// </summary>
        public int JobType { get; set; }

        /// <summary>
        /// The number of seconds from Started to Completed
        /// </summary>
        public double TimeTaken { get; set; }

        /// <summary>
        /// The number of seconds from Scheduled to Completed
        /// </summary>
        public double ApparentTimeTaken { get; set; }

        /// <summary>
        /// The number of seconds from Scheduled to Started
        /// </summary>
        public double TimeQueued { get; set; }

        /// <summary>
        /// Flag indicating if the job completed without errors
        /// </summary>
        public bool JobCompletedSuccessfully { get; set; }

        /// <summary>
        /// The last status message recorded on the job
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// The number of times the processing was retried.
        /// </summary>
        public int RetryCount { get; set; }
    }
}
