using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using BrightstarDB.Utils;
using Remotion.Linq.Utilities;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class InternalNode : INode
    {
        private readonly BPlusTreeConfiguration _config;
        private int _keyCount;
        private readonly byte[][] _keys;
        private readonly ulong[] _childPointers;

        /// <summary>
        /// Get or set the ID of the page where this node is persisted
        /// </summary>
        public ulong PageId { get; set; }

        /// <summary>
        /// Get or set the boolean flag that indicates if this node has been modified since it was loaded
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Get the boolean flag that indicates if this node is a leaf node
        /// </summary>
        public bool IsLeaf
        {
            get { return false; }
        }

        /// <summary>
        /// Get the boolean flag that indicates if this node has reached the limit for the number of keys it can contain
        /// </summary>
        public bool IsFull
        {
            get { return _keyCount == _config.InternalBranchFactor; }
        }

        /// <summary>
        /// Get the current count of keys stored in this node
        /// </summary>
        public int KeyCount
        {
            get { return _keyCount; }
        }

        /// <summary>
        /// Attempt to merge this node with the specified sibling node
        /// </summary>
        /// <param name="s">The sibling to merge with</param>
        /// <param name="joinKey">The key that separates this node from the sibling</param>
        /// <returns>True if the merge completed successfully, false otherwise</returns>
        public bool Merge(INode s, byte[] joinKey)
        {
            var sibling = s as InternalNode;
            if(sibling == null)
            {
                throw new ArgumentException("Merge node is null or not an InternalNode", "s");
            }
            if (sibling.KeyCount + KeyCount < _config.InternalBranchFactor)
            {
                if (sibling.LeftmostKey.Compare(RightmostKey) > 0)
                {
                    // Append all of siblings entries 
                    _keys[KeyCount] = new byte[_config.KeySize];
                    Array.Copy(joinKey, _keys[KeyCount], _config.KeySize);
                    Array.Copy(sibling._keys, 0, _keys, KeyCount+1, sibling.KeyCount);
                    Array.Copy(sibling.ChildPointers, 0, _childPointers, KeyCount+1, sibling.KeyCount+1);
                    _keyCount = _keyCount + sibling.KeyCount + 1;
                    return true;
                }
                throw new InvalidOperationException("Attempted to merge in left node.");
            }
            return false;
        }

        /// <summary>
        /// Dump a trace of the structure of this node to the console
        /// </summary>
        /// <param name="tree">The tree that contains this node</param>
        /// <param name="indentLevel">The indent level to use when writing the structure</param>
        public void DumpStructure(BPlusTree tree, int indentLevel)
        {
            var keyPrefix = new string(' ', indentLevel*4);
            var pointerPrefix = keyPrefix + "  ";
            INode childNode;
            for (int i = 0; i < _keyCount; i++)
            {
                Console.WriteLine("{0}PTR[{1}]: {2}", pointerPrefix, i, _childPointers[i]);
                childNode = tree.GetNode(_childPointers[i], null);
                childNode.DumpStructure(tree, indentLevel + 1);
                Console.WriteLine("{0}KEY[{1}]: {2}", keyPrefix, i, _keys[i].Dump());
            }
            Console.WriteLine("{0}PTR[{1}]: {2}", pointerPrefix, _keyCount, _childPointers[_keyCount]);
            childNode = tree.GetNode(_childPointers[_keyCount], null);
            childNode.DumpStructure(tree, indentLevel + 1);
        }

        private InternalNode(ulong pageId, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            PageId = pageId;
            _keys = new byte[_config.InternalBranchFactor][];
            _childPointers = new ulong[_config.InternalBranchFactor + 1];
            _keyCount = 0;
            IsDirty = true;
        }

        public InternalNode(ulong pageId, byte[] key, ulong leftChild, ulong rightChild,
                            BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            PageId = pageId;
            _keys = new byte[_config.InternalBranchFactor][];
            _childPointers = new ulong[_config.InternalBranchFactor + 1];
            _keys[0] = key; // TODO: copy key ?
            _childPointers[0] = leftChild;
            _childPointers[1] = rightChild;
            _keyCount = 1;
            IsDirty = true;
        }

        public InternalNode(ulong id, byte[] nodePage, int keyCount, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            PageId = id;
            _keyCount = keyCount;
            _keys = new byte[_config.InternalBranchFactor][];
            for (int i = 0, offset = BPlusTreeConfiguration.InternalNodeHeaderSize;
                 i < _keyCount;
                 i++, offset += _config.KeySize)
            {
                _keys[i] = new byte[_config.KeySize];
                Array.Copy(nodePage, offset, _keys[i], 0, _config.KeySize);
            }
            _childPointers = ByteArrayHelper.ToUlongArray(nodePage, _config.InternalNodeChildStartOffset,
                                                          _config.InternalBranchFactor + 1);
        }

        public InternalNode(ulong id, List<byte[]> keys, List<ulong >childPointers, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            PageId = id;
            _keyCount = keys.Count;
            _keys = new byte[_config.InternalBranchFactor][];
            _childPointers = new ulong[_config.InternalBranchFactor + 1];
            for (int i = 0; i < _keyCount; i++)
            {
                _keys[i] = keys[i];
            }
            for(int i = 0; i <= _keyCount; i++)
            {
                _childPointers[i] = childPointers[i];
            }
        }

        public InternalNode(ulong id, ulong onlyChild, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            PageId = id;
            _keyCount = 0;
            _keys = new byte[_config.InternalBranchFactor][];
            _childPointers = new ulong[_config.InternalBranchFactor+1];
            _childPointers[0] = onlyChild;
        }

        /// <summary>
        /// Get the serialized representation of this node
        /// </summary>
        /// <returns>The serialized node representation as a byte array</returns>
        public byte[] GetData()
        {
            var buff = new byte[_config.PageSize];
            Array.Copy(BitConverter.GetBytes(~_keyCount), buff, 4);
            ByteArrayHelper.MultiCopy(_keys, buff, 4, _keyCount, _config.KeySize);
            ByteArrayHelper.ToByteArray(_childPointers, buff, _config.InternalNodeChildStartOffset, (_keyCount + 1)*8);
            return buff;
        }

        /// <summary>
        /// Returns the ID of the right-most child of this node that contains keys less than <paramref name="key"/>
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>The child node ID</returns>
        public ulong GetChildNodeId(byte[] key)
        {
            for (int i = 0; i < _keyCount; i++)
            {
                if (key.Compare(_keys[i]) < 0)
                {
                    return _childPointers[i];
                }
            }
            return _childPointers[_keyCount];
        }

        /// <summary>
        /// Get the highest key stored in this node
        /// </summary>
        public byte[] RightmostKey
        {
            get { return _keys[_keyCount - 1]; }
        }

        /// <summary>
        /// Get the lowest key stored in this node
        /// </summary>
        public byte[] LeftmostKey
        {
            get { return _keys[0]; }
        }

        /// <summary>
        /// Splits this node into two internal nodes, creating a new node for the right-hand (upper) half of the keys
        /// </summary>
        /// <param name="rightNodeId">The ID of the page reserved to receive the newly created internal node</param>
        /// <param name="splitKey">Receives the value of the key used for the split</param>
        /// <returns>The new right-hand node</returns>
        public InternalNode Split(ulong rightNodeId, out byte[] splitKey)
        {
            var rightNode = new InternalNode(rightNodeId, _config);
            var splitIndex = _config.InternalSplitIndex;
            splitKey = _keys[splitIndex];

            Array.Copy(_keys, splitIndex + 1,
                       rightNode._keys, 0,
                       _keyCount - (splitIndex + 1));
            int pointerCopyStart = splitIndex + 1;
            int pointerCopyLength = (_keyCount - splitIndex);
            Array.Copy(_childPointers, pointerCopyStart,
                       rightNode._childPointers, 0,
                       pointerCopyLength);

            rightNode._keyCount = _keyCount - (splitIndex + 1);
            _keyCount = splitIndex;

            return rightNode;
        }

        /// <summary>
        /// Inserts a new child pointer into this internal node
        /// </summary>
        /// <param name="key">The key for the child node</param>
        /// <param name="childPointer">The child node ID</param>
        /// <exception cref="NodeFullException">Raised if this node already contains the maximum number of child pointers allowed</exception>
        /// <exception cref="DuplicateKeyException">Raised if this node already contains a child node pointer with the same key</exception>
        public void Insert(byte[] key, ulong childPointer)
        {
            if (_keyCount == _config.InternalBranchFactor)
            {
                throw new NodeFullException();
            }

            var insertIndex = Array.BinarySearch(_keys, 0, _keyCount, key, _config);
            if (insertIndex >= 0)
            {
                throw new DuplicateKeyException();
            }

            insertIndex = ~insertIndex;
            for (int i = _keyCount; i > insertIndex; i--)
            {
                _keys[i] = _keys[i - 1];
            }
            for (int i = _keyCount + 1; i > insertIndex + 1; i--)
            {
                _childPointers[i] = _childPointers[i - 1];
            }
            _keys[insertIndex] = key; // TODO: copy key ?
            _childPointers[insertIndex + 1] = childPointer;
            _keyCount++;
        }

        /// <summary>
        /// Updates the node ID pointed to be a child pointer
        /// </summary>
        /// <param name="oldChildPointer">The old child node ID to be replaced</param>
        /// <param name="newChildPointer">The new child node ID</param>
        public void UpdateChildPointer(ulong oldChildPointer, ulong newChildPointer)
        {
            if (oldChildPointer == newChildPointer) return;
            for (var i = 0; i <= _keyCount; i++)
            {
                if (_childPointers[i] == oldChildPointer)
                {
                    _childPointers[i] = newChildPointer;
                    return;
                }
            }
        }

        /// <summary>
        /// Checks if a child node has a right-hand sibling and if so returns the siblings ID
        /// </summary>
        /// <param name="childNodeId">The child node to check</param>
        /// <param name="rightSiblingId">Receives the ID of the right-hand sibling</param>
        /// <returns>True if the child node has a right-hand sibling, false otherwise</returns>
        public bool GetRightSiblingId(ulong childNodeId, out ulong rightSiblingId)
        {
            for (int i = 0; i < _keyCount; i++)
            {
                if (_childPointers[i] == childNodeId)
                {
                    rightSiblingId = _childPointers[i + 1];
                    return true;
                }
            }
            rightSiblingId = 0;
            return false;
        }

        /// <summary>
        /// Checks if a child node has a left-hand sibling and if so returns the siblings ID
        /// </summary>
        /// <param name="childNodeId">The child node to check</param>
        /// <param name="leftSiblingId">Receives the ID of the left-hand sibling</param>
        /// <returns>True if the child node has a left-hand sibling, false otherwise</returns>
        public bool GetLeftSibling(ulong childNodeId, out ulong leftSiblingId)
        {
            if (_childPointers[0] == childNodeId)
            {
                // First child has no left sibling
                leftSiblingId = 0;
                return false;
            }

            for (int i = 1; i <= _keyCount; i++)
            {
                if (_childPointers[i] == childNodeId)
                {
                    leftSiblingId = _childPointers[i - 1];
                    return true;
                }
            }
            leftSiblingId = 0;
            return false;
        }

        /// <summary>
        /// Attempts to ensure that the minimum size for this node is achieved by transferring entries from the left-hand sibling
        /// </summary>
        /// <param name="leftSibling">The left-hand sibling that will provide entries</param>
        /// <param name="joinKey">The value of the key that is present in the parent node between the pointer to this node and its left sibling</param>
        /// <param name="newJoinKey">The replacement value for the join key in the parent node</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        public bool RedistributeFromLeft(InternalNode leftSibling, byte[] joinKey, byte[] newJoinKey)
        {
            int required = _config.InternalSplitIndex - _keyCount;
            if (leftSibling.KeyCount - required < _config.InternalSplitIndex)
            {
                // Can't fulfill requirements with a borrow from left sibling
                return false;
            }

            int evenOut = (_keyCount + leftSibling._keyCount) / 2 - _keyCount;
            if (leftSibling.KeyCount  - evenOut > _config.InternalSplitIndex)
            {
                required = evenOut;
            }

            // Make space for new keys and child pointers
            _childPointers[_keyCount + required] = _childPointers[_keyCount];
            for(int i = _keyCount -1; i >= 0; i-- )
            {
                _keys[i + required] = _keys[i];
                _childPointers[i + required] = _childPointers[i];
            }
            SetKey(required-1, joinKey);
            CopyKeys(leftSibling._keys, (leftSibling._keyCount - required) + 1, 0, required - 1);
            //Array.Copy(leftSibling._keys, (leftSibling._keyCount - required) + 1, _keys, 0, required-1);
            Array.Copy(leftSibling._childPointers, (leftSibling._keyCount-required) + 1, _childPointers, 0, required);
            Array.Copy(leftSibling._keys[leftSibling.KeyCount-required], newJoinKey, _config.KeySize);
            _keyCount += required;
            leftSibling._keyCount -= required;
            return true;
        }

        private void SetKey(int ix, byte [] value)
        {
            _keys[ix] = new byte[_config.KeySize];
            Array.Copy(value, _keys[ix], _config.KeySize);
        }

        private void CopyKeys(byte[][] sourceKeys, int sourceOffset, int targetOffset, int count)
        {
            for(int i = 0; i <count; i++)
            {
                SetKey(targetOffset+i, sourceKeys[sourceOffset+i]);
            }
        }

        public bool RedistributeFromRight(InternalNode rightSibling, byte[] joinKey, byte [] newJoinKey)
        {
            int required = _config.InternalSplitIndex - _keyCount;
            if (rightSibling.KeyCount - required < _config.InternalSplitIndex)
            {
                return false;
            }

            // Copy keys and child pointers
            SetKey(_keyCount, joinKey);
            CopyKeys(rightSibling._keys, 0, _keyCount + 1, required - 1);
            //Array.Copy(rightSibling._keys, 0, _keys, _keyCount + 1, required - 1);
            Array.Copy(rightSibling._childPointers, 0, _childPointers, _keyCount + 1, required);
            Array.Copy(rightSibling._keys[required - 1], newJoinKey, _config.KeySize);

            // Shift up remaining keys and child pointers in the right node
            for(int i  = 0; i < rightSibling._keyCount - required; i++)
            {
                rightSibling._keys[i] = rightSibling._keys[i + required];
                rightSibling._childPointers[i] = rightSibling.ChildPointers[i + required];
            }
            rightSibling._childPointers[rightSibling._keyCount - required] = rightSibling._childPointers[rightSibling.KeyCount];
            rightSibling._keyCount -= required;
            _keyCount += required;
            return true;
        }

        /// <summary>
        /// Modifies the key that is immediately before the one that indexes the specified child node
        /// </summary>
        /// <param name="childNodeId">The child node pointer</param>
        /// <param name="childNodeKey">The new key value</param>
        /// <remarks>If <paramref name="childNodeId"/> is the ID of the first child node, this operation returns without modifying this node at all</remarks>
        public void SetLeftKey(ulong childNodeId, byte[] childNodeKey)
        {
            if (childNodeKey == null)
            {
                throw new ArgumentNullException("childNodeKey");
            }
            if (childNodeId == _childPointers[0])
            {
                // First pointer doesn't have a corresponding key to update
                return;
            }
            for (int i = 1; i <= _keyCount; i++)
            {
                if (_childPointers[i] == childNodeId)
                {
                    if (_keys[i-1] == null)
                    {
                        _keys[i-1] = new byte[_config.KeySize];
                    }
                    Array.Copy(childNodeKey, _keys[i-1], _config.KeySize);
                }
            }
        }

        public byte[] GetKey(ulong childNodeId)
        {
            var ix = Array.IndexOf(_childPointers, childNodeId);
            if (ix >= 0) return _keys[ix];
            return null;
        }

        public void SetKey(ulong  childNodeId, byte[] newKey)
        {
            var ix = Array.IndexOf(_childPointers, childNodeId);
            if(ix < 0) throw new ArgumentException("Cannot find child node", "childNodeId");
            if (_keys[ix] == null)
            {
                _keys[ix] = new byte[_config.KeySize];
            }
            Array.Copy(newKey, _keys[ix], _config.KeySize);
        }

        /// <summary>
        /// Removes the pointer to the specified child node
        /// </summary>
        /// <param name="childNodeId">The child node pointer to be removed</param>
        /// <remarks>The key that indexes the child node is also removed</remarks>
        public byte[] RemoveChildPointer(ulong childNodeId)
        {
            var pointerIndex = Array.IndexOf(_childPointers, childNodeId);
            if (pointerIndex == _keyCount)
            {
                // Removed the end pointer, so chop off the last key
                _keyCount--;
                _childPointers[pointerIndex] = 0;
                return _keys[_keyCount]; // Return the key we just chopped off the end of the array
            }
            else
            {
                // Removed an internal pointer, so copy all keys to its right down one place (overwriting the key for the deleted entry)
                var ret = _keys[pointerIndex];
                // _keys.ShiftDown(pointerIndex - 1);
                _keys.ShiftDown(pointerIndex);
                _childPointers.ShiftDown(pointerIndex);
                _keyCount--;
                return ret;
            }
        }

        /// <summary>
        /// Get the boolean flag that indicates if this node has fewer entries than the minimum allowed
        /// </summary>
        public bool NeedJoin
        {
            get { return _keyCount < _config.InternalSplitIndex; }
        }

        public ulong[] ChildPointers
        {
            get { return _childPointers; }
        }

        /// <summary>
        /// Determine if this node contains the specified key
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>True if this node contains the specified key, false otherwise</returns>
        public bool ContainsKey(byte[] key)
        {
            return (Array.BinarySearch(_keys, 0, _keyCount, key, _config) >= 0);
        }

        /// <summary>
        /// Returns the range of child node pointers for keys start from fromKey up to toKey
        /// </summary>
        /// <param name="fromKey"></param>
        /// <param name="toKey"></param>
        /// <returns></returns>
        public IEnumerable<ulong> Scan(byte[] fromKey, byte[] toKey)
        {
            for(int i = 0; i < _keyCount; i++)
            {
                if (fromKey.Compare(_keys[i]) < 0)
                {
                    yield return _childPointers[i];
                    if (toKey.Compare(_keys[i]) < 0)
                    {
                        yield break;
                    }
                }
                if (toKey.Compare(_keys[i]) < 0)
                {
                    yield return _childPointers[i];
                    yield break;
                }
            }
            yield return _childPointers[_keyCount];
        }

        public IEnumerable<ulong> Scan()
        {
            return _childPointers.Take(_keyCount + 1);
        }
    }
}