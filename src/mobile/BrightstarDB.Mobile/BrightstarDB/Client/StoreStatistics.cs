using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    public class StoreStatistics : IStoreStatistics
    {
        /// <summary>
        /// Gets the commit point identififier that the statistics relate to
        /// </summary>
        public ulong CommitId { get; internal set; }

        /// <summary>
        /// Gets the date/time that the statistics relate to
        /// </summary>
        public DateTime CommitTimestamp { get; internal set; }

        /// <summary>
        /// Gets the total number of triples in the store
        /// </summary>
        public ulong TotalTripleCount { get; internal set; }

        /// <summary>
        /// Gets a dictionary mapping predicate URI to the number of triples in the store using that predicate.
        /// </summary>
        public IDictionary<string, ulong> PredicateTripleCounts { get; internal set; }
    }
}