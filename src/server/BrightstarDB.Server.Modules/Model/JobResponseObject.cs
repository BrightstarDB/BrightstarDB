using System;

namespace BrightstarDB.Server.Modules.Model
{
    /// <summary>
    /// C# representation of the JSON object returned to represent a Job status
    /// </summary>
    public class JobResponseObject
    {
        /// <summary>
        /// Get / set the ID of the job
        /// </summary>
        public string JobId { get; set; }
        
        /// <summary>
        /// Get or set the string identifier for the current job status
        /// </summary>
        public string JobStatus { get; set; }
        
        /// <summary>
        /// Get or set the informational status message for the job
        /// </summary>
        public string StatusMessage { get; set; }
        
        /// <summary>
        /// Get or set the date/time when the job started processing
        /// </summary>
        public DateTime Started { get; set; }

        /// <summary>
        /// Get or set the date/time when the job completed processing
        /// </summary>
        public DateTime Ended { get; set; }
        

        public bool JobPending { get { return JobStatus.Equals("Pending", StringComparison.InvariantCultureIgnoreCase); } }
        public bool JobStarted { get { return JobStatus.Equals("Started", StringComparison.InvariantCultureIgnoreCase); } }
        public bool JobCompletedOk { get { return JobStatus.Equals("CompletedOk", StringComparison.InvariantCultureIgnoreCase); } }
        public bool JobCompletedWithErrors { get { return JobStatus.Equals("TransactionError", StringComparison.InvariantCultureIgnoreCase) || JobStatus.Equals("Unknown", StringComparison.InvariantCultureIgnoreCase); } }
        public bool InvalidJob { get { return JobStatus.Equals("NotRegistered", StringComparison.InvariantCultureIgnoreCase); } }


        public JobResponseObject(){}

    }
}
