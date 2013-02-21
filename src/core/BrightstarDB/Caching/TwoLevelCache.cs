using System;

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
        /// Adds an object to the cache
        /// </summary>
        /// <param name="key">The cache key for the object</param>
        /// <param name="o">The object to be stored. Must be serializable.</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        public void Insert(string key, object o, CachePriority cachePriority)
        {
            _primaryCache.Insert(key, o, cachePriority);
            _secondaryCache.Insert(key, o, cachePriority);
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
        /// Looks up an object in the cache
        /// </summary>
        /// <typeparam name="T">The type of the object to look up</typeparam>
        /// <param name="key">The cache key of the object</param>
        /// <returns>The object found or null if the object was not found or does not match the specified type.</returns>
        public T Lookup<T>(string key)
        {
            var ret = _primaryCache.Lookup<T>(key);
            if (typeof(T).IsValueType)
            {
                if (ret.Equals(Activator.CreateInstance(typeof(T))))
                {
                    ret = _secondaryCache.Lookup<T>(key);
                    _primaryCache.Insert(key, ret, CachePriority.Normal);
                }
                return ret;
            }
            if (ret == null)
            {
                ret = _secondaryCache.Lookup<T>(key);
                if(ret != null)
                {
                    _primaryCache.Insert(key, ret, CachePriority.Normal);
                }
            }
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
