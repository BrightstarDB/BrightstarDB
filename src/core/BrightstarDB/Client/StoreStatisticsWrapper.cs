using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    internal class StoreStatisticsWrapper : IStoreStatistics
    {
        private readonly StoreStatistics _storeStatistics;
        public StoreStatisticsWrapper(StoreStatistics storeStatistics)
        {
            if (storeStatistics == null) throw new ArgumentNullException("storeStatistics");
            _storeStatistics = storeStatistics;
        }

        public ulong CommitId { get { return _storeStatistics.CommitId; }}
        public DateTime CommitTimestamp { get { return _storeStatistics.CommitTimestamp; } }
        public ulong TotalTripleCount { get { return _storeStatistics.TotalTripleCount; } }
        public IDictionary<string, ulong> PredicateTripleCounts { get { return _storeStatistics.PredicateTripleCounts; } }
    }
}