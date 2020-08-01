using System;
using System.Collections.Generic;

namespace BrightstarDB.Storage.Statistics
{
    internal class StoreStatistics
    {
        public ulong CommitNumber { get; set; }
        public DateTime CommitTime { get; set; }
        public ulong TripleCount { get; set; }
        public Dictionary<string, ulong> PredicateTripleCounts { get; private set; } 

        public StoreStatistics(ulong commitNumber, DateTime commitTimestamp, ulong totalTripleCount,
                               Dictionary<string, ulong> predicateTripleCounts)
        {
            CommitNumber = commitNumber;
            CommitTime = commitTimestamp;
            TripleCount = totalTripleCount;
            PredicateTripleCounts = new Dictionary<string, ulong>(predicateTripleCounts);
        }
    }
}