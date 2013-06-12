using System;
using System.Collections.Generic;
using BrightstarDB.Storage.BPlusTreeStore;

namespace BrightstarDB.Storage
{
    internal class WeakReferenceNodeCache : INodeCache
    {
        private object _dictLock = new object();
        private readonly Dictionary<ulong, WeakReference> _cache;

        public WeakReferenceNodeCache()
        {
            _cache = new Dictionary<ulong, WeakReference>();
        }

        #region Implementation of INodeCache

        public void Add(INode node)
        {
            lock (_dictLock)
            {
                _cache[node.PageId] = new WeakReference(node);
            }
        }

        public void Remove(INode node)
        {
            lock (_dictLock)
            {
                _cache.Remove(node.PageId);
            }
        }

        public void Clear()
        {
            lock (_dictLock)
            {
                _cache.Clear();
            }
        }

        public bool TryGetValue(ulong nodeId, out INode node)
        {
            WeakReference wr;
            bool haveNode;
            lock (_dictLock)
            {
                haveNode = _cache.TryGetValue(nodeId, out wr);
            }
            if (haveNode && wr.IsAlive)
            {
                node = wr.Target as INode;
                return (node != null);
            }
            node = null;
            return false;
        }

        #endregion
    }
}
