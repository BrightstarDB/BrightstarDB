#if !REST_CLIENT
using System;

namespace BrightstarDB.Client
{
    internal class CommitPointInfoWrapper : ICommitPointInfo
    {
        private readonly CommitPointInfo _commitPointInfo;

        internal CommitPointInfo CommitPointInfo { get { return _commitPointInfo; } }

        public CommitPointInfoWrapper(CommitPointInfo commitPointInfo)
        {
            _commitPointInfo = commitPointInfo;
        }

        #region Implementation of ICommitPointInfo

        /// <summary>
        /// Get the name of the store that this commit point comes from
        /// </summary>
        public string StoreName
        {
            //get { return _storeName; }
            get { return _commitPointInfo.StoreName; }
        }

        /// <summary>
        /// Get the store-unique identifier for the commit point
        /// </summary>
        public ulong Id
        {
            get { return _commitPointInfo.Id; }
        }

        /// <summary>
        /// Get the date/time at which the commit occurred.
        /// </summary>
        public DateTime CommitTime
        {
            get { return _commitPointInfo.CommitTime; }
        }

        /// <summary>
        /// Get the unique identifier of the job that caused this commit
        /// </summary>
        public Guid JobId
        {
            get { return _commitPointInfo.JobId; }
        }

        #endregion
    }
}
#endif