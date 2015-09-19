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
        private ulong _rootId;
        private readonly BPlusTreeConfiguration _config;
        private readonly IPageStore _pageStore;
        private bool _isDirty;

        public BPlusTreeConfiguration Configuration { get { return _config; } }

        /// <summary>
        /// Creates a new tree in the page store
        /// </summary>
        /// <param name="txnId">The transaction id for the update</param>
        /// <param name="pageStore"></param>
        /// <param name="keySize">The size of the B+ tree's key (in bytes)</param>
        /// <param name="dataSize">The size of the values stored in leaf nodes (in bytes)</param>
        public BPlusTree(ulong txnId, IPageStore pageStore, int keySize = 8, int dataSize = 64) 
        {
            _config = new BPlusTreeConfiguration(pageStore, keySize, dataSize, pageStore.PageSize);
            _pageStore = pageStore;
            var root = MakeLeafNode(txnId);
            _rootId = root.PageId;
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
            _config = new BPlusTreeConfiguration(pageStore, keySize, dataSize, pageStore.PageSize);
            _pageStore = pageStore;
            var root = GetNode(rootPageId, profiler);
            _rootId = root.PageId;
        }

        protected IPageStore PageStore { get { return _pageStore; } }

        /// <summary>
        /// Get a flag indicating if this tree contains unsaved modifications
        /// </summary>
        public bool IsModified { get { return _isDirty; } }

        /// <summary>
        /// Get the ID of the root node of the tree
        /// </summary>
        public ulong RootId { get { return _rootId; } }

        public INode GetNode(ulong nodeId, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.GetNode"))
            {
                using (profiler.Step("Load Node"))
                {
                    INode ret;
                    var nodePage = _pageStore.Retrieve(nodeId, profiler);
                    var header = BitConverter.ToInt32(nodePage.Data, 0);
                    if (header < 0)
                    {
                        ret = MakeInternalNode(nodePage, ~header);
#if DEBUG_BTREE
                        _config.BTreeDebug("{0}: Loaded INTERNAL node from page {1}. {2}",_config.DebugId, nodePage.Id, ret.ToString());
#endif
                    }
                    else
                    {
                        ret = MakeLeafNode(nodePage, header);
#if DEBUG_BTREE
                        _config.BTreeDebug("{0}: Loaded LEAF node from page {1}. {2}", _config.DebugId, nodePage.Id, ret.ToString());
#endif
                    }
                    return ret;
                }
            }
        }

        public bool Search(byte[] key, byte[] valueBuff, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.Search"))
            {
                INode u = GetNode(_rootId, profiler);
                while (u is IInternalNode)
                {
                    var internalNode = u as IInternalNode;
                    u = GetNode(internalNode.GetChildNodeId(key), profiler);
                }
                var l = u as ILeafNode;
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
                var root = GetNode(_rootId, profiler);
                Insert(txnId, root, key, value, out splitRoot, out rightNode, out rootSplitKey, overwrite, profiler);
                if (splitRoot)
                {
                    var newRoot = MakeInternalNode(_pageStore.Create(txnId), rootSplitKey, root.PageId,
                                                   rightNode.PageId);
                    //var newRoot = new InternalNode(_pageStore.Create(), rootSplitKey, _root.PageId, rightNode.PageId,
                    //                               _config);
                    MarkDirty(txnId, root, profiler);
                    MarkDirty(txnId, newRoot, profiler);
                    _rootId = newRoot.PageId;
#if DEBUG_BTREE
                    _config.BTreeDebug("BPlusTree.Insert: Root node has split. New root ID {0}: {1}",_rootId, newRoot.Dump());
#endif
                }
                else
                {
                    // Update root page pointer
                    // If the store is a BinaryFilePageStore, then the root page ID shouldn't change.
                    // If the store is an AppendOnlyPageSTore, then the root will change if the root 
                    // is a leaf node or if a lower level split bubbled up to insert a new key into 
                    // the root node.
                    _rootId = root.PageId;
#if DEBUG_BTREE
                    _config.BTreeDebug("BPlusTree.Insert: Updated root node id is {0}", _rootId);
#endif
                }
            }
        }

        public void Delete(ulong txnId, byte[] key, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.Delete"))
            {
                var root = GetNode(_rootId, profiler);
                if (root is ILeafNode)
                {
                    (root as ILeafNode).Delete(txnId, key);
                    MarkDirty(txnId, root, profiler);
                    // Update root page pointer - see note in Insert() method above
                    _rootId = root.PageId;
                }
                else
                {
                    bool underAllocation;
                    Delete(txnId, root as IInternalNode, key, out underAllocation, profiler);
                    if (root.KeyCount == 0)
                    {
                        // Now has only a single child leaf node, which should become the new tree root
                        root = GetNode((root as IInternalNode).GetChildPointer(0), profiler);
                        _rootId = root.PageId;
                    }
                    else
                    {
                        // Update root page pointer - see note in Insert() method above
                        _rootId = root.PageId;
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
                return Scan(GetNode(_rootId, profiler), fromKey, toKey, profiler);
            }
        }

        public IEnumerable<KeyValuePair<byte[], byte []>> Scan(BrightstarProfiler profiler)
        {
            using (profiler.Step("Scan Entire BTree"))
            {
                return Scan(GetNode(_rootId, profiler), profiler);
            }
        }

        private IEnumerable<KeyValuePair<byte[], byte[]>> Scan(INode node, BrightstarProfiler profiler)
        {
            if (node is IInternalNode)
            {
                var internalNode = node as IInternalNode;
                foreach(var childNodeId in internalNode.Scan())
                {
                    foreach(var entry in Scan(GetNode(childNodeId, profiler), profiler))
                    {
                        yield return entry;
                    }
                }
            }
            if (node is ILeafNode)
            {
                var leaf = node as ILeafNode;
                foreach(var entry in leaf.Scan())
                {
                    yield return entry;
                }
            }
        }

        private IEnumerable<KeyValuePair<byte[], byte[]>> Scan(INode node, byte[] fromKey, byte[] toKey, BrightstarProfiler profiler)
        {
            if (node is IInternalNode)
            {
                var internalNode = node as IInternalNode;
                foreach(var childNodeId in internalNode.Scan(fromKey, toKey))
                {
                    foreach(var entry in Scan(GetNode(childNodeId, profiler), fromKey, toKey, profiler))
                    {
                        yield return entry;
                    }
                }
            }
            else if (node is ILeafNode)
            {
                var leaf = node as ILeafNode;
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

        private void Delete(ulong txnId, IInternalNode parentInternalNode, byte[] key, out bool underAllocation, BrightstarProfiler profiler)
        {
            if (parentInternalNode.RightmostKey == null)
            {
                throw new ArgumentException("Parent node right key is null");
            }
            var childNodeId = parentInternalNode.GetChildNodeId(key);
            var childNode = GetNode(childNodeId, profiler);
            if (childNode is ILeafNode)
            {
                var childLeafNode = childNode as ILeafNode;
                // Delete the key and mark the node as updated. This may update the child node id
                childLeafNode.Delete(txnId, key);
                MarkDirty(txnId, childLeafNode, profiler);
                if (childLeafNode.PageId != childNodeId)
                {
                    parentInternalNode.UpdateChildPointer(txnId, childNodeId, childLeafNode.PageId);
                    childNodeId = childLeafNode.PageId;
                }

                if (childLeafNode.NeedsJoin)
                {
                    ulong leftSiblingId, rightSiblingId;
                    ILeafNode leftSibling = null, rightSibling = null;
                    bool hasLeftSibling = parentInternalNode.GetLeftSibling(childNodeId, out leftSiblingId);
                    if (hasLeftSibling)
                    {
                        leftSibling = GetNode(leftSiblingId, profiler) as ILeafNode;
                        if (childLeafNode.RedistributeFromLeft(txnId, leftSibling))
                        {
                            parentInternalNode.SetLeftKey(txnId, childLeafNode.PageId, childLeafNode.LeftmostKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            MarkDirty(txnId, leftSibling, null);
                            parentInternalNode.UpdateChildPointer(txnId, leftSiblingId, leftSibling.PageId);
                            underAllocation = false;
                            return;
                        }
                    }
                    bool hasRightSibling = parentInternalNode.GetRightSiblingId(childNodeId, out rightSiblingId);
                    
                        
                    if (hasRightSibling)
                    {
                        rightSibling = GetNode(rightSiblingId, profiler) as ILeafNode;
#if DEBUG
                        if (rightSibling.LeftmostKey.Compare(childLeafNode.RightmostKey) <= 0)
                        {
                            throw new Exception("Right-hand sibling has a left key lower than this nodes right key.");
                        }
#endif
                        if (childLeafNode.RedistributeFromRight(txnId, rightSibling))
                        {
                            MarkDirty(txnId, rightSibling, profiler);
                            parentInternalNode.UpdateChildPointer(txnId, rightSiblingId, rightSibling.PageId);
                            parentInternalNode.SetLeftKey(txnId, rightSibling.PageId, rightSibling.LeftmostKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = false;
                            return;
                        }
                    }
                    if (hasLeftSibling && childLeafNode.Merge(txnId, leftSibling))
                    {
                        parentInternalNode.RemoveChildPointer(txnId, leftSiblingId);
                        parentInternalNode.SetLeftKey(txnId, childLeafNode.PageId, childLeafNode.LeftmostKey);
                        MarkDirty(txnId, parentInternalNode, profiler);
                        underAllocation = parentInternalNode.NeedJoin;
                        return;
                    }
                    if (hasRightSibling && childLeafNode.Merge(txnId, rightSibling))
                    {
                        byte[] nodeKey = parentInternalNode.RemoveChildPointer(txnId, rightSiblingId);
                        if (nodeKey == null)
                        {
                            // We merged in the right-most node, so we need to generate a key
                            nodeKey = new byte[_config.KeySize];
                            Array.Copy(rightSibling.RightmostKey, nodeKey, _config.KeySize);
                            ByteArrayHelper.Increment(nodeKey);
                        }
                        parentInternalNode.SetKey(txnId, childLeafNode.PageId, nodeKey);
                        MarkDirty(txnId, parentInternalNode, profiler);
                        underAllocation = parentInternalNode.NeedJoin;
                        return;
                    }
                }
                underAllocation = false;
                return;
            }


            if (childNode is IInternalNode)
            {
                bool childUnderAllocated;
                var childInternalNode = childNode as IInternalNode;
                Delete(txnId, childInternalNode, key, out childUnderAllocated, profiler);
                if (childInternalNode.PageId != childNodeId)
                {
                    // Child node page changed
                    parentInternalNode.UpdateChildPointer(txnId, childNodeId, childInternalNode.PageId);
                    MarkDirty(txnId, parentInternalNode, profiler);
                    childNodeId = childInternalNode.PageId;
                }

                if (childUnderAllocated)
                {
                    IInternalNode leftSibling = null, rightSibling = null;
                    ulong leftSiblingId, rightSiblingId;

                    // Redistribute values from left-hand sibling
                    bool hasLeftSibling = parentInternalNode.GetLeftSibling(childNodeId, out leftSiblingId);
                    if (hasLeftSibling)
                    {
                        leftSibling = GetNode(leftSiblingId, profiler) as IInternalNode;
                        byte[] joinKey = parentInternalNode.GetKey(leftSiblingId);
                        var newJoinKey = new byte[_config.KeySize];
                        if (childInternalNode.RedistributeFromLeft(txnId, leftSibling, joinKey, newJoinKey))
                        {
                            MarkDirty(txnId, leftSibling, profiler);
                            parentInternalNode.UpdateChildPointer(txnId, leftSiblingId, leftSibling.PageId);
                            parentInternalNode.SetKey(txnId, leftSibling.PageId, newJoinKey);
                            MarkDirty(txnId, parentInternalNode, profiler);
                            underAllocation = false;
                            return;
                        }
                    }

                    // Redistribute values from right-hand sibling
                    bool hasRightSibling = parentInternalNode.GetRightSiblingId(childNodeId, out rightSiblingId);
                    if (hasRightSibling)
                    {
                        rightSibling = GetNode(rightSiblingId, profiler) as IInternalNode;
                        byte[] joinKey = parentInternalNode.GetKey(childInternalNode.PageId);
                        byte[] newJoinKey = new byte[_config.KeySize];
                        if (childInternalNode.RedistributeFromRight(txnId, rightSibling, joinKey, newJoinKey))
                        {
                            MarkDirty(txnId, rightSibling, profiler);
                            parentInternalNode.UpdateChildPointer(txnId, rightSiblingId, rightSibling.PageId);
                            // parentInternalNode.SetKey(rightSibling.PageId, newJoinKey); -- think this is wrong should be:
                            parentInternalNode.SetKey(txnId, childInternalNode.PageId, newJoinKey);
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
                        if (leftSibling.Merge(txnId, childInternalNode, joinKey))
                        {
                            MarkDirty(txnId, leftSibling, profiler);
                            if (leftSibling.PageId != leftSiblingId)
                            {
                                // We have a new page id (append-only stores will do this)
                                parentInternalNode.UpdateChildPointer(txnId, leftSiblingId, leftSibling.PageId);
                            }
                            parentInternalNode.RemoveChildPointer(txnId, childInternalNode.PageId);
                            parentInternalNode.SetKey(txnId, leftSibling.PageId, mergedNodeKey);
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
                        if (childInternalNode.Merge(txnId, rightSibling, joinKey))
                        {
                            MarkDirty(txnId, childInternalNode, profiler);
                            var nodeKey = parentInternalNode.RemoveChildPointer(txnId, rightSiblingId);
                            if (childInternalNode.PageId != childNodeId)
                            {
                                // We have a new page id for the child node (append-only stores will do this)
                                parentInternalNode.UpdateChildPointer(txnId, childNodeId, childInternalNode.PageId);
                            }
                            if (nodeKey == null)
                            {
                                // We merged in the right-most node, so we need to generate a key
                                nodeKey = new byte[_config.KeySize];
                                Array.Copy(rightSibling.RightmostKey, nodeKey, _config.KeySize);
                                ByteArrayHelper.Increment(nodeKey);
                            }
                            parentInternalNode.SetKey(txnId, childInternalNode.PageId, nodeKey);
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
            if (node is ILeafNode)
            {
#if DEBUG_BTREE
                _config.BTreeDebug("BPlusTree.Insert Key={0} into LEAF node {1}", key.Dump(), node.PageId);
#endif
                var leaf = node as ILeafNode;
                if (leaf.IsFull)
                {
#if DEBUG_BTREE
                    _config.BTreeDebug("BPlusTree.Insert. Target leaf node is full.");
#endif
                    var newPage = _pageStore.Create(txnId);
                    var newNode = leaf.Split(txnId, newPage, out splitKey);
                    if (key.Compare(splitKey) < 0)
                    {
                        leaf.Insert(txnId, key, value, overwrite: overwrite, profiler: profiler);
                    }
                    else
                    {
                        newNode.Insert(txnId, key, value, overwrite: overwrite, profiler: profiler);
                    }
                    MarkDirty(txnId, leaf, profiler);
                    MarkDirty(txnId, newNode, profiler);
                    split = true;
                    rightNode = newNode;
                }
                else
                {
                    leaf.Insert(txnId, key, value, overwrite: overwrite, profiler: profiler);
                    MarkDirty(txnId, leaf, profiler);
                    split = false;
                    rightNode = null;
                    splitKey = null;
                }
                return leaf.PageId;
            }
            else
            {
#if DEBUG_BTREE
                _config.BTreeDebug("BPlusTree.Insert Key={0} into INTERNAL node {1}", key.Dump(), node.PageId);
#endif
                var internalNode = node as IInternalNode;
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
#if DEBUG_BTREE
                        _config.BTreeDebug("BPlusTree.Insert: Root node is full.");
#endif
                        using (profiler.Step("Split Internal Node"))
                        {
                            // Need to split this node to insert the new child node
                            rightNode = internalNode.Split(txnId, _pageStore.Create(txnId), out splitKey);
                            MarkDirty(txnId, rightNode, profiler);
                            split = true;
                            if (childSplitKey.Compare(splitKey) < 0)
                            {
                                internalNode.Insert(txnId, childSplitKey, rightChild.PageId);
                            }
                            else
                            {
                                (rightNode as IInternalNode).Insert(txnId, childSplitKey, rightChild.PageId);
                            }
                            // update child pointers if required (need to check both internalNode and rightNode as we don't know which side the modified child node ended up on)
                            if (newChildNodeId != childNodeId)
                            {
                                internalNode.UpdateChildPointer(txnId, childNodeId, newChildNodeId);
                                (rightNode as IInternalNode).UpdateChildPointer(txnId, childNodeId, newChildNodeId);
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
                            internalNode.Insert(txnId, childSplitKey, rightChild.PageId);
                        }
                    }
                    using (profiler.Step("Update Child Pointer"))
                    {
                        if (newChildNodeId != childNodeId)
                        {
                            internalNode.UpdateChildPointer(txnId, childNodeId, newChildNodeId);
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
                            internalNode.UpdateChildPointer(txnId, childNodeId, newChildNodeId);
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
            _isDirty = true;
            _pageStore.MarkDirty(txnId, node.PageId);
        }

        public virtual ulong Save(ulong transactionId, BrightstarProfiler profiler)
        {
            using (profiler.Step("BPlusTree.Save"))
            {
                _isDirty = false;
                return RootId;
            }
        }

        public void DumpStructure()
        {
            GetNode(_rootId, null).DumpStructure(this, 0);
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

        public int PreloadTree(int numPages, BrightstarProfiler profiler)
        {
            if (numPages == 0) return 0;
            var pageQueue = new Queue<ulong>(numPages);
            pageQueue.Enqueue(_rootId);

            var numLoaded = 0;
            while (numLoaded < numPages)
            {
                ulong pageId;
                try
                {
                    pageId = pageQueue.Dequeue();
                }
                catch (InvalidOperationException)
                {
                    // Raised when the queue is empty
                    return numLoaded;
                }
                var node = GetNode(pageId, profiler);
                numLoaded++;
                if (node is IInternalNode)
                {
                    var internalNode = node as IInternalNode;
                    foreach (var childPageId in internalNode.Scan())
                    {
                        pageQueue.Enqueue(childPageId);
                    }
                }
            }
            return numLoaded;
        }

        

        #region Node factory methods

        private ILeafNode MakeLeafNode(ulong txnId)
        {
            return new LeafNode(_pageStore.Create(txnId), 0, 0, _config);
        }

        private ILeafNode MakeLeafNode(IPage nodePage, int keyCount)
        {
            return new LeafNode(nodePage, keyCount, _config);
        }

        private INode MakeInternalNode(IPage nodePage, int keyCount)
        {
            return new InternalNode(nodePage, keyCount, _config);
        }

        private INode MakeInternalNode(IPage nodePage, byte[] rootSplitKey, ulong leftPageId, ulong rightPageId)
        {
            return new InternalNode(nodePage, rootSplitKey, leftPageId, rightPageId, _config);
        }

        #endregion
    }
}
