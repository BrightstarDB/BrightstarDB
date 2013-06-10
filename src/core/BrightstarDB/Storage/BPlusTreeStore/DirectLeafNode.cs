using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Profiling;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class DirectLeafNode : ILeafNode
    {
        private ulong _nodeId;
        private readonly byte[] _nodeData;
        private readonly BPlusTreeConfiguration _config;
        private int _keyCount;
        private ulong _prevPointer;
        private ulong _nextPointer;

        /// <summary>
        /// Creates a leaf node that will be backed by a new page
        /// </summary>
        /// <param name="reservedPageId">The reserved id of the new page</param>
        /// <param name="prevPointer">UNUSED</param>
        /// <param name="nextPointer">UNUSED</param>
        /// <param name="treeConfiguration">The tree configuration parameters</param>
        public DirectLeafNode(ulong reservedPageId, ulong prevPointer, ulong nextPointer,
                              BPlusTreeConfiguration treeConfiguration)
        {
            // TODO: Replace uses of this constructor with one which provides the new page buffer
            _config = treeConfiguration;
            _nodeId = reservedPageId;
            _nodeData = new byte[treeConfiguration.PageSize];
            KeyCount = 0;
            Prev = prevPointer;
            Next = nextPointer;
        }

        /// <summary>
        /// Creates a new leaf node with content loaded from an ordered enumeration of key/value pairs
        /// </summary>
        /// <param name="reservedPageId"></param>
        /// <param name="prevPointer"></param>
        /// <param name="nextPointer"></param>
        /// <param name="treeConfiguration"></param>
        /// <param name="orderedValues">The enumerateion of key-value pairs to load into the leaf node</param>
        /// <param name="numValuesToLoad">The maximum number of key-value pairs to insert into the new leaf node</param>
        public DirectLeafNode(ulong reservedPageId, ulong prevPointer, ulong nextPointer,
                              BPlusTreeConfiguration treeConfiguration,
                              IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues,
                              int numValuesToLoad)
            : this(
                reservedPageId, new byte[treeConfiguration.PageSize], prevPointer, nextPointer, treeConfiguration,
                orderedValues, numValuesToLoad)
        {
            // TODO: Replace uses of this constructor with one which provides the new page buffer
        }

        public DirectLeafNode(ulong nodeId, byte[] nodeData, int keyCount, BPlusTreeConfiguration treeConfiguration)
        {
            _nodeId = nodeId;
            _nodeData = nodeData;
            _keyCount = keyCount;
            _prevPointer = BitConverter.ToUInt64(nodeData, 4);
            _nextPointer = BitConverter.ToUInt64(nodeData, 12);
            _config = treeConfiguration;
            IsDirty = false;
        }

        public DirectLeafNode(ulong nodeId, byte[] nodeData, ulong prevPointer, ulong nextPointer,
                              BPlusTreeConfiguration treeConfiguration,
                              IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues, int numValuesToLoad)
        {
            _nodeId = nodeId;
            _nodeData = nodeData;
            _config = treeConfiguration;
            Prev = prevPointer;
            Next = nextPointer;
            int numLoaded = 0;
            foreach (var kvp in orderedValues.Take(numValuesToLoad))
            {
                SetKey(numLoaded, kvp.Key);
                if (_config.ValueSize > 0)
                {
                    SetValue(numLoaded, kvp.Value);
                }
                numLoaded++;
            }
            KeyCount = numLoaded;
        }

        public ulong Prev
        {
            get { return _prevPointer; }
            private set
            {
                _prevPointer = value;
                Array.Copy(BitConverter.GetBytes(_prevPointer), 0, _nodeData, 4, 8);
                IsDirty = true;
            }
        }



        public bool NeedsJoin { get { return KeyCount < _config.LeafSplitIndex; } }

        public ulong Next
        {
            get { return _nextPointer; }
            private set
            {
                _nextPointer = value;
                Array.Copy(BitConverter.GetBytes(_nextPointer), 0, _nodeData, 12, 8);
                IsDirty = true;
            }
        }

        public int KeyCount
        {
            get { return _keyCount; }
            private set
            {
                _keyCount = value;
                Array.Copy(BitConverter.GetBytes(_keyCount), _nodeData, 4);
                IsDirty = true;
            }
        }

        #region ILeafNode methods
        public bool Merge(INode s)
        {
            var sibling = s as DirectLeafNode;
            if (sibling == null)
            {
                throw new ArgumentException("Merge node is null or not a DirectLeafNode", "s");
            }

            if (sibling.KeyCount + KeyCount <= _config.LeafLoadFactor)
            {
                if (sibling.LeftmostKey.Compare(RightmostKey) > 0)
                {
                    // Append all of the siblings entries
                    Array.Copy(sibling._nodeData, KeyOffset(0),
                               _nodeData, KeyOffset(KeyCount),
                               sibling.KeyCount*_config.KeySize);
                    if (_config.ValueSize > 0)
                    {
                        Array.Copy(sibling._nodeData, ValueOffset(0),
                                   _nodeData, ValueOffset(KeyCount),
                                   sibling.KeyCount*_config.ValueSize);
                    }
                }
                else
                {
                    // Prepend all of the sibling entries
                    RightShift(sibling.KeyCount);
                    Array.Copy(sibling._nodeData, KeyOffset(0),
                               _nodeData, KeyOffset(0),
                               sibling.KeyCount*_config.KeySize);
                    Array.Copy(sibling._nodeData, ValueOffset(0),
                               _nodeData, ValueOffset(0),
                               sibling.KeyCount*_config.ValueSize);
                }
                KeyCount += sibling.KeyCount;
                return true;
            }
            return false;
        }

        public void Insert(byte[] key, byte[] value, bool overwrite = false, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("LeafNode.Insert"))
            {
                if (IsFull) throw new NodeFullException();
                int insertIndex = Search(key);
                if (insertIndex >= 0)
                {
                    if (overwrite)
                    {
                        Array.Copy(value, 0, 
                            _nodeData, ValueOffset(insertIndex),
                            Math.Min(value.Length, _config.ValueSize));
                        return;
                    }
                    throw new DuplicateKeyException();
                }
                insertIndex = ~insertIndex;
                RightShiftFrom(insertIndex, 1);
                Array.Copy(key, 0, _nodeData, KeyOffset(insertIndex), _config.KeySize);
                if (_config.ValueSize > 0 && value != null)
                {
                    Array.Copy(value, 0, _nodeData, ValueOffset(insertIndex), Math.Min(_config.ValueSize, value.Length));
                }
                KeyCount++;
            }
        }

        public ILeafNode Split(ulong newNodeId, out byte[] splitKey)
        {
            var rightNode = new DirectLeafNode(newNodeId, PageId, Next, _config);
            Next = newNodeId;
            splitKey = new byte[_config.KeySize];
            int numToMove = KeyCount - _config.LeafSplitIndex;
            Array.Copy(_nodeData, KeyOffset(_config.LeafSplitIndex), splitKey, 0, _config.KeySize);
            Array.Copy(_nodeData, KeyOffset(_config.LeafSplitIndex),
                       rightNode._nodeData, KeyOffset(0),
                       numToMove*_config.KeySize);
            if (_config.ValueSize > 0)
            {
                Array.Copy(_nodeData, ValueOffset(_config.LeafSplitIndex),
                           rightNode._nodeData, ValueOffset(0),
                           numToMove*_config.ValueSize);
            }
            rightNode.KeyCount = numToMove;
            KeyCount = _config.LeafSplitIndex;
            return rightNode;
        }

        public bool GetValue(byte[] key, byte[] buffer)
        {
            int index = Search(key);
            if (index >= 0)
            {
                if (_config.ValueSize > 0)
                {
                    Array.Copy(_nodeData, ValueOffset(index), buffer, 0, _config.ValueSize);
                }
                return true;
            }
            return false;
        }

        public void Delete(byte[] key)
        {
            int deleteIndex = Search(key);
            if (deleteIndex >= 0)
            {
                int moveUp = KeyCount - (deleteIndex + 1);
                if (moveUp > 0)
                {
                    Array.Copy(_nodeData, KeyOffset(deleteIndex + 1),
                               _nodeData, KeyOffset(deleteIndex),
                               moveUp*_config.KeySize);
                    Array.Copy(_nodeData, ValueOffset(deleteIndex + 1),
                               _nodeData, ValueOffset(deleteIndex),
                               moveUp*_config.ValueSize);
                }
                KeyCount--;
            }
        }

        public bool RedistributeFromLeft(ILeafNode leftNode)
        {
            var left = leftNode as DirectLeafNode;
            if (left == null) throw new ArgumentException("Expected a DirectLeafNode instance", "leftNode");

            int copyCount = (KeyCount + left.KeyCount)/2 - KeyCount;
            if (copyCount > 0)
            {
                RightShift(copyCount);
                // Copy keys and data from left node
                int keyOffset = KeyOffset(left.KeyCount - copyCount);
                Array.Copy(left._nodeData, keyOffset, _nodeData, KeyOffset(0), copyCount*_config.KeySize);
                if (_config.ValueSize > 0)
                {
                    int valueOffset = ValueOffset(left.KeyCount - copyCount);
                    Array.Copy(left._nodeData, valueOffset, _nodeData, ValueOffset(0), copyCount*_config.ValueSize);
                }
                KeyCount += copyCount;
                left.KeyCount -= copyCount;
                return true;
            }
            return false;
        }


        public bool RedistributeFromRight(ILeafNode rightNode)
        {
            var right = rightNode as DirectLeafNode;
            if (right == null) throw new ArgumentException("Expected a DirectLeafNode instance", "rightNode");

            int copyCount = (KeyCount + rightNode.KeyCount)/2 - KeyCount;
            if (copyCount > 0)
            {
                // Copy keys and data from right
                Array.Copy(right._nodeData, BPlusTreeConfiguration.LeafNodeHeaderSize,
                           _nodeData, KeyOffset(KeyCount),
                           copyCount*_config.KeySize);
                if (_config.ValueSize > 0)
                {
                    Array.Copy(right._nodeData, _config.LeafDataStartOffset,
                        _nodeData, ValueOffset(KeyCount),
                        copyCount * _config.ValueSize);
                }
                // Shift up the remaining keys in the right
                right.LeftShift(copyCount);
                // Update my key count
                KeyCount += copyCount;
                return true;
            }
            return false;
        }

        public IEnumerable<KeyValuePair<byte[], byte[]>> Scan()
        {
            for (int i = 0; i < _keyCount; i++)
            {
                yield return new KeyValuePair<byte[], byte[]>(GetKey(i), GetValue(i));
            }
        }

        public IEnumerable<KeyValuePair<byte[], byte[]>> Scan(byte[] fromKey, byte[] toKey)
        {
            // TODO: Replace this with a version that passes in a key/value buffer to fill instead of always creating a new KeyValuePair
            int startOffset, startIx, offset, ix;
            int endIx = BPlusTreeConfiguration.LeafNodeHeaderSize + (_keyCount*_config.KeySize);
            for (startOffset = BPlusTreeConfiguration.LeafNodeHeaderSize, startIx = 0; startOffset < endIx && _nodeData.Compare(startOffset, fromKey, 0, _config.KeySize) < 0; startOffset+=_config.KeySize, startIx++ ){}
            for (offset = startOffset, ix = startIx;
                 offset < endIx && (_nodeData.Compare(offset, toKey, 0, _config.KeySize) <= 0);
                 offset += _config.KeySize, ix++)
            {
                yield return new KeyValuePair<byte[], byte[]>(GetKey(ix), GetValue(ix));
            }
        }

        #endregion

        #region Implementation of INode

        public ulong PageId
        {
            get { return _nodeId; }
            //set { throw new NotSupportedException("Cannot change the PageId for a DirectLeafNode"); }
            set { _nodeId = value; }
        }

        public bool IsDirty { get; set; }

        public bool IsLeaf { get { return true; } }
        public bool IsFull { get { return KeyCount == _config.LeafLoadFactor; } }

        public byte[] RightmostKey
        {
            get
            {
                var buff = new byte[_config.KeySize];
                GetKey(KeyCount - 1, buff);
                return buff;
            }
        }

        public byte[] LeftmostKey
        {
            get
            {
                var buff = new byte[_config.KeySize];
                GetKey(0, buff);
                return buff;
            }
        }

        public byte[] GetData()
        {
            return _nodeData;
        }

        public void DumpStructure(BPlusTree tree, int indentLevel)
        {
            if (KeyCount == 0)
            {
                Console.WriteLine("EMPTY LEAF NODE");
                return;
            }
            Console.WriteLine("{0}LEAF@{1}[{2} keys: {3} - {4}]",
                new string(' ', indentLevel*4), PageId, KeyCount, LeftmostKey.Dump(), RightmostKey.Dump());
        }
        #endregion

        /// <summary>
        /// Remove <paramref name="count"/> entries from the left of this node and move remaining entries up
        /// </summary>
        /// <param name="count"></param>
        private void LeftShift(int count)
        {
            int remaining = KeyCount - count;
            Array.Copy(_nodeData, BPlusTreeConfiguration.LeafNodeHeaderSize + (count*_config.KeySize),
                       _nodeData, BPlusTreeConfiguration.LeafNodeHeaderSize,
                       remaining*_config.KeySize);
            Array.Copy(_nodeData, _config.LeafDataStartOffset + (count*_config.ValueSize),
                       _nodeData, _config.LeafDataStartOffset,
                       remaining*_config.ValueSize);
            KeyCount -= count;
        }

        /// <summary>
        /// Move all keys and values in this node right by <paramref name="count"/> places
        /// </summary>
        /// <param name="count"></param>
        private void RightShift(int count)
        {
            int i, keyOffset;
            int keyShift = count*_config.KeySize;
            if (_config.ValueSize > 0)
            {
                int valueOffset;
                int valueShift = count * _config.ValueSize;
                for (i = KeyCount - 1,
                     keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize + ((KeyCount - 1)*_config.KeySize),
                     valueOffset = _config.LeafDataStartOffset + ((KeyCount - 1)*_config.ValueSize);
                     i >= 0;
                     i--, keyOffset -= _config.KeySize, valueOffset -= _config.ValueSize)
                {
                    Array.Copy(_nodeData, keyOffset, _nodeData, keyOffset + keyShift, _config.KeySize);
                    Array.Copy(_nodeData, valueOffset, _nodeData, valueOffset + valueShift, _config.ValueSize);
                }
            }
            else
            {
                for (i = KeyCount - 1,
                     keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize + ((KeyCount - 1) * _config.KeySize);
                     i >= 0;
                     i--, keyOffset -= _config.KeySize)
                {
                    Array.Copy(_nodeData, keyOffset, _nodeData, keyOffset + keyShift, _config.KeySize);
                }
            }
        }

        private void RightShiftFrom(int ix, int numPlaces)
        {
            int i, keyOffset;
            int keyShift = numPlaces*_config.KeySize;
            if (_config.ValueSize > 0)
            {
                int valueOffset;
                int valueShift = numPlaces*_config.ValueSize;
                for (i = KeyCount - 1,
                     keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize + ((KeyCount - 1) * _config.KeySize),
                     valueOffset = _config.LeafDataStartOffset + ((KeyCount - 1) * _config.ValueSize);
                     i >= ix;
                     i--, keyOffset -= _config.KeySize, valueOffset -= _config.ValueSize)
                {
                    Array.Copy(_nodeData, keyOffset, _nodeData, keyOffset + keyShift, _config.KeySize);
                    Array.Copy(_nodeData, valueOffset, _nodeData, valueOffset + valueShift, _config.ValueSize);
                }
            }
            else
            {
                for (i = KeyCount - 1,
                     keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize + ((KeyCount - 1) * _config.KeySize);
                     i >= ix;
                     i--, keyOffset -= _config.KeySize)
                {
                    Array.Copy(_nodeData, keyOffset, _nodeData, keyOffset + keyShift, _config.KeySize);
                }
            }
        }
        private byte[] GetKey(int ix)
        {
            var buff = new byte[_config.KeySize];
            GetKey(ix, buff);
            return buff;
        }

        /// <summary>
        /// Reads the key at offset <paramref name="ix"/> into the provided buffer
        /// </summary>
        /// <param name="ix">The offset of the key to be read</param>
        /// <param name="buff">The buffer to load the key into</param>
        private void GetKey(int ix, byte[] buff)
        {
            var offset = BPlusTreeConfiguration.LeafNodeHeaderSize + (_config.KeySize*ix);
            Array.Copy(_nodeData, offset, buff, 0, _config.KeySize);
        }

        /// <summary>
        /// Writes the key at offset <paramref name="ix"/> from the provided buffer
        /// </summary>
        /// <param name="ix">The offset of the key to be written</param>
        /// <param name="buff">The value to be written for the key</param>
        private void SetKey(int ix, byte[] buff)
        {
            var offset = BPlusTreeConfiguration.LeafNodeHeaderSize + (_config.KeySize * ix);
            Array.Copy(buff, 0, _nodeData, offset, _config.KeySize);
        }

        private byte[] GetValue(int ix)
        {
            var buff = new byte[_config.ValueSize];
            GetValue(ix, buff);
            return buff;
        }

        /// <summary>
        /// Reads the value at offset <paramref name="ix"/> into the provided buffer
        /// </summary>
        /// <param name="ix">The offset of the value to be read</param>
        /// <param name="buff">The buffer to load the value into</param>
        private void GetValue(int ix , byte[] buff)
        {
            var offset = _config.LeafDataStartOffset + (_config.ValueSize*ix);
            Array.Copy(_nodeData, offset, buff, 0, _config.ValueSize);
        }


        /// <summary>
        /// Writes the value at offset <paramref name="ix"/> from the provided buffer
        /// </summary>
        /// <param name="ix">The offset of the value to be written</param>
        /// <param name="buff">The new value to write</param>
        private void SetValue(int ix, byte[] buff)
        {
            var offset = _config.LeafDataStartOffset + (_config.ValueSize*ix);
            Array.Copy(buff, 0, _nodeData, offset, _config.ValueSize);
        }

        private int Search(byte[] key)
        {
            // TODO: replace with a binary search algorithm
            for (int i = 0, keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize;
                 i < KeyCount;
                 i++,keyOffset += _config.KeySize)
            {
                var cmp = key.Compare(0, _nodeData, keyOffset, _config.KeySize);
                if (cmp == 0) return i;
                if (cmp < 0) return ~i;
            }
            return ~KeyCount;
        }

        private int KeyOffset(int keyIndex)
        {
            return BPlusTreeConfiguration.LeafNodeHeaderSize + (keyIndex*_config.KeySize);
        }

        private int ValueOffset(int valueIndex)
        {
            return _config.LeafDataStartOffset + (valueIndex*_config.ValueSize);
        }
    }
}
