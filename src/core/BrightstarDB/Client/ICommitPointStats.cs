using System.Collections.Generic;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Represents the triple count statistics for a given commit point
    /// </summary>
    /// <remarks>This interface inherits all of the basic <see cref="ICommitPointInfo"/> fields.</remarks>
    public interface ICommitPointStats : ICommitPointInfo
    {
        /// <summary>
        /// The total number of triples in the store at this commit point.
        /// </summary>
        long TotalTripleCount { get; }

        /// <summary>
        /// A dictionary mapping the unique predicate URLs to the count of the number
        /// of triples using that predicate in the store at this commit point.
        /// </summary>
        Dictionary<string, long> PredicateTripleCount { get; } 
    }
}
