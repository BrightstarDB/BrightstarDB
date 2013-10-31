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
    }
}
