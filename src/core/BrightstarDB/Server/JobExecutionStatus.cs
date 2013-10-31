using System;
using BrightstarDB.Dto;

namespace BrightstarDB.Server
{
    internal class JobExecutionStatus
    {
        public Guid JobId { get; set; }
        public JobStatus JobStatus { get; set; }
        public string Information { get; set; }
        public ExceptionDetailObject ExceptionDetail { get; set; }
        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
    }
}
