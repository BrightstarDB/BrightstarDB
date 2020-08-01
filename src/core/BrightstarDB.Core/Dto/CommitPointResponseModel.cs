using System;
using BrightstarDB.Client;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// 
    /// </summary>
    public class CommitPointResponseModel : ICommitPointInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public ulong Id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string StoreName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime CommitTime { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Guid JobId { get; set; }
    }
}
