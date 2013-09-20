using System;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Object used to cache SPARQL query results.
    /// </summary>
#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    internal class CachedQueryResult
    {
        public DateTime Timestamp { get; set; }
        public String Result { get; set; }
        public CachedQueryResult(){}
        public CachedQueryResult(DateTime timestamp, String result)
        {
            Timestamp = timestamp;
            Result = result;
        }
    }
}
