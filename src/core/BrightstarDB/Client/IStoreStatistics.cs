using System;
using System.Collections.Generic;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Represents the triple statistics for a store at a specific commit point.
    /// </summary>
    public interface IStoreStatistics
    {
        /// <summary>
        /// Gets the commit point identififier that the statistics relate to
        /// </summary>
        ulong CommitId { get; }

        /// <summary>
        /// Gets the date/time that the statistics relate to
        /// </summary>
        DateTime CommitTimestamp { get; }

        /// <summary>
        /// Gets the total number of triples in the store
        /// </summary>
        ulong TotalTripleCount { get; }

        /// <summary>
        /// Gets a dictionary mapping predicate URI to the number of triples in the store using that predicate.
        /// </summary>
        IDictionary<string, ulong> PredicateTripleCounts { get; } 
    }
}
