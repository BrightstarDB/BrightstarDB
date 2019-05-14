using System;
using System.Linq;
using System.Threading;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#elif WINDOWS_PHONE
using BrightstarDB.Mobile.Compatibility;
#else
using System.Collections.Concurrent;
#endif

namespace BrightstarDB.Caching
{
    /// <summary>
    /// A simple least-recently-used cache eviction policy implementation.
    /// </summary>
    public class LruCacheEvictionPolicy : ICacheEvictionPolicy
    {
        private readonly ConcurrentDictionary<string, TimestampedCacheEntry> _normalPriorityEntries;
        private readonly ConcurrentDictionary<string, TimestampedCacheEntry> _highPriorityEntries;
        private bool _running;
#if SILVERLIGHT || PORTABLE
        private readonly ManualResetEvent _evictionCompleted = new ManualResetEvent(false);
#else
        private readonly ManualResetEventSlim _evictionCompleted = new ManualResetEventSlim();
#endif
        /// <summary>
        /// Creates a new LRU cache eviction policy
        /// </summary>
        public LruCacheEvictionPolicy()
        {
            _highPriorityEntries= new ConcurrentDictionary<string, TimestampedCacheEntry>();
            _normalPriorityEntries = new ConcurrentDictionary<string, TimestampedCacheEntry>();
        }

        /// <summary>
        /// Runs this policy against the specified cache until a target number of bytes have been evicted from the cache
        /// </summary>
        /// <param name="cache">The cache to run against</param>
        /// <param name="target">The target number of bytes to be evicted from the cache</param>
        public void Run(AbstractCache cache, long target)
        {
            lock (this)
            {
                if (_running)
                {
#if SILVERLIGHT || PORTABLE
                    _evictionCompleted.WaitOne();
#else
                    _evictionCompleted.Wait();
#endif
                }
                _running = true;
                _evictionCompleted.Reset();
            }
            long evictedBytes = 0;
            TimestampedCacheEntry removed;
            foreach(var lpEntry in _normalPriorityEntries.Values.OrderBy(e=>e.Timestamp))
            {
                if (_normalPriorityEntries.TryRemove(lpEntry.Key, out removed))
                {
                    evictedBytes += cache.EvictEntry(lpEntry.Key);
                }
                if(evictedBytes >= target) break;
            }
            if (evictedBytes < target)
            {
                foreach(var npEntry in _highPriorityEntries.Values.OrderBy(e=>e.Timestamp))
                {
                    if (_highPriorityEntries.TryRemove(npEntry.Key, out removed))
                    {
                        evictedBytes += cache.EvictEntry(npEntry.Key);
                    }
                    if (evictedBytes >= target) break;
                }
            }
            _evictionCompleted.Set();
        }

        /// <summary>
        /// Initialize the eviction policy to run on the specified cache
        /// </summary>
        /// <param name="cache"></param>
        public void Initialize(AbstractCache cache)
        {
            foreach(var entry in cache.ListEntries())
            {
                NotifyInsert(entry.Key, entry.Size, entry.Priority);
            }
        }

        /// <summary>
        /// Tracks cache inserts
        /// </summary>
        /// <param name="insertedKey">The inserted key</param>
        /// <param name="size">The size (in bytes) of the inserted value</param>
        /// <param name="priority">The priority assigned to the inserted cache item</param>
        public void NotifyInsert(string insertedKey, long size, CachePriority priority)
        {
            ConcurrentDictionary<string, TimestampedCacheEntry> queue = null;
            switch (priority)
            {
                case CachePriority.Normal:
                    queue = _normalPriorityEntries;
                    break;
                case CachePriority.High:
                    queue = _highPriorityEntries;
                    break;
            }
            if (queue != null)
            {
                queue[insertedKey] = new TimestampedCacheEntry(insertedKey);
            }
        }

        /// <summary>
        /// Tracks cache removals
        /// </summary>
        /// <param name="removedKey">The removed key</param>
        public void NotifyRemove(string removedKey)
        {
            TimestampedCacheEntry removed;
            if (_normalPriorityEntries.ContainsKey(removedKey))
            {
                _normalPriorityEntries.TryRemove(removedKey, out removed);
            }
            if (_highPriorityEntries.ContainsKey(removedKey))
            {
                _highPriorityEntries.TryRemove(removedKey, out removed);
            }
        }

        /// <summary>
        /// Tracks cache lookups
        /// </summary>
        /// <param name="lookupKey">The key accessed</param>
        public void NotifyLookup(string lookupKey)
        {
            TimestampedCacheEntry entry;
            if (_normalPriorityEntries.TryGetValue(lookupKey, out entry)) entry.Timestamp = DateTime.UtcNow.Ticks;
            if (_highPriorityEntries.TryGetValue(lookupKey, out entry)) entry.Timestamp = DateTime.UtcNow.Ticks;
        }

        private class TimestampedCacheEntry
        {
            public string Key { get; private set; }
            public long Timestamp { get; set; }

            public TimestampedCacheEntry(string key)
            {
                Key = key;
                Timestamp = DateTime.UtcNow.Ticks;
            }
        }
    }
}
