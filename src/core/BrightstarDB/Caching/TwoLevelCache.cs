namespace BrightstarDB.Caching
{
    /// <summary>
    /// A cache implementation that wraps two separate caches, one acts as the primary cache (typically 
    /// a memory cache) and the other provides a secondary cache (typically a large directory cache).
    /// </summary>
    public class TwoLevelCache : ICache
    {
        private readonly ICache _primaryCache;
        private readonly ICache _secondaryCache;

        /// <summary>
        /// Creates a new 2-level cache
        /// </summary>
        /// <param name="firstLevelCache">The primary cache</param>
        /// <param name="secondLevelCache">The second-level cache</param>
        public TwoLevelCache(ICache firstLevelCache, ICache secondLevelCache)
        {
            _primaryCache = firstLevelCache;
            _secondaryCache = secondLevelCache;
        }

        #region Implementation of ICache

        /// <summary>
        /// Adds a new item to the cache
        /// </summary>
        /// <param name="key">The cache key for the item</param>
        /// <param name="data">The data to be stored as a byte array</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        public void Insert(string key, byte[] data, CachePriority cachePriority)
        {
            _primaryCache.Insert(key, data, cachePriority);
            _secondaryCache.Insert(key, data, cachePriority);
        }

        /// <summary>
        /// Looks for an item in the cache and returns the bytes for that item
        /// </summary>
        /// <param name="key">The cache key of the item</param>
        /// <returns>The bytes for the cached item or null if the item is not found in the cache</returns>
        public byte[] Lookup(string key)
        {
            var ret = _primaryCache.Lookup(key) ?? _secondaryCache.Lookup(key);
            return ret;
        }

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">The cache key of the item to be removed</param>
        public void Remove(string key)
        {
            _primaryCache.Remove(key);
            _secondaryCache.Remove(key);
        }

        /// <summary>
        /// Returns true if the cache contains an entry with the specified key
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if the cache contains an entry with the specified key, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            return _primaryCache.ContainsKey(key) || _secondaryCache.ContainsKey(key);
        }

        #endregion
    }
}
