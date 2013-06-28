using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class LruResourceCache : IResourceCache
    {
        private readonly LruCache<ulong, IResource> _cache;
 
        public LruResourceCache() : this(Configuration.ResourceCacheLimit)
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
    }
}
