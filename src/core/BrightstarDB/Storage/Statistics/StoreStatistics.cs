using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;

namespace BrightstarDB.Storage.Statistics
{
    internal class StoreStatistics
    {
        public ulong CommitNumber { get; set; }
        public DateTime CommitTime { get; set; }
        public ulong TripleCount { get; set; }
        public Dictionary<string, ulong> PredicateTripleCounts { get; private set; }
        public Dictionary<string, PredicateStatistics> PredicateStatistics { get; private set; }

        public StoreStatistics(ulong commitNumber, DateTime commitTimestamp, ulong totalTripleCount,
            Dictionary<string, ulong> predicateTripleCounts)
        {
            CommitNumber = commitNumber;
            CommitTime = commitTimestamp;
            TripleCount = totalTripleCount;
            PredicateTripleCounts = predicateTripleCounts;
            PredicateStatistics = null;
        }

        public StoreStatistics(ulong commitNumber, DateTime commitTimestamp, ulong totalTripleCount,
            Dictionary<string, PredicateStatistics> predicateStatistics)
        {
            CommitNumber = commitNumber;
            CommitTime = commitTimestamp;
            TripleCount = totalTripleCount;
            PredicateTripleCounts = predicateStatistics.ToDictionary(x => x.Key, x => x.Value.TripleCount);
            PredicateStatistics = predicateStatistics;
        }
    }

    internal class PredicateStatistics
    {
        public ulong TripleCount { get; private set; }
        public ulong DistinctSubjectCount { get; private set; }
        public ulong DistinctObjectCount { get; private set; }

        public PredicateStatistics(ulong tripleCount, ulong distinctSubjectCount, ulong distinctObjectCount)
        {
            TripleCount = tripleCount;
            DistinctSubjectCount = distinctSubjectCount;
            DistinctObjectCount = distinctObjectCount;
        }

        public double SubjectPredicateTripleCount => (double) TripleCount/DistinctSubjectCount;
        public double ObjectPredicateTripleCount => (double) TripleCount/DistinctObjectCount;
    }
}