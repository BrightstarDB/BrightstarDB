using System;

namespace BrightstarDB.Client
{
    public class CommitPointInfo : ICommitPointInfo
    {
        #region Implementation of ICommitPointInfo

        /// <summary>
        /// Get the name of the store that this commit point comes from
        /// </summary>
        public string StoreName { get; internal set; }

        /// <summary>
        /// Get the store-unique identifier for the commit point
        /// </summary>
        public ulong Id { get; internal set; }

        /// <summary>
        /// Get the date/time at which the commit occurred.
        /// </summary>
        public DateTime CommitTime { get; internal set; }

        /// <summary>
        /// Get the unique identifier of the job that caused this commit
        /// </summary>
        public Guid JobId { get; internal set; }

        #endregion
    }
}
