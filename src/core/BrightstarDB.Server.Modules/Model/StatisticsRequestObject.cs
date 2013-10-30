using System;

namespace BrightstarDB.Server.Modules.Model
{
    public class StatisticsRequestObject
    {
        public string StoreName { get; set; }
        public DateTime? Earliest { get; set; }
        public DateTime? Latest { get; set; }
        public int Skip { get; set; }
    }
}
