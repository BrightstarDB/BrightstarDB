using System;
using System.Collections.Generic;

namespace BrightstarDB.Azure.Common
{
    public interface IJobQueue
    {
        /// <summary>
        /// Inserts a job into the job queue
        /// </summary>
        /// <param name="storeId">The target store for the job</param>
        /// <param name="jobType">The job type</param>
        /// <param name="jobData">The input data for the job</param>
        /// <param name="scheduledRunTime">OPTIONAL: The earlies date/time when the job should run</param>
        /// <returns>The new job ID</returns>
        string QueueJob(string storeId, JobType jobType, string jobData, DateTime? scheduledRunTime);

        /// <summary>
        /// Acquires the next available job for processing by this worker
        /// </summary>
        /// <param name="storeId">OPTIONAL: filter the available list to return only jobs available for the specified store</param>
        /// <returns>A JobInfo instance or null if no available job was found</returns>
        JobInfo NextJob(string storeId);

        /// <summary>
        /// Returns an enumeration over all jobs assigned to this worker that
        /// have not yet been marked as completed.
        /// </summary>
        /// <returns>An enumeration of JobInfo instances</returns>
        // Not currently used anywhere
        //IEnumerable<JobInfo> GetActiveJobs();

        /// <summary>
        /// Update the user-friendly status message for a job
        /// </summary>
        /// <param name="jobId">The ID of the job to be updated</param>
        /// <param name="statusMessage">The new status message for the job</param>
        void UpdateStatus(string jobId, string statusMessage);

        /// <summary>
        /// Mark the job as committing and update the status message
        /// </summary>
        /// <param name="jobId">The ID of the job to be updated</param>
        /// <param name="statusMessage">The new status message for the job</param>
        void StartCommit(string jobId, string statusMessage);

        /// <summary>
        /// Mark a job as completed
        /// </summary>
        /// <param name="jobId">The ID of the job to be updated</param>
        /// <param name="finalStatus">The final status code for the job</param>
        /// <param name="finalStatusMessage">The final user-friendly message for the job</param>
        void CompleteJob(string jobId, JobStatus finalStatus, string finalStatusMessage);

        /// <summary>
        /// Record a processing exception for a job and return it to the job pool
        /// </summary>
        /// <param name="jobId">The Id of the job to be updated</param>
        /// <param name="failureMessage">The user-friendly failure message for the job.</param>
        /// <param name="ex">The exception stack trace to record for the failure</param>
        void FailWithException(string jobId, string failureMessage, Exception ex);

        /// <summary>
        /// Return an acquired job to the pool
        /// </summary>
        /// <param name="jobId">The Id of the job to be updated</param>
        void ReleaseJob(string jobId);

        /// <summary>
        /// Get details about a specific job
        /// </summary>
        /// <param name="storeId">The Id of the store that the job is targeted on</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance or NULL of no match was found</returns>
        JobInfo GetJob(string storeId, string jobId);

        /// <summary>
        /// Cleans up the store by deleting all completed jobs that were finished
        /// before the current date/time less maxJobAge
        /// </summary>
        /// <param name="maxJobAge"></param>
        void Cleanup(TimeSpan maxJobAge);

        /// <summary>
        /// Removes all jobs (completed and uncompleted) from the queue
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Returns the info for the last job that committed to the specified store
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        JobInfo GetLastCommit(string storeId);

        /// <summary>
        /// Returns an enumeration over all jobs in the store that are to work on the specified store
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        IEnumerable<JobInfo> GetJobs(string storeId);
    }
}
