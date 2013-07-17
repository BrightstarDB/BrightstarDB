using System;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class LruResourceIdCache : IResourceIdCache
    {
        private LruCache<string, ulong> _cache;
 
        public int CacheEntryCount { get { return _cache.Count; } }

        public LruResourceIdCache() : this(Configuration.ResourceCacheLimit)
        {
            
        }

        public LruResourceIdCache(int limit, int highWatermark = 0, int lowWatermark = 0)
        {
            _cache = new LruCache<string, ulong>(limit, highWatermark, lowWatermark);
        }

        public void Add(string resourceHashString, ulong resourceId)
        {
            _cache.InsertOrUpdate(resourceHashString, resourceId);
        }

        public bool TryGetValue(string resourceHashString, out ulong resourceId)
        {
            return _cache.TryLookup(resourceHashString, out resourceId);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _cache.Clear();
                    _cache = null;
                    _disposed = true;
                }
            }
        }

        ~LruResourceIdCache()
        {
            Dispose(false);
        }
    }
}
