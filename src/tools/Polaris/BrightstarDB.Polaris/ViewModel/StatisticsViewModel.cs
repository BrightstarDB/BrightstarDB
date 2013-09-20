using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;

namespace BrightstarDB.Polaris.ViewModel
{
    public class StatisticsViewModel 
    {
        public ulong CommitId { get; set; }
        public DateTime CommitTimestamp { get; set; }
        public ulong TotalTripleCount { get; set; }
        public List<PredicateTripleCountViewModel> PredicateTripleCounts { get; set; } 

        public StatisticsViewModel(IStoreStatistics s)
        {
            CommitId = s.CommitId;
            CommitTimestamp = s.CommitTimestamp;
            TotalTripleCount = s.TotalTripleCount;
            PredicateTripleCounts =
                s.PredicateTripleCounts.Select(
                    e => new PredicateTripleCountViewModel {Predicate = e.Key, TripleCount = e.Value}).ToList();
        }
    }
}
