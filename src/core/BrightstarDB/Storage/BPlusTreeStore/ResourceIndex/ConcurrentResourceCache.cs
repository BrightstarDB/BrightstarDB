using System;
using System.Collections.Concurrent;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class ConcurrentResourceCache : IResourceCache
    {
        private readonly ConcurrentDictionary<ulong, WeakReference> _cache = new ConcurrentDictionary<ulong, WeakReference>();

        #region Implementation of IResourceCache

        public void Add(ulong resourceId, IResource resource)
        {
            var wr = new WeakReference(resource);
            _cache.AddOrUpdate(resourceId, wr, (k, v) => v.IsAlive ? v : wr);
        }

        public bool TryGetValue(ulong resourceId, out IResource resource)
        {
            WeakReference wr;
            if (_cache.TryGetValue(resourceId, out wr))
            {
                if (wr.IsAlive)
                {
                    resource = wr.Target as IResource;
                    return true;
                }
                _cache.TryRemove(resourceId, out wr);
            }
            resource = null;
            return false;
        }

        public void Clear()
        {
            _cache.Clear();
        }

        #endregion
    }
}
