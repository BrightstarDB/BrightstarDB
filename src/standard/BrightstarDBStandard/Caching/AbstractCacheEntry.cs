namespace BrightstarDB.Caching
{
    /// <summary>
    /// Abstract base class for entries in a <see cref="AbstractCache"/>
    /// </summary>
    public abstract  class AbstractCacheEntry
    {
        /// <summary>
        /// Get or set the entry key
        /// </summary>
        public string Key { get; protected set; }

        /// <summary>
        /// Get or set the entry priority
        /// </summary>
        public CachePriority Priority { get; protected set; }

        /// <summary>
        /// Get or set the size of the entry data (in bytes)
        /// </summary>
        public long Size { get; protected set; }

        /// <summary>
        /// Get or set the entry data
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetBytes();
    }
}