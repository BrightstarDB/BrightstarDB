using System;
using BrightstarDB.Dto;

namespace BrightstarDB.Server
{
    internal class JobExecutionStatus
    {
        public Guid JobId { get; set; }
        public string Label { get; set; }
        public JobStatus JobStatus { get; set; }
        public string Information { get; set; }
        public ExceptionDetailObject ExceptionDetail { get; set; }
        /// <summary>
        /// Get or set the Date/Time when the job was first queued for processing
        /// </summary>
        public DateTime Queued { get; set; }
        /// <summary>
        /// Get or set the Date/Time when the job started processing
        /// </summary>
        public DateTime Started { get; set; }
        /// <summary>
        /// Get or set the Date/Time when the job completed processing
        /// </summary>
        public DateTime Ended { get; set; }
    }
}
