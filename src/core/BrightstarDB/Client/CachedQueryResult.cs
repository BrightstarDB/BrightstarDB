using System;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Object used to cache SPARQL query results.
    /// </summary>
#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    public class CachedQueryResult
    {
        /// <summary>
        /// Timestamp for the cached results object
        /// </summary>
        public DateTime Timestamp { get; set; }
        /// <summary>
        /// The cached results serialized as a string
        /// </summary>
        public String Result { get; set; }

        /// <summary>
        /// DO NOT USER: Provided for serialization purposes
        /// </summary>
        [Obsolete("Provided only for serialization purposes")]
        public CachedQueryResult(){}

        /// <summary>
        /// Creates a new cached result
        /// </summary>
        /// <param name="timestamp">The timestamp for the result</param>
        /// <param name="result">The result serialized as a string</param>
        public CachedQueryResult(DateTime timestamp, String result)
        {
            Timestamp = timestamp;
            Result = result;
        }
    }
}
