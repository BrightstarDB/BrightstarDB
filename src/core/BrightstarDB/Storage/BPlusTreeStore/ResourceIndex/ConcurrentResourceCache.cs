using System;
#if !PORTABLE
using System.Collections.Concurrent;
#else
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal class ConcurrentResourceCache : IResourceCache
    {
        private ConcurrentDictionary<ulong, WeakReference> _cache = new ConcurrentDictionary<ulong, WeakReference>();

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

        ~ConcurrentResourceCache()
        {
            Dispose(false);
        }
    }
}
