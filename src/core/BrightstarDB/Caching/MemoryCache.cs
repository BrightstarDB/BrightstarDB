using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.Caching
{
    /// <summary>
    /// An in-memory implementation of a Brightstar Cache
    /// </summary>
    public class MemoryCache : AbstractCache
    {
        private readonly ConcurrentDictionary<string, MemoryCacheEntry> _cache;

        private class MemoryCacheEntry : AbstractCacheEntry
        {
            private readonly byte[] _bytes;
            public MemoryCacheEntry(string key, byte[] data, CachePriority priority)
            {
                Key = key;
                Size = data.Length;
                Priority = priority;
                _bytes = data;
            }
            public override byte[] GetBytes()
            {
                return _bytes;
            }
        }

        /// <summary>
        /// Creates a new in-memory cache
        /// </summary>
        /// <param name="cacheSize">The maximum size of the cache in bytes</param>
        /// <param name="cacheEvictionPolicy">The eviction policy to use to maintain the cache size</param>
        /// <param name="highwaterMark">The cache size (in bytes) that will trigger the cache eviction policy to run</param>
        /// <param name="lowwaterMark">The cache size (in bytes) that the eviction policy will attempt to achieve when it is run</param>
        public MemoryCache(long cacheSize, ICacheEvictionPolicy cacheEvictionPolicy, long highwaterMark = 0, long lowwaterMark = 0) : base(cacheSize, cacheEvictionPolicy, highwaterMark, lowwaterMark)
        {
            _cache = new ConcurrentDictionary<string, MemoryCacheEntry>();
            CacheEvictionPolicy.Initialize(this);
        }

        #region Overrides of AbstractCache

        /// <summary>
        /// Provides an enumeration over the entries in the cache.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<AbstractCacheEntry> ListEntries()
        {
            return _cache.Values.Cast<AbstractCacheEntry>();
        }

        /// <summary>
        /// Implemented in derived classes to add a new entry to the cache
        /// </summary>
        /// <param name="key">The key for the new entry</param>
        /// <param name="data">The data for the new entry</param>
        /// <param name="cachePriority">The entry priority</param>
        /// <returns>The newly created cache entry</returns>
        protected override AbstractCacheEntry AddEntry(string key, byte[] data, CachePriority cachePriority)
        {
            var entry = new MemoryCacheEntry(key, data, cachePriority);
            _cache.AddOrUpdate(key, entry, (k, e) => entry);
            return entry;
        }

        /// <summary>
        /// Implemented in dervied classes to retrieve an entry from the cache
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>The cache entry found or null if there was no match on <paramref name="key"/></returns>
        protected override AbstractCacheEntry GetEntry(string key)
        {
            MemoryCacheEntry ret;
            if (_cache.TryGetValue(key, out ret))
            {
                return ret;
            }
            return null;
        }

        /// <summary>
        /// Removes the entry with the specified key from the cache
        /// </summary>
        /// <param name="key">The key of the entry to be removed</param>
        /// <returns>The number of bytes of data evicted from the cache as a result of this operation. May be 0 if the key was not found in the cache.</returns>
        protected override long RemoveEntry(string key)
        {
            MemoryCacheEntry removedEntry;
            if (_cache.TryRemove(key, out removedEntry))
            {
                return removedEntry.Size;
            }
            return 0;
        }

        #endregion
    }
}
