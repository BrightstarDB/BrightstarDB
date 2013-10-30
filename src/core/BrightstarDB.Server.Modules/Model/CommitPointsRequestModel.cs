using System;

namespace BrightstarDB.Server.Modules.Model
{
    public class CommitPointsRequestModel
    {
        public string StoreName { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public DateTime? Timestamp { get; set; }
        public DateTime? Earliest { get; set; }
        public DateTime? Latest { get; set; }
    }
}
