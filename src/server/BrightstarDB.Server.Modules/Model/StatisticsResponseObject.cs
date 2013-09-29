using System;
using System.Collections.Generic;

namespace BrightstarDB.Server.Modules.Model
{
    public class StatisticsResponseObject
    {
        public ulong CommitId { get; set; }
        public DateTime CommitTimestamp { get; set; }
        public ulong TotalTripleCount { get; set; }
        public Dictionary<string, ulong> PredicateTripleCounts { get; set; } 
    }
}