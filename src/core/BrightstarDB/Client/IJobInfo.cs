using System;
using BrightstarDB.Dto;

namespace BrightstarDB.Client
{
    ///<summary>
    /// Information about a Brightstar job.
    ///</summary>
    public interface IJobInfo
    {
        ///<summary>
        /// Returns true is the job is pending execution.
        ///</summary>
        bool JobPending { get; }

        /// <summary>
        /// Returns true if the job has started executing.
        /// </summary>
        bool JobStarted { get; }

        /// <summary>
        /// Returns true if the job has completed with errors.
        /// </summary>
        bool JobCompletedWithErrors { get; }

        /// <summary>
        /// Returns true if the job has successfully completed.
        /// </summary>
        bool JobCompletedOk { get; }

        /// <summary>
        /// The current job status message.
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// The Job id.
        /// </summary>
        string JobId { get; }

        /// <summary>
        /// Exception information
        /// </summary>
        ExceptionDetailObject ExceptionInfo { get; }

        /// <summary>
        /// Get the Date/Time when the job was queued to be processed
        /// </summary>
        DateTime QueuedTime { get; }

        /// <summary>
        /// Get the Date/Time when the job started to be processed
        /// </summary>
        DateTime StartTime { get; }

        /// <summary>
        /// Get the Date/Time when the job completed processing
        /// </summary>
        DateTime EndTime { get; }
    }
}
