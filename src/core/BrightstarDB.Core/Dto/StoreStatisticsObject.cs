using System;
using System.Collections.Generic;
using BrightstarDB.Client;

namespace BrightstarDB.Dto
{
    internal class StoreStatisticsObject : IStoreStatistics
    {
        public ulong CommitId { get; set; }
        public DateTime CommitTimestamp { get; set; }
        public ulong TotalTripleCount { get; set; }
        public IDictionary<string, ulong> PredicateTripleCounts { get; set; }
    }
}
