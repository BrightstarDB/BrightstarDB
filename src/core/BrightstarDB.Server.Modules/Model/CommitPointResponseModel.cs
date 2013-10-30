using System;

namespace BrightstarDB.Server.Modules.Model
{
    public class CommitPointResponseModel
    {
        public ulong Id { get; set; }
        public string StoreName { get; set; }
        public DateTime CommitTime { get; set; }
        public Guid JobId { get; set; }
    }
}
