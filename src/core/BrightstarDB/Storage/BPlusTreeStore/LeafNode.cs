using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class LeafNode : INode
    {
        private readonly BPlusTreeConfiguration _config;
        private readonly byte[][] _dataSegments;
        private readonly byte[][] _keys;
        private readonly ulong _prevPointer;
        private int _keyCount;
        private ulong _nextPointer;

        public LeafNode(ulong reservedPageId, ulong prevPointer, ulong nextPointer, BPlusTreeConfiguration treeConfig)
        {
            _config = treeConfig;
            _keyCount = 0;
            _prevPointer = prevPointer;
            _nextPointer = nextPointer;
            _keys = new byte[_config.LeafLoadFactor][];
            _dataSegments = new byte[_config.LeafLoadFactor][];
            PageId = reservedPageId;
            IsDirty = true;
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
        public LeafNode(ulong reservedPageId, ulong prevPointer, ulong nextPointer, BPlusTreeConfiguration treeConfiguration, IEnumerable<KeyValuePair<byte[], byte []>> orderedValues, int numValuesToLoad)
        {
            _config = treeConfiguration;
            _prevPointer = prevPointer;
            _nextPointer = nextPointer;
            _keys = new byte[_config.LeafLoadFactor][];
            _dataSegments = new byte[_config.LeafLoadFactor][];
            PageId = reservedPageId;
            IsDirty = true;
            _keyCount = 0;
            foreach(var kvp in orderedValues.Take(numValuesToLoad))
            {
                _keys[_keyCount] = new byte[_config.KeySize];
                _dataSegments[_keyCount] = new byte[_config.ValueSize];
                Array.Copy(kvp.Key, _keys[_keyCount], _config.KeySize);
                if (_config.ValueSize > 0)
                {
                    Array.Copy(kvp.Value, _dataSegments[_keyCount], _config.ValueSize);
                }
                _keyCount++;
            }
        }

        public LeafNode(ulong nodeId, byte[] nodePage, int keyCount, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            _keyCount = keyCount;
            _prevPointer = BitConverter.ToUInt64(nodePage, 4);
            _nextPointer = BitConverter.ToUInt64(nodePage, 12);
            _keys = new byte[_config.LeafLoadFactor][];
            _dataSegments = new byte[_config.LeafLoadFactor][];

            for (int i = 0, j = BPlusTreeConfiguration.LeafNodeHeaderSize; i < _keyCount; i++,j += _config.KeySize)
            {
                _keys[i] = new byte[_config.KeySize];
                Array.Copy(nodePage, j, _keys[i], 0, _config.KeySize);
            }
            if (_config.ValueSize > 0)
            {
                for (int i = 0, j = _config.LeafDataStartOffset; i < _keyCount; i++, j += (_config.ValueSize + 1))
                {
                    byte dataLength = nodePage[j];
                    _dataSegments[i] = new byte[dataLength];
                    Array.Copy(nodePage, j + 1, _dataSegments[i], 0, dataLength);
                }
            }
            PageId = nodeId;
            IsDirty = false;
        }

        /// <summary>
        /// Returns an enumeration of all key-value pairs in this leaf node that fall in the range <paramref name="fromKey"/> to <paramref name="toKey"/> (inclusive)
        /// </summary>
        /// <param name="fromKey">The lowest key to return in the enumeration</param>
        /// <param name="toKey">The highest key to return in the enumeration</param>
        /// <returns>An enumeration of key value pairs</returns>
        public IEnumerable<KeyValuePair<byte[], byte[]>> Scan(byte[] fromKey, byte[] toKey)
        {
            int startIx;
            for (startIx = 0; (startIx < _keyCount) && _keys[startIx].Compare(fromKey) < 0; startIx++)
            {
            }
            for (int i = startIx; i < _keyCount && (_keys[i].Compare(toKey) <= 0); i++)
            {
                yield return new KeyValuePair<byte[], byte[]>(_keys[i], _dataSegments[i]);
            }
        }

        /// <summary>
        /// Boolean flag indicating if the number of elements in this leaf node has fallen below the minimum allowed
        /// </summary>
        public bool NeedsJoin
        {
            get { return _keyCount < _config.LeafSplitIndex; }
        }

        /// <summary>
        /// Get the ID of the next leaf node in the btree
        /// </summary>
        public ulong Next
        {
            get { return _nextPointer; }
        }

        /// <summary>
        /// Get the ID of the previous leaf node in the btree
        /// </summary>
        public ulong Prev
        {
            get { return _prevPointer; }
        }

        #region INode Members

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
            get { return true; }
        }

        /// <summary>
        /// Get the boolean flag that indicates if this node has reached the limit for the number of keys it can contain
        /// </summary>
        public bool IsFull
        {
            get { return _keyCount == _config.LeafLoadFactor; }
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
        /// <returns>True if the merge completed successfully, false otherwise</returns>
        public bool Merge(INode s)
        {
            var sibling = s as LeafNode;
            if (sibling == null)
            {
                throw new ArgumentException("Merge node is null or not a LeafNode", "s");
            }

            if (sibling.KeyCount + KeyCount <= _config.LeafLoadFactor)
            {
                if (sibling.LeftmostKey.Compare(RightmostKey) > 0)
                {
                    // Append all of siblings entries
                    Array.Copy(sibling._keys, 0, _keys, _keyCount, sibling._keyCount);
                    Array.Copy(sibling._dataSegments, 0, _dataSegments, _keyCount, sibling._keyCount);
                    _keyCount += sibling._keyCount;
                    return true;
                }
                // Prepend all of siblings entries
                for (int i = _keyCount - 1; i >= 0; i--)
                {
                    _keys[i + sibling._keyCount] = _keys[i];
                    _dataSegments[i + sibling._keyCount] = _dataSegments[i];
                }
                Array.Copy(sibling._keys, 0, _keys, 0, sibling._keyCount);
                Array.Copy(sibling._dataSegments, 0, _dataSegments, 0, sibling._keyCount);
                _keyCount += sibling._keyCount;
                return true;
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
            if (_keyCount == 0)
            {
                Console.WriteLine("EMPTY LEAF NODE");
                return;
            }
            Console.WriteLine("{0}LEAF@{1}[{2} keys: {3} - {4}]", new string(' ', indentLevel*4), PageId, _keyCount, _keys[0].Dump(), _keys[_keyCount - 1].Dump());
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
        /// Get the serialized representation of this node
        /// </summary>
        /// <returns>The serialized node representation as a byte array</returns>
        public byte[] GetData()
        {
            var buff = new byte[_config.PageSize];
            Array.Copy(BitConverter.GetBytes(_keyCount), buff, 4);
            Array.Copy(BitConverter.GetBytes(_prevPointer), 0, buff, 4, 8);
            Array.Copy(BitConverter.GetBytes(_nextPointer), 0, buff, 12, 8);
            for (int i = 0, offset = BPlusTreeConfiguration.LeafNodeHeaderSize;
                 i < _keyCount;
                 i++,offset += _config.KeySize)
            {
                Array.Copy(_keys[i], 0, buff, offset, _config.KeySize);
            }
            if (_config.ValueSize > 0)
            {
                for (int i = 0, offset = _config.LeafDataStartOffset;
                     i < _keyCount;
                     i++, offset += _config.ValueSize + 1)
                {
                    buff[offset] = (byte) _dataSegments[i].Length;
                    Array.Copy(_dataSegments[i], 0, buff, offset + 1, _dataSegments[i].Length);
                }
            }
            return buff;
        }

        #endregion

        /// <summary>
        /// Inserts a key-value pair into this leaf node
        /// </summary>
        /// <param name="key">The key to be inserted</param>
        /// <param name="value">The value to be inserted</param>
        /// <param name="overwrite">Boolean flag indicating if an existing value with the same key should be overwritten</param>
        /// <param name="profiler"></param>
        /// <exception cref="NodeFullException">Raised if this leaf node is currently full</exception>
        /// <exception cref="DuplicateKeyException">Raised if a value already exists for <paramref name="key"/> and <paramref name="overwrite"/> is set to false</exception>
        public void Insert(byte[] key, byte[] value, bool overwrite = false, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("LeafNode.Insert"))
            {
                if (IsFull) throw new NodeFullException();
                int insertIndex = Array.BinarySearch(_keys, 0, _keyCount, key, _config);
                if (insertIndex >= 0)
                {
                    if (overwrite)
                    {
                        Array.Copy(value, 0, _dataSegments[insertIndex], 0, value.Length);
                        return;
                    }
                    throw new DuplicateKeyException();
                }
                using (profiler.Step("Memory Copy"))
                {
                    insertIndex = ~insertIndex;
                    for (int i = _keyCount; i > insertIndex; i--)
                    {
                        _keys[i] = _keys[i - 1];
                        _dataSegments[i] = _dataSegments[i - 1];
                    }
                }
                using (profiler.Step("Insert KVP"))
                {
                    _keys[insertIndex] = new byte[_config.KeySize];
                    Array.Copy(key, _keys[insertIndex], _config.KeySize);
                    _dataSegments[insertIndex] = new byte[_config.ValueSize];
                    if (_config.ValueSize > 0 && value != null)
                    {
                        Array.Copy(value, _dataSegments[insertIndex], value.Length);
                    }
                }
                _keyCount++;
            }
        }

        /// <summary>
        /// Split this leaf node into two
        /// </summary>
        /// <param name="newNodeId">The ID of the page reserved to store the new node</param>
        /// <param name="splitKey">Receives the key that was used for the split</param>
        /// <returns>The new node created by the split</returns>
        /// <remarks>The split operation always creates a new node for the upper (right-hand) half of the keys and keeps the lower (left-hand) half in this leaf node.</remarks>
        public LeafNode Split(ulong newNodeId, out byte[] splitKey)
        {
            var rightNode = new LeafNode(newNodeId, PageId, _nextPointer, _config);
            _nextPointer = newNodeId;
            splitKey = new byte[_config.KeySize];
            Array.Copy(_keys[_config.LeafSplitIndex], splitKey, _config.KeySize);
            Array.Copy(_keys, _config.LeafSplitIndex, rightNode._keys, 0, _keyCount - _config.LeafSplitIndex);
            Array.Copy(_dataSegments, _config.LeafSplitIndex, rightNode._dataSegments, 0,
                       _keyCount - _config.LeafSplitIndex);
            rightNode._keyCount = _keyCount - _config.LeafSplitIndex;
            _keyCount = _config.LeafSplitIndex;
            return rightNode;
        }

        /// <summary>
        /// Retrieves the value associated with the specified key in this leaf node
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <param name="buffer">Receives the value associated with <paramref name="key"/></param>
        /// <returns>True if an entry was found for <paramref name="key"/> in this node, false otherwise</returns>
        public bool GetValue(byte[] key, byte[] buffer)
        {
            int index = Array.BinarySearch(_keys, 0, _keyCount, key, _config);
            if (index >= 0)
            {
                if (_config.ValueSize > 0)
                {
                    Array.Copy(_dataSegments[index], buffer, _dataSegments[index].Length);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes the key-value pair indexed by <paramref name="key"/> from this node
        /// </summary>
        /// <param name="key">The key of the entry to be removed</param>
        public void Delete(byte[] key)
        {
            int deleteIndex = Array.BinarySearch(_keys, 0, _keyCount, key, _config);
            if (deleteIndex >= 0)
            {
                for (int i = deleteIndex + 1; i < _keyCount; i++)
                {
                    _keys[i - 1] = _keys[i];
                    _dataSegments[i - 1] = _dataSegments[i];
                }
                _keyCount--;
            }
        }

        /// <summary>
        /// Attempts to ensure that the minimum size for this node is achieved by transferring entries from the left-hand sibling
        /// </summary>
        /// <param name="leftNode">The left-hand sibling that will provide entries</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        public bool RedistributeFromLeft(LeafNode leftNode)
        {
            int copyCount = (_keyCount + leftNode.KeyCount)/2 - _keyCount;
            if (copyCount > 0)
            {
                // Move down my data segments to make room for the copied ones
                for (int i = _keyCount - 1; i >= 0; i--)
                {
                    _keys[i + copyCount] = _keys[i];
                    _dataSegments[i + copyCount] = _dataSegments[i];
                }
                // Copy keys and data segments from left node
                Array.Copy(leftNode._keys, leftNode._keyCount - copyCount, _keys, 0, copyCount);
                Array.Copy(leftNode._dataSegments, leftNode._keyCount - copyCount, _dataSegments, 0, copyCount);
                _keyCount += copyCount;
                leftNode._keyCount -= copyCount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to ensure that the minimum size for this node is achieved by transferring entries from the right-hand sibling
        /// </summary>
        /// <param name="rightNode">The right-hand sibling that will provide entries</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        public bool RedistributeFromRight(LeafNode rightNode)
        {
            int copyCount = (_keyCount + rightNode._keyCount)/2 - _keyCount;
            if (copyCount > 0)
            {
                // Copy keys and data segments from right node
                Array.Copy(rightNode._keys, 0, _keys, _keyCount, copyCount);
                Array.Copy(rightNode._dataSegments, 0, _dataSegments, _keyCount, copyCount);
                // Shift up the remaining keys and data segments in the right node
                for (int i = 0; i < rightNode._keyCount - copyCount; i++)
                {
                    rightNode._keys[i] = rightNode._keys[i + copyCount];
                    rightNode._dataSegments[i] = rightNode._dataSegments[i + copyCount];
                }
                rightNode._keyCount -= copyCount;
                _keyCount += copyCount;
                return true;
            }
            return false;
        }

        public IEnumerable<KeyValuePair<byte[], byte []>> Scan()
        {
            for(int i = 0 ; i < _keyCount;i++)
            {
                yield return new KeyValuePair<byte[], byte[]>(_keys[i], _dataSegments[i]);
            }
        }

    }
}