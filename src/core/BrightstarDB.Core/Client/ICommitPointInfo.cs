using System;

namespace BrightstarDB.Client
{
    ///<summary>
    /// ICommitPointInfo contains information about a specific commit point.
    ///</summary>
    public interface ICommitPointInfo
    {
        /// <summary>
        /// Get the name of the store that this commit point comes from
        /// </summary>
        string StoreName { get; }

        /// <summary>
        /// Get the store-unique identifier for the commit point
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// Get the date/time at which the commit occurred.
        /// </summary>
        DateTime CommitTime { get; }

        /// <summary>
        /// Get the unique identifier of the job that caused this commit
        /// </summary>
        Guid JobId { get; }
    }
}
