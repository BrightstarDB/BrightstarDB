using System;
using BrightstarDB.Client;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// C# representation of the JSON object returned to represent a Job status
    /// </summary>
    public class JobResponseModel : IJobInfo
    {
        /// <summary>
        /// Get / set the ID of the job
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Get / set the user-friendly label of the job
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Exception information
        /// </summary>
        public ExceptionDetailObject ExceptionInfo { get; set; }

        /// <summary>
        /// Get or set the Date/Time when the job was queued to be processed
        /// </summary>
        public DateTime QueuedTime { get; set; }

        /// <summary>
        /// Get or set the Date/Time when the job started to be processed
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Get or set the Date/Time when the job completed processing
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Get or set the string identifier for the current job status
        /// </summary>
        public string JobStatus { get; set; }
        
        /// <summary>
        /// Get or set the informational status message for the job
        /// </summary>
        public string StatusMessage { get; set; }
        
        /// <summary>
        /// Get or set the name of the store where this job runs
        /// </summary>
        public string StoreName { get; set; }

        ///<summary>
        /// Returns true is the job is pending execution.
        ///</summary>
        public bool JobPending { get { return JobStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase); } }

        /// <summary>
        /// Returns true if the job has started executing.
        /// </summary>
        public bool JobStarted { get { return JobStatus.Equals("Started", StringComparison.OrdinalIgnoreCase); } }

        /// <summary>
        /// Returns true if the job has successfully completed.
        /// </summary>
        public bool JobCompletedOk { get { return JobStatus.Equals("CompletedOk", StringComparison.OrdinalIgnoreCase); } }

        /// <summary>
        /// Returns true if the job has completed with errors.
        /// </summary>
        public bool JobCompletedWithErrors { get { return JobStatus.Equals("TransactionError", StringComparison.OrdinalIgnoreCase) || JobStatus.Equals("Unknown", StringComparison.OrdinalIgnoreCase); } }
        /// <summary>
        /// 
        /// </summary>
        public bool InvalidJob { get { return JobStatus.Equals("NotRegistered", StringComparison.OrdinalIgnoreCase); } }
    }
}
