using System;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class LruResourceCache : IResourceCache
    {
        private LruCache<ulong, IResource> _cache;

        public LruResourceCache()
            : this(Configuration.ResourceCacheLimit)
        {

        }

        public LruResourceCache(int limit, int highWatermark = 0, int lowWatermark = 0)
        {
            _cache = new LruCache<ulong, IResource>(limit, highWatermark, lowWatermark);
        }


        public void Add(ulong resourceId, IResource resource)
        {
            _cache.InsertOrUpdate(resourceId, resource);
        }

        public bool TryGetValue(ulong resourceId, out IResource resource)
        {
            return _cache.TryLookup(resourceId, out resource);
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
                    _cache = null;
                    _disposed = true;
                }
            }
        }
    }
}
