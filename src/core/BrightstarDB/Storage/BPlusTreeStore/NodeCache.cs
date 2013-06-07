using System;
using System.Threading.Tasks;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class NodeCache : INodeCache
    {
        private readonly IndexedCircularBuffer<ulong, WeakReference> _leafNodeCache;
        private readonly IndexedCircularBuffer<ulong, InternalNode> _internalNodeCache;

        public NodeCache(int internalNodeCapacity, int leafNodeCapacity)
        {
            _leafNodeCache = new IndexedCircularBuffer<ulong, WeakReference>(leafNodeCapacity);
            _internalNodeCache = new IndexedCircularBuffer<ulong, InternalNode>(internalNodeCapacity);
        }

        public void Add(INode node)
        {
            if (node is ILeafNode)
            {
                _leafNodeCache.Insert(node.PageId, new WeakReference(node));
            }
            else if (node is InternalNode)
            {
                _internalNodeCache.Insert(node.PageId, node as InternalNode);
            }
        }

        public void Remove(INode node)
        {
            if(node.IsLeaf)
            {
                _leafNodeCache.Remove(node.PageId);
            }
            else
            {
                _internalNodeCache.Remove(node.PageId);
            }
        }

        public void Clear()
        {
            _leafNodeCache.Clear();
            _internalNodeCache.Clear();
        }

        public bool TryGetValue(ulong nodeId, out INode node)
        {
            bool hit = false;
            node = null;
            WeakReference wr;
            if (_leafNodeCache.TryGetValue(nodeId, out wr) && wr.IsAlive)
            {
                node = wr.Target as INode;
                hit = (node != null);
            }
            else
            {
                InternalNode internalNode;
                if (_internalNodeCache.TryGetValue(nodeId, out internalNode))
                {
                    node = internalNode;
                    hit = (node != null);
                } 
            }
            return hit;
        }

        
    }
}
