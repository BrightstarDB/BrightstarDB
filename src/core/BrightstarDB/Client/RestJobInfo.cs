using System;
using System.ServiceModel;
using System.Web.Script.Serialization;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Serialization class for the JSON representation of job information
    /// </summary>
    public class RestJobInfo : IJobInfo
    {
        /// <summary>
        /// Get / set the job ID
        /// </summary>
        /// <remarks>For serialization purposes only</remarks>
        public string Id { get; set; }

        /// <summary>
        /// Get / set the job status
        /// </summary>
        /// <remarks>For serialization purposes only</remarks>
        public string Status { get; set; }

        ///<summary>
        /// Returns true is the job is pending execution.
        ///</summary>
        [ScriptIgnore]
        public bool JobPending
        {
            get { return Status.Equals("pending", StringComparison.InvariantCultureIgnoreCase) ||
                Status.Equals("0"); }
        }

        /// <summary>
        /// Returns true if the job has started executing.
        /// </summary>
        [ScriptIgnore]
        public bool JobStarted
        {
            get { return Status.Equals("started", StringComparison.InvariantCultureIgnoreCase) ||
                Status.Equals("1") ||
                Status.Equals("committing") || Status.Equals("2"); }
        }

        /// <summary>
        /// Returns true if the job has completed with errors.
        /// </summary>
        [ScriptIgnore]
        public bool JobCompletedWithErrors
        {
            get { return Status.Equals("completedWithErrors", StringComparison.InvariantCultureIgnoreCase) ||
                Status.Equals("99"); }
        }

        /// <summary>
        /// Returns true if the job has successfully completed.
        /// </summary>
        [ScriptIgnore]
        public bool JobCompletedOk
        {
            get { return Status.Equals("completedOk", StringComparison.InvariantCultureIgnoreCase) ||
                Status.Equals("98"); }
        }

        /// <summary>
        /// The current job status message.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// The Job id.
        /// </summary>
        [ScriptIgnore]
        public string JobId
        {
            get { return Id; }
        }

        /// <summary>
        /// Exception information
        /// </summary>
        [ScriptIgnore]
        public ExceptionDetail ExceptionInfo
        {
            get { return null; }
        }
    }
}
