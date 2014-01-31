using System;
using BrightstarDB.Client;
using BrightstarDB.Server;

namespace BrightstarDB.Dto
{
    internal class JobInfoObject : IJobInfo
    {
        /// <summary>
        /// Creates a JobInfoObject for a newly queued job
        /// </summary>
        /// <param name="jobId">The GUID identifier assigned to the new job.</param>
        /// <param name="label">The user-friendly label given to the job (may be NULL)</param>
        public JobInfoObject(Guid jobId, string label)
        {
            JobId = jobId.ToString();
            JobStatus = JobStatus.Pending;
            QueuedTime = DateTime.UtcNow;
            Label = label;
        }

        /// <summary>
        /// Creates a JobInfoObject that copies information from an internal JobExecutionStatus object
        /// </summary>
        /// <param name="executionStatus"></param>
        public JobInfoObject(JobExecutionStatus executionStatus)
        {
            JobId = executionStatus.JobId.ToString();
            JobStatus = executionStatus.JobStatus;
            StatusMessage = executionStatus.Information;
            ExceptionInfo = executionStatus.ExceptionDetail;
            QueuedTime = executionStatus.Queued;
            StartTime = executionStatus.Started;
            EndTime = executionStatus.Ended;
            Label = executionStatus.Label;
        }

        public bool JobPending { get { return JobStatus == JobStatus.Pending; } }
        public bool JobStarted { get {return JobStatus == JobStatus.Started; } }

        public bool JobCompletedWithErrors
        {
            get
            {
                return JobStatus == JobStatus.TransactionError || JobStatus == JobStatus.NotRegistered ||
                       JobStatus == JobStatus.Unknown;
            }
        }

        public bool JobCompletedOk { get { return JobStatus == JobStatus.CompletedOk; } }

        public string StatusMessage { get; set; }
        public string JobId { get; set; }
        public string Label { get; set; }
        public ExceptionDetailObject ExceptionInfo { get; set; }
        public JobStatus JobStatus { get; set; }

        public DateTime QueuedTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
