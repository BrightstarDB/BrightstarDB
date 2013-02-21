using System;

#if !SILVERLIGHT
using System.ServiceModel;
#endif

namespace BrightstarDB.Server
{
    internal class JobExecutionStatus
    {
        public Guid JobId { get; set; }
        public JobStatus JobStatus { get; set; }
        public string Information { get; set; }
        public ExceptionDetail ExceptionDetail { get; set; }
        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
    }
}
