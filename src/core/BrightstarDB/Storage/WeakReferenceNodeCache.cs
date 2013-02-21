using System;
using System.Collections.Generic;
using BrightstarDB.Storage.BPlusTreeStore;

namespace BrightstarDB.Storage
{
    internal class WeakReferenceNodeCache : INodeCache
    {
        private readonly Dictionary<ulong, WeakReference> _cache;

        public WeakReferenceNodeCache()
        {
            _cache = new Dictionary<ulong, WeakReference>();
        }

        #region Implementation of INodeCache

        public void Add(INode node)
        {
            _cache[node.PageId] = new WeakReference(node);
        }

        public void Remove(INode node)
        {
            _cache.Remove(node.PageId);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public bool TryGetValue(ulong nodeId, out INode node)
        {
            WeakReference wr;
            if (_cache.TryGetValue(nodeId, out wr) && wr.IsAlive)
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
