using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class LeafNode : ILeafNode
    {
        private IPage _page;
        private readonly BPlusTreeConfiguration _config;
        private int _keyCount;
        private ulong _prevPointer;
        private ulong _nextPointer;

        /// <summary>
        /// Creates a leaf node that will be backed by a new page
        /// </summary>
        /// <param name="reservedPage">The backing page for this node. Must be writeable</param>
        /// <param name="prevPointer">UNUSED</param>
        /// <param name="nextPointer">UNUSED</param>
        /// <param name="treeConfiguration">The tree configuration parameters</param>
        public LeafNode(IPage reservedPage, ulong prevPointer, ulong nextPointer,
                              BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            AssertWriteable(reservedPage);
            _page = reservedPage;
            KeyCount = 0;
            Prev = prevPointer;
            Next = nextPointer;
#if DEBUG_BTREE
            _config.BTreeDebug("+Leaf1 {0}", _page.Id);
#endif
        }

        public LeafNode(IPage page, int keyCount, BPlusTreeConfiguration treeConfiguration)
        {
            _page = page;
            _keyCount = keyCount;
            _prevPointer = BitConverter.ToUInt64(_page.Data, 4);
            _nextPointer = BitConverter.ToUInt64(_page.Data, 12);
            _config = treeConfiguration;
#if DEBUG_BTREE
            _config.BTreeDebug("+Leaf2 {0}", _page.Id);
#endif
        }

        public LeafNode(IPage page, ulong prevPointer, ulong nextPointer,
                              BPlusTreeConfiguration treeConfiguration,
                              IEnumerable<KeyValuePair<byte[], byte[]>> orderedValues, int numValuesToLoad)
        {
            _page = page;
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
#if DEBUG_BTREE
            _config.BTreeDebug("+Leaf3 {0}", _page.Id);
#endif
        }

        public ulong Prev
        {
            get { return _prevPointer; }
            private set
            {
                _prevPointer = value;
                _page.SetData(BitConverter.GetBytes(_prevPointer), 0, 4, 8);
            }
        }



        public bool NeedsJoin { get { return KeyCount < _config.LeafSplitIndex; } }

        public ulong Next
        {
            get { return _nextPointer; }
            private set
            {
                _nextPointer = value;
                _page.SetData(BitConverter.GetBytes(_nextPointer), 0, 12, 8);
            }
        }

        public int KeyCount
        {
            get { return _keyCount; }
            private set
            {
                _keyCount = value;
                _page.SetData(BitConverter.GetBytes(_keyCount), 0, 0, 4);
            }
        }

        #region ILeafNode methods

        public bool Merge(ulong txnId, INode s)
        {
            var sibling = s as LeafNode;
            if (sibling == null)
            {
                throw new ArgumentException("Merge node is null or not a DirectLeafNode", "s");
            }

            if (sibling.KeyCount + KeyCount <= _config.LeafLoadFactor)
            {
                EnsureWriteable(txnId);
                if (sibling.LeftmostKey.Compare(RightmostKey) > 0)
                {
                    // Append all of the siblings entries
                    _page.SetData(sibling.GetData(), KeyOffset(0),
                               KeyOffset(KeyCount),
                               sibling.KeyCount*_config.KeySize);
                    if (_config.ValueSize > 0)
                    {
                        _page.SetData(sibling.GetData(), ValueOffset(0),
                                   ValueOffset(KeyCount),
                                   sibling.KeyCount*_config.ValueSize);
                    }
                }
                else
                {
                    // Prepend all of the sibling entries
                    RightShift(sibling.KeyCount);
                    _page.SetData(sibling.GetData(), KeyOffset(0),
                               KeyOffset(0),
                               sibling.KeyCount*_config.KeySize);
                    _page.SetData(sibling.GetData(), ValueOffset(0),
                               ValueOffset(0),
                               sibling.KeyCount*_config.ValueSize);
                }
                KeyCount += sibling.KeyCount;
                return true;
            }
            return false;
        }

        public void Insert(ulong txnId, byte[] key, byte[] value, bool overwrite = false, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("LeafNode.Insert"))
            {
                if (IsFull) throw new NodeFullException();
                int insertIndex = Search(key);
                if (insertIndex >= 0)
                {
                    if (overwrite)
                    {
                        EnsureWriteable(txnId);
                        _page.SetData(value, 0, 
                            ValueOffset(insertIndex),
                            Math.Min(value.Length, _config.ValueSize));
                        return;
                    }
                    throw new DuplicateKeyException();
                }
                EnsureWriteable(txnId);
                insertIndex = ~insertIndex;
                RightShiftFrom(insertIndex, 1);
                _page.SetData(key, 0, KeyOffset(insertIndex), _config.KeySize);
                if (_config.ValueSize > 0 && value != null)
                {
                    _page.SetData(value, 0, ValueOffset(insertIndex), Math.Min(_config.ValueSize, value.Length));
                }
                KeyCount++;
#if DEBUG_BTREE
_config.BTreeDebug("LeafNode.Insert@{0}. Key={1}. Updated Node: {2}",PageId, key.Dump(), DumpKeys());
#endif
            }
        }

        public ILeafNode Split(ulong txnId, IPage rightNodePage, out byte[] splitKey)
        {
            EnsureWriteable(txnId);
            Next = rightNodePage.Id;
            splitKey = new byte[_config.KeySize];
            int numToMove = KeyCount - _config.LeafSplitIndex;
            Array.Copy(_page.Data, KeyOffset(_config.LeafSplitIndex), splitKey, 0, _config.KeySize);
#if DEBUG_BTREE
            _config.BTreeDebug("LeafNode.Split. SplitKey={0}. NumToMove={1}. Structure Before: {2}", splitKey.Dump(), numToMove, Dump());
            _config.BTreeDebug("LeafNode.Split@{0}. Keys before: {1}", PageId, DumpKeys());
#endif
            rightNodePage.SetData(_page.Data, KeyOffset(_config.LeafSplitIndex),
                                  KeyOffset(0), numToMove*_config.KeySize);
            if (_config.ValueSize > 0)
            {
                rightNodePage.SetData(_page.Data, ValueOffset(_config.LeafSplitIndex),
                                      ValueOffset(0), numToMove*_config.ValueSize);
            }
            var rightNodeKeyCount = numToMove;
            rightNodePage.SetData(BitConverter.GetBytes(rightNodeKeyCount), 0, 0, 4);
            var rightNode = new LeafNode(rightNodePage, rightNodeKeyCount, _config);
            KeyCount = _config.LeafSplitIndex;
#if DEBUG_BTREE
_config.BTreeDebug("LeafNode.Split. Structure After : {0}. Right Node After: {1}",
    Dump(), rightNode.Dump());
_config.BTreeDebug("LeafNode.Split@{0}. Keys after: {1}", PageId, DumpKeys());
#endif
            return rightNode;
        }

        public bool GetValue(byte[] key, byte[] buffer)
        {
            int index = Search(key);
            if (index >= 0)
            {
                if (_config.ValueSize > 0)
                {
                    Array.Copy(_page.Data, ValueOffset(index), buffer, 0, _config.ValueSize);
                }
                return true;
            }
            return false;
        }

        public void Delete(ulong txnId, byte[] key)
        {
            int deleteIndex = Search(key);
            if (deleteIndex >= 0)
            {
                EnsureWriteable(txnId);
                int moveUp = KeyCount - (deleteIndex + 1);
                if (moveUp > 0)
                {
                    Array.Copy(_page.Data, KeyOffset(deleteIndex + 1),
                               _page.Data, KeyOffset(deleteIndex),
                               moveUp*_config.KeySize);
                    Array.Copy(_page.Data, ValueOffset(deleteIndex + 1),
                               _page.Data, ValueOffset(deleteIndex),
                               moveUp*_config.ValueSize);
                }
                KeyCount--;
            }
        }

        public bool RedistributeFromLeft(ulong txnId, ILeafNode leftNode)
        {
            var left = leftNode as LeafNode;
            if (left == null) throw new ArgumentException("Expected a LeafNode instance", "leftNode");

            int copyCount = (KeyCount + left.KeyCount)/2 - KeyCount;
            if (copyCount > 0)
            {
                EnsureWriteable(txnId);
                RightShift(copyCount);
                // Copy keys and data from left node
                int keyOffset = KeyOffset(left.KeyCount - copyCount);
                _page.SetData(left.GetData(), keyOffset, KeyOffset(0), copyCount*_config.KeySize);
                if (_config.ValueSize > 0)
                {
                    int valueOffset = ValueOffset(left.KeyCount - copyCount);
                    _page.SetData(left.GetData(), valueOffset, ValueOffset(0), copyCount*_config.ValueSize);
                }
                KeyCount += copyCount;
                left.KeyCount -= copyCount;
                return true;
            }
            return false;
        }


        public bool RedistributeFromRight(ulong txnId, ILeafNode rightNode)
        {
            var right = rightNode as LeafNode;
            if (right == null) throw new ArgumentException("Expected a LeafNode instance", "rightNode");

            int copyCount = (KeyCount + rightNode.KeyCount)/2 - KeyCount;
            if (copyCount > 0)
            {
                EnsureWriteable(txnId);
                // Copy keys and data from right
                _page.SetData(right.GetData(), BPlusTreeConfiguration.LeafNodeHeaderSize,
                              KeyOffset(KeyCount), copyCount*_config.KeySize);
                if (_config.ValueSize > 0)
                {
                    _page.SetData(right.GetData(), _config.LeafDataStartOffset,
                        ValueOffset(KeyCount), copyCount * _config.ValueSize);
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
            for (startOffset = BPlusTreeConfiguration.LeafNodeHeaderSize, startIx = 0; startOffset < endIx && _page.Data.Compare(startOffset, fromKey, 0, _config.KeySize) < 0; startOffset+=_config.KeySize, startIx++ ){}
            for (offset = startOffset, ix = startIx;
                 offset < endIx && (_page.Data.Compare(offset, toKey, 0, _config.KeySize) <= 0);
                 offset += _config.KeySize, ix++)
            {
                yield return new KeyValuePair<byte[], byte[]>(GetKey(ix), GetValue(ix));
            }
        }

        #endregion

        #region Implementation of INode

        public ulong PageId
        {
            get { return _page.Id; }
        }

        public bool IsDirty { get { return _page.IsDirty; } }
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
            return _page.Data;
        }

        public void DumpStructure(BPlusTree tree, int indentLevel)
        {
            if (KeyCount == 0)
            {
                Console.WriteLine("{0}EMPTY LEAF NODE",
                    new string(' ', indentLevel));
                return;
            }
            Console.WriteLine("{0}LEAF@{1}[{2} keys: {3} - {4}]",
                new string(' ', indentLevel*4), PageId, KeyCount, LeftmostKey.Dump(), RightmostKey.Dump());
        }

        public string Dump()
        {
            if (KeyCount == 0)
            {
                return "EMPTY LEAF NODE";
            }
            return String.Format("LEAF@{0}[{1} keys: {2} - {3}]",
                                 PageId, KeyCount, LeftmostKey.Dump(),
                                 RightmostKey.Dump());
        }

        private string DumpKeys()
        {
            if (KeyCount == 0)
            {
                return "[]";
            }
            var ret = new StringBuilder();
            for (int i = 0; i < KeyCount; i++)
            {
                ret.AppendFormat("[{0}]=>{1}", i, GetKey(i).Dump());
            }
            return ret.ToString();
        }

        #endregion

        /// <summary>
        /// Remove <paramref name="count"/> entries from the left of this node and move remaining entries up
        /// </summary>
        /// <param name="count"></param>
        private void LeftShift(int count)
        {
            int remaining = KeyCount - count;
            _page.SetData(_page.Data, BPlusTreeConfiguration.LeafNodeHeaderSize + (count*_config.KeySize),
                          BPlusTreeConfiguration.LeafNodeHeaderSize, remaining*_config.KeySize);
            _page.SetData(_page.Data, _config.LeafDataStartOffset + (count*_config.ValueSize),
                          _config.LeafDataStartOffset, remaining*_config.ValueSize);
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
                    _page.SetData(_page.Data, keyOffset, keyOffset + keyShift, _config.KeySize);
                    _page.SetData(_page.Data, valueOffset, valueOffset + valueShift, _config.ValueSize);
                }
            }
            else
            {
                for (i = KeyCount - 1,
                     keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize + ((KeyCount - 1) * _config.KeySize);
                     i >= 0;
                     i--, keyOffset -= _config.KeySize)
                {
                    _page.SetData(_page.Data, keyOffset, keyOffset + keyShift, _config.KeySize);
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
                    _page.SetData(_page.Data, keyOffset, keyOffset + keyShift, _config.KeySize);
                    _page.SetData(_page.Data, valueOffset, valueOffset + valueShift, _config.ValueSize);
                }
            }
            else
            {
                for (i = KeyCount - 1,
                     keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize + ((KeyCount - 1) * _config.KeySize);
                     i >= ix;
                     i--, keyOffset -= _config.KeySize)
                {
                    _page.SetData(_page.Data, keyOffset, keyOffset + keyShift, _config.KeySize);
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
            Array.Copy(_page.Data, offset, buff, 0, _config.KeySize);
        }

        /// <summary>
        /// Writes the key at offset <paramref name="ix"/> from the provided buffer
        /// </summary>
        /// <param name="ix">The offset of the key to be written</param>
        /// <param name="buff">The value to be written for the key</param>
        private void SetKey(int ix, byte[] buff)
        {
            var offset = BPlusTreeConfiguration.LeafNodeHeaderSize + (_config.KeySize * ix);
            _page.SetData(buff, 0, offset, _config.KeySize);
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
            Array.Copy(_page.Data, offset, buff, 0, _config.ValueSize);
        }


        /// <summary>
        /// Writes the value at offset <paramref name="ix"/> from the provided buffer
        /// </summary>
        /// <param name="ix">The offset of the value to be written</param>
        /// <param name="buff">The new value to write</param>
        private void SetValue(int ix, byte[] buff)
        {
            var offset = _config.LeafDataStartOffset + (_config.ValueSize*ix);
            _page.SetData(buff, 0, offset, _config.ValueSize);
        }

        private int Search(byte[] key)
        {
            // TODO: replace with a binary search algorithm
            for (int i = 0, keyOffset = BPlusTreeConfiguration.LeafNodeHeaderSize;
                 i < KeyCount;
                 i++,keyOffset += _config.KeySize)
            {
                var cmp = key.Compare(0, _page.Data, keyOffset, _config.KeySize);
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

        private void EnsureWriteable(ulong txnId)
        {
            if (!_config.PageStore.IsWriteable(_page))
            {
                _page = _config.PageStore.GetWriteablePage(txnId, _page);
            }
        }

        private void AssertWriteable(IPage page)
        {
            if (!_config.PageStore.IsWriteable(page))
            {
                throw new InvalidOperationException(Strings.INode_Attempt_to_write_to_a_fixed_page);
            }
        }
    }
}
