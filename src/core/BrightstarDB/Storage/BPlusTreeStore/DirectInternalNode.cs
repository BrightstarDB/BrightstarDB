using System;
using System.Collections.Generic;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class DirectInternalNode : IInternalNode
    {

        private readonly BPlusTreeConfiguration _config;
        private int _keyCount;
        private ulong _pageId;
        private byte[] _pageData;

        private DirectInternalNode(ulong pageId, BPlusTreeConfiguration treeConfig)
        {
            // TODO: Replace this with a version that takes the IPage
            _pageId = pageId;
            _config = treeConfig;
            _pageData = new byte[treeConfig.PageSize];
            _keyCount = 0;
        }

        public DirectInternalNode(ulong pageId, byte[] pageData, int keyCount, BPlusTreeConfiguration treeConfig)
        {
            _pageId = pageId;
            _config = treeConfig;
            _pageData = pageData;
            _keyCount = keyCount;
        }

        public DirectInternalNode(ulong pageId, byte[] splitKey, ulong leftPageId, ulong rightPageId,
                                  BPlusTreeConfiguration treeConfiguration)
        {
            _pageId = pageId;
            _config = treeConfiguration;
            _pageData = new byte[treeConfiguration.PageSize];
            Array.Copy(splitKey, 0, _pageData, KeyOffset(0), _config.KeySize);
            Array.Copy(BitConverter.GetBytes(leftPageId), 0, _pageData, PointerOffset(0), 8);
            Array.Copy(BitConverter.GetBytes(rightPageId), 0, _pageData, PointerOffset(1), 8);
            KeyCount = 1;
        }

        public DirectInternalNode(ulong pageId, ulong onlyChild, BPlusTreeConfiguration treeConfiguration)
        {
            _pageId = pageId;
            _config = treeConfiguration;
            _pageData = new byte[treeConfiguration.PageSize];
            Array.Copy(BitConverter.GetBytes(onlyChild), 0, _pageData, PointerOffset(0), 8);
            KeyCount = 0;
        }

        public DirectInternalNode(ulong pageId, List<byte[]> keys, List<ulong> values, BPlusTreeConfiguration treeConfiguration)
        {
            _pageId = pageId;
            _config = treeConfiguration;
            _pageData = new byte[treeConfiguration.PageSize];
            KeyCount = keys.Count;
            int i, keyOffset, pointerOffset;
            for (i = 0, keyOffset = KeyOffset(0); i < keys.Count; i++, keyOffset+=_config.KeySize)
            {
                Array.Copy(keys[i], 0, _pageData, keyOffset, _config.KeySize);
            }
            for (i = 0, pointerOffset = PointerOffset(0); i < KeyCount + 1; i++, pointerOffset += 8)
            {
                Array.Copy(BitConverter.GetBytes(values[i]), 0, _pageData, pointerOffset, 8);
            }
        }

        public ulong PageId { get { return _pageId; } set { _pageId = value; } }

        public bool IsDirty { get; set; }

        public bool IsLeaf
        {
            get { return false; }
        }

        public bool IsFull { get { return _keyCount == _config.InternalBranchFactor; } }

        public byte[] RightmostKey { get { return GetKey(_keyCount - 1); } }
        public byte[] LeftmostKey { get { return GetKey(0); } }

        public byte[] GetData()
        {
            return _pageData;
        }

        public int KeyCount
        {
            get { return _keyCount; }
            private set
            {
                _keyCount = value;
                Array.Copy(BitConverter.GetBytes(~_keyCount), _pageData, 4);
                IsDirty = true;
            }
        }

        public void DumpStructure(BPlusTree tree, int indentLevel)
        {
            var keyPrefix = new string(' ', indentLevel * 4);
            var pointerPrefix = keyPrefix + "  ";
            INode childNode;
            for (int i = 0; i < _keyCount; i++)
            {
                var childPointer = GetPointer(i);
                Console.WriteLine("{0}PTR[{1}]: {2}", pointerPrefix, i, childPointer);
                childNode = tree.GetNode(childPointer, null);
                childNode.DumpStructure(tree, indentLevel + 1);
                Console.WriteLine("{0}KEY[{1}]: {2}", keyPrefix, i, GetKey(i).Dump());
            }
            var lastPointer = GetPointer(KeyCount);
            Console.WriteLine("{0}PTR[{1}]: {2}", pointerPrefix, _keyCount, lastPointer);
            childNode = tree.GetNode(lastPointer, null);
            childNode.DumpStructure(tree, indentLevel + 1);
        }

        public bool NeedJoin { get { return KeyCount < _config.InternalSplitIndex; } }

        public bool Merge(INode s, byte[] joinKey)
        {
            var sibling = s as DirectInternalNode;
            if (sibling == null)
            {
                throw new ArgumentException("Merge node is null or not a DirectInternalNode", "s");
            }
            if (sibling.KeyCount + KeyCount < _config.InternalBranchFactor)
            {
                if (sibling.LeftmostKey.Compare(RightmostKey) > 0)
                {
                    // Append join key followed by all of sibling's entries
                    Array.Copy(joinKey, 0, _pageData, KeyOffset(KeyCount), _config.KeySize);
                    Array.Copy(sibling._pageData, KeyOffset(0),
                               _pageData, KeyOffset(KeyCount + 1),
                               sibling.KeyCount*_config.KeySize);
                    Array.Copy(sibling._pageData, PointerOffset(0),
                               _pageData, PointerOffset(KeyCount + 1),
                               (sibling.KeyCount + 1)*8);
                    KeyCount = KeyCount + sibling.KeyCount + 1;
                    return true;
                }
                throw new InvalidOperationException("Attempted to merge in left node.");
            }
            return false;
        }

        public ulong GetChildNodeId(byte[] key)
        {
            for (int i = 0, offset = KeyOffset(0); i < _keyCount; i++, offset+=_config.KeySize)
            {
                if (key.Compare(0, _pageData, offset, _config.KeySize) < 0)
                {
                    return GetPointer(i);
                }
            }
            return GetPointer(KeyCount);
        }

        /// <summary>
        /// Splits this node into two internal nodes, creating a new node for the right-hand (upper) half of the keys
        /// </summary>
        /// <param name="rightNodeId">The ID of the page reserved to receive the newly created internal node</param>
        /// <param name="splitKey">Receives the value of the key used for the split</param>
        /// <returns>The new right-hand node</returns>
        public IInternalNode Split(ulong rightNodeId, out byte[] splitKey)
        {
            var rightNode = new DirectInternalNode(rightNodeId, _config);
            var splitIndex = _config.InternalSplitIndex;
            splitKey = GetKey(splitIndex);

            Array.Copy(_pageData, KeyOffset(splitIndex + 1),
                       rightNode._pageData, KeyOffset(0),
                       (KeyCount - (splitIndex + 1))*_config.KeySize);
            var pointerCopyStart = PointerOffset(splitIndex + 1);
            var pointerCopyLength = (KeyCount - splitIndex)*8;
            Array.Copy(_pageData, pointerCopyStart,
                       rightNode._pageData, PointerOffset(0),
                       pointerCopyLength);
            rightNode.KeyCount = KeyCount - (splitIndex + 1);
            KeyCount = splitIndex;
            return rightNode;
        }

        public void Insert(byte[] key, ulong childPointer)
        {
            if (_keyCount == _config.InternalBranchFactor)
            {
                throw new NodeFullException();
            }

            var insertIndex = Search(key);
            if (insertIndex >= 0)
            {
                throw new DuplicateKeyException();
            }

            insertIndex = ~insertIndex;
            RightShiftFrom(insertIndex, 1);
            Array.Copy(key, 0, _pageData, KeyOffset(insertIndex), _config.KeySize);
            Array.Copy(BitConverter.GetBytes(childPointer), 0,
                       _pageData, PointerOffset(insertIndex + 1),
                       8);
            KeyCount++;
        }

        /// <summary>
        /// Updates the node ID pointed to be a child pointer
        /// </summary>
        /// <param name="oldChildPointer">The old child node ID to be replaced</param>
        /// <param name="newChildPointer">The new child node ID</param>
        public void UpdateChildPointer(ulong oldChildPointer, ulong newChildPointer)
        {
            if (oldChildPointer == newChildPointer) return;
            int i, pointerOffset;
            byte[] ocp = BitConverter.GetBytes(oldChildPointer);
            byte[] ncp = BitConverter.GetBytes(newChildPointer);
            for (i = 0, pointerOffset = PointerOffset(0); i <= KeyCount; i++, pointerOffset += 8)
            {
                if (_pageData.Compare(pointerOffset, ocp, 0, 8) == 0)
                {
                    Array.Copy(ncp, 0, _pageData, pointerOffset, 8);
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
            int i, pointerOffset;
            byte[] cni = BitConverter.GetBytes(childNodeId);
            for(i=0,pointerOffset=PointerOffset(0); i < KeyCount; i++, pointerOffset+=8)
            {
                if (_pageData.Compare(pointerOffset, cni, 0, 8) == 0)
                {
                    rightSiblingId = GetPointer(i + 1);
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
            int i, pointerOffset;
            byte[] cni = BitConverter.GetBytes(childNodeId);
            if (_pageData.Compare(PointerOffset(0), cni, 0, 8) == 0)
            {
                // First child has no left sibling
                leftSiblingId = 0;
                return false;
            }

            
            for (i = 1, pointerOffset=PointerOffset(1); i <= KeyCount; i++, pointerOffset += 8)
            {
                if (_pageData.Compare(pointerOffset, cni, 0, 8) == 0)
                {
                    leftSiblingId = GetPointer(i - 1);
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
        public bool RedistributeFromLeft(IInternalNode leftSibling, byte[] joinKey, byte[] newJoinKey)
        {
            DirectInternalNode left = leftSibling as DirectInternalNode;
            if (left == null)
            {
                throw new ArgumentException("Expected a DirectInternalNode as left sibling", "leftSibling");
            }

            int required = _config.InternalSplitIndex - KeyCount;
            if (leftSibling.KeyCount - required < _config.InternalSplitIndex)
            {
                // Can't fulfill requirements with a borrow from left sibling
                return false;
            }

            int evenOut = (KeyCount + left.KeyCount)/2 - KeyCount;
            if (leftSibling.KeyCount - evenOut > _config.InternalSplitIndex)
            {
                required = evenOut;
            }

            // Make space for new keys and child pointers
            RightShift(required);
            Array.Copy(joinKey, 0, _pageData, KeyOffset(required - 1), _config.KeySize);
            Array.Copy(left._pageData, KeyOffset(left.KeyCount - (required- 1)),
                       _pageData, KeyOffset(0),
                       (required - 1)*_config.KeySize);
            Array.Copy(left._pageData, PointerOffset(left.KeyCount - (required - 1)),
                       _pageData, PointerOffset(0),
                       (required)*8);
            Array.Copy(left._pageData, KeyOffset(left.KeyCount-required), newJoinKey, 0, _config.KeySize);
            KeyCount += required;
            left.KeyCount -= required;
            return true;
        }

        public bool RedistributeFromRight(IInternalNode rightSibling, byte[] joinKey, byte[] newJoinKey)
        {
            var right = rightSibling as DirectInternalNode;
            if (right == null) throw new ArgumentException("Expected a DirectInternalNode as right sibling", "rightSibling");

            int required = _config.InternalSplitIndex - _keyCount;
            if (rightSibling.KeyCount - required < _config.InternalSplitIndex)
            {
                return false;
            }

            // Copy keys and child pointers
            Array.Copy(joinKey, 0, _pageData, KeyOffset(KeyCount), _config.KeySize); // Set key[KeyCount+1] to joinKey
            Array.Copy(right._pageData, KeyOffset(0),
                       _pageData, KeyOffset(KeyCount + 1),
                       (required - 1)*_config.KeySize);
            Array.Copy(right._pageData, PointerOffset(0),
                       _pageData, PointerOffset(KeyCount + 1),
                       required*8);
            Array.Copy(right._pageData, KeyOffset(required - 1),
                       newJoinKey, 0, _config.KeySize);
            right.LeftShift(required);
            KeyCount += required;
            return true;
        }

        public void SetLeftKey(ulong childNodeId, byte[] childNodeKey)
        {
            if (childNodeKey == null)
            {
                throw new ArgumentNullException("childNodeKey");
            }
            byte[] cni = BitConverter.GetBytes(childNodeId);
            if (_pageData.Compare(PointerOffset(0), cni, 0, 8) == 0)
            {
                // First pointer doesn't have a corresponding key to update
                return;
            }
            int i, pointerOffset;
            for (i = 1, pointerOffset = PointerOffset(1); i <= KeyCount; i++, pointerOffset += 8)
            {
                if (_pageData.Compare(pointerOffset, cni, 0, 8) == 0)
                {
                    Array.Copy(childNodeKey, 0, _pageData, KeyOffset(i - 1), _config.KeySize);
                }
            }
        }

        public byte[] GetKey(ulong childNodeId)
        {
            var ix = Search(childNodeId);
            if (ix >= 0) return GetKey(ix);
            return null;
        }

        public void SetKey(ulong childNodeId, byte[] newKey)
        {
            var ix = Search(childNodeId);
            if (ix < 0) throw new ArgumentException("Cannot find child node " + childNodeId, "childNodeId");
            Array.Copy(newKey, 0, _pageData, KeyOffset(ix), _config.KeySize);
        }

        public byte[] RemoveChildPointer(ulong childNodeId)
        {
            var pointerIndex = Search(childNodeId);
            if (pointerIndex < 0)
            {
                    throw new ArgumentException("Cannot find child node " + childNodeId, "childNodeId");
            }
            if (pointerIndex == KeyCount)
            {
                // Removed the end pointer, so retrieve the last key (to be returned) and then chop it off 
                byte[] ret = GetKey(KeyCount);
                KeyCount--;
                return ret;
            }
            else
            {
                // Removed an internal pointer so copy all keys to its right up one place
                var ret = GetKey(pointerIndex);
                LeftShiftFrom(pointerIndex, 1);
                return ret;
            }
        }

        public bool ContainsKey(byte[] key)
        {
            return Search(key) >= 0;
        }

        public IEnumerable<ulong> Scan(byte[] fromKey, byte[] toKey)
        {
            int i, pointerOffset, keyOffset;
            for (i = 0, pointerOffset = PointerOffset(0), keyOffset = KeyOffset(0);
                 i < KeyCount;
                 i++, pointerOffset += 8, keyOffset += _config.KeySize)
            {
                if (fromKey.Compare(0, _pageData, keyOffset, _config.KeySize) < 0)
                {
                    yield return GetPointer(i);
                    if (toKey.Compare(0, _pageData, keyOffset, _config.KeySize) < 0)
                    {
                        yield break;
                    }
                }
                if (toKey.Compare(0, _pageData, keyOffset, _config.KeySize) < 0)
                {
                    yield return GetPointer(i);
                    yield break;
                }
            }
            yield return GetPointer(KeyCount);
        }

        public IEnumerable<ulong> Scan()
        {
            for (int i = 0; i <= KeyCount; i++)
            {
                yield return GetPointer(i);
            }
        }

        public ulong GetChildPointer(int ix)
        {
            if (ix > KeyCount) throw new ArgumentOutOfRangeException("ix");
            return GetPointer(ix);
        }

        private int KeyOffset(int keyIx)
        {
            return BPlusTreeConfiguration.InternalNodeHeaderSize + (keyIx*_config.KeySize);
        }

        private int PointerOffset(int pointerIx)
        {
            return _config.InternalNodeChildStartOffset + (pointerIx*8);
        }

        private byte[] GetKey(int keyIx)
        {
            byte[] buff = new byte[_config.KeySize];
            GetKey(keyIx, buff);
            return buff;
        }

        private void GetKey(int keyIx, byte[] buff)
        {
            Array.Copy(_pageData, KeyOffset(keyIx), buff, 0, _config.KeySize);
        }

        private ulong GetPointer(int pointerIx)
        {
            byte[] buff = new byte[8];
            GetPointer(pointerIx, buff);
            return BitConverter.ToUInt64(buff, 0);
        }

        private void GetPointer(int pointerIx, byte[] buff)
        {
            Array.Copy(_pageData, PointerOffset(pointerIx), buff, 0, 8);
        }

        private int Search(byte[] key)
        {
            // TODO: replace with a binary search algorithm
            for (int i = 0, keyOffset = KeyOffset(0);
                 i < KeyCount;
                 i++, keyOffset += _config.KeySize)
            {
                var cmp = key.Compare(0, _pageData, keyOffset, _config.KeySize);
                if (cmp == 0) return i;
                if (cmp < 0) return ~i;
            }
            return ~KeyCount;
        }

        private int Search(ulong pointer)
        {
            // Note: this has to be a linear search as pointer values are in no particular order
            int i, pointerOffset;
            byte[] p = BitConverter.GetBytes(pointer);
            for (i = 0, pointerOffset = PointerOffset(0); i < KeyCount + 1; i++, pointerOffset+=8)
            {
                var cmp = _pageData.Compare(pointerOffset, p, 0, 8);
                if ( cmp == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        private void RightShiftFrom(int ix, int numPlaces)
        {
            int i, keyOffset, pointerOffset;
            int keyShift = numPlaces * _config.KeySize;
            int pointerShift = numPlaces*8;
            int lastPointerOffset = PointerOffset(KeyCount);
            Array.Copy(_pageData, lastPointerOffset, _pageData, lastPointerOffset+pointerShift, 8);
            for (i = KeyCount - 1,
                 keyOffset = KeyOffset(KeyCount - 1),
                 pointerOffset = PointerOffset(KeyCount - 1);
                 i >= ix;
                 i--, keyOffset -= _config.KeySize, pointerOffset -= 8)
            {
                Array.Copy(_pageData, keyOffset, _pageData, keyOffset + keyShift, _config.KeySize);
                Array.Copy(_pageData, pointerOffset, _pageData, pointerOffset + pointerShift, 8);
            }
        }

        private void RightShift(int numPlaces)
        {
            RightShiftFrom(0, numPlaces);
        }

        private void LeftShift(int numPlaces)
        {
            LeftShiftFrom(0, numPlaces);
        }

        private void LeftShiftFrom(int ix, int numPlaces)
        {
            Array.Copy(_pageData, KeyOffset(ix + numPlaces),
                       _pageData, KeyOffset(ix),
                       (KeyCount - ix - numPlaces) * _config.KeySize);
            Array.Copy(_pageData, PointerOffset(ix + numPlaces),
                       _pageData, PointerOffset(ix),
                       (KeyCount - ix - numPlaces + 1) * 8);
            KeyCount -= numPlaces;
        }
    }
}
