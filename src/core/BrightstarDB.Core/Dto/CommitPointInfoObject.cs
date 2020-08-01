using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Client;

namespace BrightstarDB.Dto
{
    internal class CommitPointInfoObject : ICommitPointInfo
    {
        public string StoreName { get; set; }
        public ulong Id { get; set; }
        public DateTime CommitTime { get; set; }
        public Guid JobId { get; set; }
    }
}
