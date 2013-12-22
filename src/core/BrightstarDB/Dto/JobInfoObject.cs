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
        /// <param name="jobId"></param>
        public JobInfoObject(Guid jobId)
        {
            JobId = jobId.ToString();
            JobStatus = JobStatus.Pending;
            QueuedTime = DateTime.UtcNow;
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
        public ExceptionDetailObject ExceptionInfo { get; set; }
        public JobStatus JobStatus { get; set; }

        public DateTime QueuedTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
