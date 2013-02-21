using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class BPlusTree
    {
        private INode _root;
        private readonly BPlusTreeConfiguration _config;
        private readonly IPageStore _pageStore;
        private readonly Dictionary<ulong, INode> _modifiedNodes;
        private readonly INodeCache _nodeCache;

        public BPlusTreeConfiguration Configuration { get { return _config; } }

        /// <summary>
        /// Creates a new tree in the page store
        /// </summary>
        /// <param name="pageStore"></param>
        /// <param name="keySize">The size of the B+ tree's key (in bytes)</param>
        /// <param name="dataSize">The size of the values stored in leaf nodes (in bytes)</param>
        public BPlusTree(IPageStore pageStore, int keySize = 8, int dataSize = 64) 
        {
            _config = new BPlusTreeConfiguration(keySize, dataSize, pageStore.PageSize);
            _pageStore = pageStore;
            _modifiedNodes = new Dictionary<ulong, INode>();
            _root = new LeafNode(pageStore.Create(), 0, 0, _config);
            _nodeCache = new WeakReferenceNodeCache();
            _modifiedNodes[_root.PageId] = _root;
            _nodeCache.Add(_root);
        }

        /// <summary>
        /// Opens an existing tree in the page store
        /// </summary>
        /// <param name="pageStore"></param>
        /// <param name="rootPageId">The page ID of the BTree root node</param>
        /// <param name="keySize"></param>
        /// <param name="dataSize"></param>
        /// <param name="profiler"></param>
        public BPlusTree(IPageStore pageStore, ulong rootPageId, int keySize = 8, int dataSize = 64, BrightstarProfiler profiler = null)
        {
            _config = new BPlusTreeConfiguration(keySize, dataSize, pageStore.PageSize);
            _pageStore = pageStore;
            _modifiedNodes = new Dictionary<ulong, INode>();
            _nodeCache = new WeakReferenceNodeCache();
            _root = GetNode(rootPageId, profiler);
            _nodeCache.Add(_root);
        }

        protected IPageStore PageStore { get { return _pageStore; } }

        /// <summary>
        /// Get a flag indicating if this tree contains unsaved modifications
        /// </summary>
        public bool IsModified { get { return _modifiedNodes.Count > 0; } }

        /// <summary>
        /// Get the ID of the root node of the tree
        /// </summary>
        public ulong RootId { get { return _root.PageId; } }

        public IEnumerable<InternalNode> ModifiedInternalNodes
        {
            get { return _modifiedNodes.Values.OfType<InternalNode>(); }
        }

        public INode GetNode(ulong nodeId, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.GetNode"))
            {
                INode ret;
                if (_modifiedNodes.TryGetValue(nodeId, out ret))
                {
                    profiler.Incr("NodeCache Hit");
                    return ret;
                }
                if (_nodeCache.TryGetValue(nodeId, out ret))
                {
                    profiler.Incr("NodeCache Hit");
                    return ret;
                }

                profiler.Incr("NodeCache Miss");
                using (profiler.Step("Load Node"))
                {
                    var nodePage = _pageStore.Retrieve(nodeId, profiler);
                    var header = BitConverter.ToInt32(nodePage, 0);
                    if (header < 0)
                    {
                        ret = new InternalNode(nodeId, nodePage, ~header, _config);
                    }
                    else
                    {
                        ret = new LeafNode(nodeId, nodePage, header, _config);
                    }
                    _nodeCache.Add(ret);
                    return ret;
                }
            }
        }

        public bool Search(byte[] key, byte[] valueBuff, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.Search"))
            {
                INode u = _root;
                while (u is InternalNode)
                {
                    var internalNode = u as InternalNode;
                    u = GetNode(internalNode.GetChildNodeId(key), profiler);
                }
                var l = u as LeafNode;
                return l.GetValue(key, valueBuff);
            }
        }

        public void Insert(ulong txnId, byte[] key, byte[] value, bool overwrite = false, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("BPlusTree.Insert"))
            {
                bool splitRoot;
                INode rightNode;
                byte[] rootSplitKey;
                Insert(txnId, _root, key, value, out splitRoot, out rightNode, out rootSplitKey, overwrite, profiler);
                if (splitRoot)
                {
                    var newRoot = new InternalNode(_pageStore.Create(), rootSplitKey, _root.PageId, rightNode.PageId,
                                                   _config);
                    MarkDirty(txnId, _root, profiler);
                    _modifiedNodes[newRoot.PageId] = newRoot;
                    _root = newRoot;
                }
            }
        }

        public void Delete(ulong txnId, byte[] key, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.Delete"))
            {
                if (_root is LeafNode)
                {
                    (_root as LeafNode).Delete(key);
                    MarkDirty(txnId, _root, profiler);
                }
                else
                {
                    bool underAllocation;
                    Delete(txnId, _root as InternalNode, key, out underAllocation, profiler);
                    if (_root.KeyCount ==0)
                    {
                        // Now has only a single child leaf node, which should become the new tree root
                        _root = GetNode((_root as InternalNode).ChildPointers[0], profiler);
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the key-value pairs stored in the BTree starting with <paramref name="fromKey"/>
        /// up to <paramref name="toKey"/> (inclusive)
        /// </summary>
        /// <param name="fromKey">The lowest key to return in the enumeration</param>
        /// <param name="toKey">The highest key to return in the enumeration</param>
        /// <param name="profiler"></param>
        /// <returns>An enumeration of key-value pairs from the BTree</returns>
        public IEnumerable<KeyValuePair<byte[], byte[]>> Scan(byte[] fromKey, byte[] toKey, BrightstarProfiler profiler )
        {
            using (profiler.Step("Scan BTree Range"))
            {
                if (fromKey.Compare(toKey) > 1)
                {
                    throw new ArgumentException("Scan can only be performed in increasing order.");
                }
                return Scan(_root, fromKey, toKey, profiler);
            }
        }

        public IEnumerable<KeyValuePair<byte[], byte []>> Scan(BrightstarProfiler profiler)
        {
            using (profiler.Step("Scan Entire BTree"))
            {
                return Scan(_root, profiler);
            }
        }

        private IEnumerable<KeyValuePair<byte[], byte[]>> Scan(INode node, BrightstarProfiler profiler)
        {
            if (node is InternalNode)
            {
                var internalNode = node as InternalNode;
                foreach(var childNodeId in internalNode.Scan())
                {
                    foreach(var entry in Scan(GetNode(childNodeId, profiler), profiler))
                    {
                        yield return entry;
                    }
                }
            }
            if (node is LeafNode)
            {
                var leaf = node as LeafNode;
                foreach(var entry in leaf.Scan())
                {
                    yield return entry;
                }
            }
        }

        private IEnumerable<KeyValuePair<byte[], byte[]>> Scan(INode node, byte[] fromKey, byte[] toKey, BrightstarProfiler profiler)
        {
            if (node is InternalNode)
            {
                var internalNode = node as InternalNode;
                foreach(var childNodeId in internalNode.Scan(fromKey, toKey))
                {
                    foreach(var entry in Scan(GetNode(childNodeId, profiler), fromKey, toKey, profiler))
                    {
                        yield return entry;
                    }
                }
            }
            else if (node is LeafNode)
            {
                var leaf = node as LeafNode;
                foreach(var entry in leaf.Scan(fromKey, toKey))
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Convenience method that wraps <see cref="Scan(byte[], byte[], BrightstarProfiler)"/> to convert keys to/from ulongs
        /// </summary>
        /// <param name="fromKey">The lowest key to return in the enumeration</param>
        /// <param name="toKey">The highest key to return in the enumeration</param>
        /// <param name="profiler"></param>
        /// <returns>An enumeration of key-value pars from the BTree</returns>
        public IEnumerable<KeyValuePair<ulong, byte[]>> Scan(ulong fromKey, ulong toKey, BrightstarProfiler profiler)
        {
            return
                Scan(BitConverter.GetBytes(fromKey), BitConverter.GetBytes(toKey), profiler).Select(
                    v => new KeyValuePair<ulong, byte[]>(BitConverter.ToUInt64(v.Key, 0), v.Value));
        }

        private void Delete(ulong txnId, InternalNode parentInternalNode, byte[] key, out bool underAllocation, BrightstarProfiler profiler)
        {
            if (parentInternalNode.RightmostKey == null)
            {
                throw new ArgumentException("Parent node right key is null");
            }
            var childNodeId = parentInternalNode.GetChildNodeId(key);
            var childNode = GetNode(childNodeId, profiler);
            if (childNode is LeafNode)
            {
                var childLeafNode = childNode as LeafNode;
                // Delete the key and mark the node as updated. This may update the child node id
                childLeafNode.Delete(key);
                MarkDirty(txnId, childLeafNode, profiler);
                if (childLeafNode.PageId != childNodeId)
                {
                    parentInternalNode.UpdateChildPointer(childNodeId, childLeafNode.PageId);
                    childNodeId = childLeafNode.PageId;
                }

                if (childLeafNode.NeedsJoin)
                {
                    ulong leftSiblingId, rightSiblingId;
                    LeafNode leftSibling = null, rightSibling = null;
                    bool hasLeftSibling = parentInternalNode.GetLeftSibling(childNodeId, out leftSiblingId);
                    if (hasLeftSibling)
                    {
                        leftSibling = GetNode(leftSiblingId, profiler) as LeafNode;
                        if (childLeafNode.RedistributeFromLeft(leftSibling))
                        {
                            parentInternalNode.SetLeftKey(childLeafNode.PageId, childLeafNode.LeftmostKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            MarkDirty(txnId, leftSibling, null);
                            parentInternalNode.UpdateChildPointer(leftSiblingId, leftSibling.PageId);
                            underAllocation = false;
                            return;
                        }
                    }
                    bool hasRightSibling = parentInternalNode.GetRightSiblingId(childNodeId, out rightSiblingId);
                    
                        
                    if (hasRightSibling)
                    {
                        rightSibling = GetNode(rightSiblingId, profiler) as LeafNode;
#if DEBUG
                        if (rightSibling.LeftmostKey.Compare(childLeafNode.RightmostKey) <= 0)
                        {
                            throw new Exception("Right-hand sibling has a left key lower than this nodes right key.");
                        }
#endif
                        if (childLeafNode.RedistributeFromRight(rightSibling))
                        {
                            MarkDirty(txnId, rightSibling, profiler);
                            parentInternalNode.UpdateChildPointer(rightSiblingId, rightSibling.PageId);
                            parentInternalNode.SetLeftKey(rightSibling.PageId, rightSibling.LeftmostKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = false;
                            return;
                        }
                    }
                    if (hasLeftSibling && childLeafNode.Merge(leftSibling))
                    {
                        parentInternalNode.RemoveChildPointer(leftSiblingId);
                        parentInternalNode.SetLeftKey(childLeafNode.PageId, childLeafNode.LeftmostKey);
                        MarkDirty(txnId, parentInternalNode, profiler);
                        underAllocation = parentInternalNode.NeedJoin;
                        return;
                    }
                    if (hasRightSibling && childLeafNode.Merge(rightSibling))
                    {
                        byte[] nodeKey = parentInternalNode.RemoveChildPointer(rightSiblingId);
                        if (nodeKey == null)
                        {
                            // We merged in the right-most node, so we need to generate a key
                            nodeKey = new byte[_config.KeySize];
                            Array.Copy(rightSibling.RightmostKey, nodeKey, _config.KeySize);
                            ByteArrayHelper.Increment(nodeKey);
                        }
                        parentInternalNode.SetKey(childLeafNode.PageId, nodeKey);
                        MarkDirty(txnId, parentInternalNode, profiler);
                        underAllocation = parentInternalNode.NeedJoin;
                        return;
                    }
                }
                underAllocation = false;
                return;
            }


            if (childNode is InternalNode)
            {
                bool childUnderAllocated;
                var childInternalNode = childNode as InternalNode;
                Delete(txnId, childInternalNode, key, out childUnderAllocated, profiler);
                if (childInternalNode.PageId != childNodeId)
                {
                    // Child node page changed
                    parentInternalNode.UpdateChildPointer(childNodeId, childInternalNode.PageId);
                    MarkDirty(txnId, parentInternalNode, profiler);
                    childNodeId = childInternalNode.PageId;
                }

                if (childUnderAllocated)
                {
                    InternalNode leftSibling = null, rightSibling = null;
                    ulong leftSiblingId, rightSiblingId;

                    // Redistribute values from left-hand sibling
                    bool hasLeftSibling = parentInternalNode.GetLeftSibling(childNodeId, out leftSiblingId);
                    if (hasLeftSibling)
                    {
                        leftSibling = GetNode(leftSiblingId, profiler) as InternalNode;
                        byte[] joinKey = parentInternalNode.GetKey(leftSiblingId);
                        var newJoinKey = new byte[_config.KeySize];
                        if (childInternalNode.RedistributeFromLeft(leftSibling, joinKey, newJoinKey))
                        {
                            MarkDirty(txnId, leftSibling, profiler);
                            parentInternalNode.UpdateChildPointer(leftSiblingId, leftSibling.PageId);
                            parentInternalNode.SetKey(leftSibling.PageId, newJoinKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = false;
                            return;
                        }
                    }

                    // Redistribute values from right-hand sibling
                    bool hasRightSibling = parentInternalNode.GetRightSiblingId(childNodeId, out rightSiblingId);
                    if (hasRightSibling)
                    {
                        rightSibling = GetNode(rightSiblingId, profiler) as InternalNode;
                        byte[] joinKey = parentInternalNode.GetKey(childInternalNode.PageId);
                        byte[] newJoinKey = new byte[_config.KeySize];
                        if (childInternalNode.RedistributeFromRight(rightSibling, joinKey, newJoinKey))
                        {
                            MarkDirty(txnId, rightSibling, profiler);
                            parentInternalNode.UpdateChildPointer(rightSiblingId, rightSibling.PageId);
                            // parentInternalNode.SetKey(rightSibling.PageId, newJoinKey); -- think this is wrong should be:
                            parentInternalNode.SetKey(childInternalNode.PageId, newJoinKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = false;
                            return;
                        }
                    }

                    // Merge with left-hand sibling
                    if (hasLeftSibling)
                    {
                        // Attempt to merge child node into its left sibling
                        var joinKey = parentInternalNode.GetKey(leftSibling.PageId);
                        var mergedNodeKey = parentInternalNode.GetKey(childInternalNode.PageId);
                        if (mergedNodeKey == null)
                        {
                            mergedNodeKey = new byte[_config.KeySize];
                            Array.Copy(childInternalNode.RightmostKey, mergedNodeKey, _config.KeySize);
                            ByteArrayHelper.Increment(mergedNodeKey);
                        }
                        if (leftSibling.Merge(childInternalNode, joinKey))
                        {
                            MarkDirty(txnId, leftSibling, profiler);
                            if (leftSibling.PageId != leftSiblingId)
                            {
                                // We have a new page id (append-only stores will do this)
                                parentInternalNode.UpdateChildPointer(leftSiblingId, leftSibling.PageId);
                            }
                            parentInternalNode.RemoveChildPointer(childInternalNode.PageId);
                            parentInternalNode.SetKey(leftSibling.PageId, mergedNodeKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = parentInternalNode.NeedJoin;
                            return;
                        }
                    }

                    // Merge with right-hand sibling
                    if (hasRightSibling)
                    {
                        // Attempt to merge right sibling into child node
                        var joinKey = parentInternalNode.GetKey(childNodeId);
                        if (childInternalNode.Merge(rightSibling, joinKey))
                        {
                            MarkDirty(txnId, childInternalNode, profiler);
                            var nodeKey = parentInternalNode.RemoveChildPointer(rightSiblingId);
                            if (childInternalNode.PageId != childNodeId)
                            {
                                // We have a new page id for the child node (append-only stores will do this)
                                parentInternalNode.UpdateChildPointer(childNodeId, childInternalNode.PageId);
                            }
                            if (nodeKey == null)
                            {
                                // We merged in the right-most node, so we need to generate a key
                                nodeKey = new byte[_config.KeySize];
                                Array.Copy(rightSibling.RightmostKey, nodeKey, _config.KeySize);
                                ByteArrayHelper.Increment(nodeKey);
                            }
                            parentInternalNode.SetKey(childInternalNode.PageId, nodeKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = parentInternalNode.NeedJoin;
                            return;
                        }
                    }

                    throw new NotImplementedException(
                        "Not yet implemented handling for internal node becoming under allocated");
                }
                underAllocation = false;
            }
            else
            {
                throw new BrightstarInternalException(String.Format("Unrecognised B+ Tree node class : {0}",
                                                                    childNode.GetType()));
            }
        }


        private ulong Insert(ulong txnId, INode node, byte[] key, byte[] value, out bool split, out INode rightNode, out byte[] splitKey, bool overwrite, BrightstarProfiler profiler)
        {
            if (node is LeafNode)
            {
                var leaf = node as LeafNode;
                if (leaf.IsFull)
                {
                    var newNode = leaf.Split(_pageStore.Create(), out splitKey);
                    _modifiedNodes[newNode.PageId] = newNode;
                    if (key.Compare(splitKey) < 0)
                    {
                        leaf.Insert(key, value, overwrite, profiler);
                    }
                    else
                    {
                        newNode.Insert(key, value, overwrite, profiler);
                    }
                    MarkDirty(txnId, leaf, profiler);
                    split = true;
                    rightNode = newNode;
                }
                else
                {
                    leaf.Insert(key, value, overwrite, profiler);
                    MarkDirty(txnId, leaf, profiler);
                    split = false;
                    rightNode = null;
                    splitKey = null;
                }
                return leaf.PageId;
            }
            else
            {
                var internalNode = node as InternalNode;
                var childNodeId = internalNode.GetChildNodeId(key);
                var childNode = GetNode(childNodeId, profiler);
                bool childSplit;
                INode rightChild;
                byte[] childSplitKey;
                var newChildNodeId = Insert(txnId, childNode, key, value, out childSplit, out rightChild, out childSplitKey, overwrite, profiler);
                if (childSplit)
                {
                    if (internalNode.IsFull)
                    {
                        using (profiler.Step("Split Internal Node"))
                        {
                            // Need to split this node to insert the new child node
                            rightNode = internalNode.Split(_pageStore.Create(), out splitKey);
                            _modifiedNodes[rightNode.PageId] = rightNode;
                            split = true;
                            if (childSplitKey.Compare(splitKey) < 0)
                            {
                                internalNode.Insert(childSplitKey, rightChild.PageId);
                            }
                            else
                            {
                                (rightNode as InternalNode).Insert(childSplitKey, rightChild.PageId);
                            }
                            // update child pointers if required (need to check both internalNode and rightNode as we don't know which side the modified child node ended up on)
                            if (newChildNodeId != childNodeId)
                            {
                                internalNode.UpdateChildPointer(childNodeId, newChildNodeId);
                                (rightNode as InternalNode).UpdateChildPointer(childNodeId, newChildNodeId);
                            }
                        }
                    }
                    else
                    {
                        using (profiler.Step("Insert into internal node"))
                        {
                            split = false;
                            rightNode = null;
                            splitKey = null;
                            internalNode.Insert(childSplitKey, rightChild.PageId);
                        }
                    }
                    using (profiler.Step("Update Child Pointer"))
                    {
                        if (newChildNodeId != childNodeId)
                        {
                            internalNode.UpdateChildPointer(childNodeId, newChildNodeId);
                        }
                        MarkDirty(txnId, internalNode, profiler);
                    }
                    return internalNode.PageId;
                }
                else
                {
                    using (profiler.Step("Update Child Pointer"))
                    {
                        if (newChildNodeId != childNodeId)
                        {
                            internalNode.UpdateChildPointer(childNodeId, newChildNodeId);
                            MarkDirty(txnId, internalNode, profiler);
                        }
                        split = false;
                        rightNode = null;
                        splitKey = null;
                        return internalNode.PageId;
                    }
                }
            }
        }

        private void MarkDirty(ulong txnId, INode node, BrightstarProfiler profiler)
        {
            using (profiler.Step("MarkDirty"))
            {
                if (!node.IsDirty)
                {
                    //_nodeCache.Remove(node.PageId);
                    _nodeCache.Remove(node);
                    if (!_pageStore.IsWriteable(node.PageId))
                    {
                        node.PageId = _pageStore.Create();
                    }
                    node.IsDirty = true;
                    _modifiedNodes[node.PageId] = node;
                }
                _pageStore.Write(txnId, node.PageId, node.GetData(), profiler: profiler);
                //Task.Factory.StartNew(() => _pageStore.Write(txnId, node.PageId, node.GetData(), profiler:null)); // Not passing through the profiler because it is not thread-safe
            }
        }

        public virtual ulong Save(ulong transactionId, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.Save"))
            {
                foreach (var n in _modifiedNodes.Values)
                {
                    _pageStore.Write(transactionId, n.PageId, n.GetData(), profiler: profiler);
                    n.IsDirty = false;
                    //_nodeCache.Add(n.PageId, new WeakReference(n));
                    _nodeCache.Add(n);
                }
                _modifiedNodes.Clear();
                return RootId;
            }
        }

        public void DumpStructure()
        {
            _root.DumpStructure(this, 0);
        }

        public void Insert(ulong txnId, ulong key, byte[] value, bool overwrite = false, BrightstarProfiler profiler = null)
        {
            Insert(txnId, BitConverter.GetBytes(key), value, overwrite, profiler);
        }

        public bool Search(ulong key, byte[] valueBuffer, BrightstarProfiler profiler)
        {
            return Search(BitConverter.GetBytes(key), valueBuffer, profiler);
        }

        public void Delete(ulong txnId, ulong key, BrightstarProfiler profiler)
        {
            Delete(txnId, BitConverter.GetBytes(key), profiler);
        }

        
    }
}
