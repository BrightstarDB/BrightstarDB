using System;
using System.Collections.Generic;
using System.Text;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;
using VDS.RDF;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class InternalNode : IInternalNode
    {

        private readonly BPlusTreeConfiguration _config;
        private int _keyCount;
        private IPage _page;

        /// <summary>
        /// Creates a new empty node backed by the specified persistant storage page
        /// </summary>
        /// <param name="page">The page to be used to back the node</param>
        /// <param name="treeConfig">The tree configuration parameters</param>
        private InternalNode(IPage page,  BPlusTreeConfiguration treeConfig)
        {
            _page = page;
            _config = treeConfig;
            _keyCount = 0;
        }

        /// <summary>
        /// Initializes a new node using the data contained in the specified persisten storage page
        /// </summary>
        /// <param name="page">The page used to back the node</param>
        /// <param name="keyCount">The number of keys in the node (as read from page data)</param>
        /// <param name="treeConfig">The tree configuration parameters</param>
        /// <remarks>Strictly speaking we don't have to pass the key count because it can be read from
        /// the page data. However, the code that invokes this constructor needs to check the key
        /// count value to see if a node contains a leaf or an internal node and having this constructor
        /// separate from one that specifies that the page is empty initially is also convenient</remarks>
        public InternalNode(IPage page, int keyCount, BPlusTreeConfiguration treeConfig)
        {
            _config = treeConfig;
            _page = page;
            _keyCount = keyCount;
#if DEBUG_BTREE
            treeConfig.BTreeDebug("+Internal {0}", page.Id);
#endif
        }

        /// <summary>
        /// Creates a new node backed by the specified persistent storage page with a single
        /// key and left and right pointers.
        /// </summary>
        /// <param name="page">The page used to back the node. Must be writeable.</param>
        /// <param name="splitKey">The key that separates left and right child pointers</param>
        /// <param name="leftPageId">The left child node pointer</param>
        /// <param name="rightPageId">The right child node pointer</param>
        /// <param name="treeConfiguration">The tree configuration</param>
        /// <exception cref="InvalidOperationException">Raised if <paramref name="page"/> is not a writeable page</exception>
        public InternalNode(IPage page, byte[] splitKey, ulong leftPageId, ulong rightPageId,
                                  BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            AssertWriteable(page);
            _page = page;
            _page.SetData(splitKey, 0, KeyOffset(0), _config.KeySize);
            _page.SetData(BitConverter.GetBytes(leftPageId), 0, PointerOffset(0), 8);
            _page.SetData(BitConverter.GetBytes(rightPageId), 0, PointerOffset(1), 8);
            KeyCount = 1;
#if DEBUG_BTREE
            _config.BTreeDebug("+Internal {0}", _page.Id);
#endif
        }

        /// <summary>
        /// Creates a new node backed by the specified persistent storage page that has only
        /// a single child node pointer
        /// </summary>
        /// <param name="page">The page used to back the node. Must be writeable.</param>
        /// <param name="onlyChild">The child node pointer</param>
        /// <param name="treeConfiguration">The tree configuration</param>
        /// <exception cref="InvalidOperationException">Raised if <paramref name="page"/> is not a writeable page</exception>
        public InternalNode(IPage page, ulong onlyChild, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            AssertWriteable(page);
            _page = page;
            _page.SetData(BitConverter.GetBytes(onlyChild), 0, PointerOffset(0), 8);
            KeyCount = 0;
#if DEBUG_BTREE
            _config.BTreeDebug("+Internal {0}", _page.Id);
#endif
        }

        /// <summary>
        /// Creates a new node backed by the specified persistent storage page 
        /// containing a list of keys and child values
        /// </summary>
        /// <param name="page">The page used to back the node. Must be writeable</param>
        /// <param name="keys">The list of keys for the node</param>
        /// <param name="values">The list of values for the node</param>
        /// <param name="treeConfiguration">The tree configuration</param>
        public InternalNode(IPage page, List<byte[]> keys, List<ulong> values, BPlusTreeConfiguration treeConfiguration)
        {
            _config = treeConfiguration;
            AssertWriteable(page);
            _page = page;
            KeyCount = keys.Count;
            int i, keyOffset, pointerOffset;
            for (i = 0, keyOffset = KeyOffset(0); i < keys.Count; i++, keyOffset+=_config.KeySize)
            {
                _page.SetData(keys[i], 0, keyOffset, _config.KeySize);
            }
            for (i = 0, pointerOffset = PointerOffset(0); i < KeyCount + 1; i++, pointerOffset += 8)
            {
                _page.SetData(BitConverter.GetBytes(values[i]), 0, pointerOffset, 8);
            }
#if DEBUG_BTREE
            _config.BTreeDebug("+Internal {0}", _page.Id);
#endif
        }

        public ulong PageId { get { return _page.Id; } }

        public bool IsDirty { get { return _page.IsDirty; } }

        public bool IsLeaf { get { return false; } }

        public bool IsFull { get { return _keyCount == _config.InternalBranchFactor; } }

        public byte[] RightmostKey { get { return GetKey(_keyCount - 1); } }
        public byte[] LeftmostKey { get { return GetKey(0); } }

        public byte[] GetData() { return _page.Data; }

        public int KeyCount
        {
            get { return _keyCount; }
            private set
            {
                _keyCount = value;
                _page.SetData(BitConverter.GetBytes(~_keyCount), 0, 0, 4);
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

        public string Dump()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("InternalNode @{0}:", PageId);
            for (int i = 0; i < _keyCount; i++)
            {
                var childPointer = GetPointer(i);
                sb.AppendFormat("\n PTR[{0}]: {1}", i, childPointer);
                sb.AppendFormat("\nKEY[{0}]: {1}", i, GetKey(i).Dump());
            }
            var lastPointer = GetPointer(KeyCount);
            sb.AppendFormat("\n PTR[{0}]: {1}", _keyCount, lastPointer);
            return sb.ToString();
        }

        public bool NeedJoin { get { return KeyCount < _config.InternalSplitIndex; } }

        public bool Merge(ulong txnId, INode s, byte[] joinKey)
        {
            var sibling = s as InternalNode;
            if (sibling == null)
            {
                throw new ArgumentException("Merge node is null or not a DirectInternalNode", "s");
            }
            if (sibling.KeyCount + KeyCount < _config.InternalBranchFactor)
            {
                if (sibling.LeftmostKey.Compare(RightmostKey) > 0)
                {
                    EnsureWriteable(txnId);
                    // Append join key followed by all of sibling's entries
                    _page.SetData(joinKey, 0, KeyOffset(KeyCount), _config.KeySize);
                    _page.SetData(sibling.GetData(), KeyOffset(0),
                                  KeyOffset(KeyCount + 1),
                                  sibling.KeyCount*_config.KeySize);
                    _page.SetData(sibling.GetData(), PointerOffset(0),
                                  PointerOffset(KeyCount + 1),
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
                if (key.Compare(0, _page.Data, offset, _config.KeySize) < 0)
                {
                    return GetPointer(i);
                }
            }
            return GetPointer(KeyCount);
        }

        /// <summary>
        /// Splits this node into two internal nodes, creating a new node for the right-hand (upper) half of the keys
        /// </summary>
        /// <param name="txnId"></param>
        /// <param name="rightNodePage">The new page reserved to receive the newly created internal node</param>
        /// <param name="splitKey">Receives the value of the key used for the split</param>
        /// <returns>The new right-hand node</returns>
        public IInternalNode Split(ulong txnId, IPage rightNodePage, out byte[] splitKey)
        {
#if DEBUG_BTREE
            _config.BTreeDebug("InternalNode.Split. Id={0}. Structure Before: {1}", PageId, Dump());
#endif
            EnsureWriteable(txnId);
            var splitIndex = _config.InternalSplitIndex;
            splitKey = GetKey(splitIndex);
            rightNodePage.SetData(_page.Data, KeyOffset(splitIndex + 1),
                                  KeyOffset(0),
                                  (KeyCount - (splitIndex + 1))*_config.KeySize);
            var pointerCopyStart = PointerOffset(splitIndex + 1);
            var pointerCopyLength = (KeyCount - splitIndex)*8;
            rightNodePage.SetData(_page.Data, pointerCopyStart,
                                  PointerOffset(0),
                                  pointerCopyLength);
            var rightNodeKeyCount = KeyCount - (splitIndex + 1);
            rightNodePage.SetData(BitConverter.GetBytes(~rightNodeKeyCount), 0, 0, 4);
            var rightNode = new InternalNode(rightNodePage, rightNodeKeyCount, _config);
            KeyCount = splitIndex;
#if DEBUG_BTREE
            _config.BTreeDebug("InternalNode.Split. Structure After: Id={0} {1}\nRight Node After: Id={2} {3}",
                PageId, Dump(), rightNode.PageId, rightNode.Dump());
#endif
            return rightNode;
        }

        public void Insert(ulong txnId, byte[] key, ulong childPointer)
        {
#if DEBUG_BTREE
            _config.BTreeDebug("InternalNode.Insert. Key={0}. Before: Id={1} {2}",
                key.Dump(), PageId, Dump());
#endif
            if (_keyCount == _config.InternalBranchFactor)
            {
                throw new NodeFullException();
            }

            var insertIndex = Search(key);
            if (insertIndex >= 0)
            {
                throw new DuplicateKeyException();
            }

            EnsureWriteable(txnId);
            insertIndex = ~insertIndex;
            RightShiftFrom(insertIndex, 1);
            _page.SetData(key, 0,
                          KeyOffset(insertIndex), _config.KeySize);
            _page.SetData(BitConverter.GetBytes(childPointer), 0,
                          PointerOffset(insertIndex + 1), 8);
            KeyCount++;
#if DEBUG_BTREE
            _config.BTreeDebug("InternalNode.Insert. Key={0}. After: Id={1} {2}",
                key.Dump(), PageId, Dump());
#endif
        }

        /// <summary>
        /// Updates the node ID pointed to be a child pointer
        /// </summary>
        /// <param name="txnId"></param>
        /// <param name="oldChildPointer">The old child node ID to be replaced</param>
        /// <param name="newChildPointer">The new child node ID</param>
        public void UpdateChildPointer(ulong txnId, ulong oldChildPointer, ulong newChildPointer)
        {
#if DEBUG_BTREE
            _config.BTreeDebug("InternalNode.UpdateChildPointer. Old={0}, New={1}. Before: Id={2} {3}",
                oldChildPointer, newChildPointer, PageId, Dump());
#endif
            if (oldChildPointer == newChildPointer) return;
            EnsureWriteable(txnId);
            int i, pointerOffset;
            byte[] ocp = BitConverter.GetBytes(oldChildPointer);
            byte[] ncp = BitConverter.GetBytes(newChildPointer);
            for (i = 0, pointerOffset = PointerOffset(0); i <= KeyCount; i++, pointerOffset += 8)
            {
                if (_page.Data.Compare(pointerOffset, ocp, 0, 8) == 0)
                {
                    _page.SetData(ncp, 0, pointerOffset, 8);
#if DEBUG_BTREE
                    _config.BTreeDebug("InternalNode.UpdateChildPointer. Old={0}, New={1}. After: Id={2} {3}",
                        oldChildPointer, newChildPointer, PageId, Dump());
#endif
                    return;
                }
            }
#if DEBUG_BTREE
            _config.BTreeDebug("InternalNode.UpdateChildPointer. Old={0}, New={1}. No match found for old child pointer.",
                oldChildPointer, newChildPointer);
#endif
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
                if (_page.Data.Compare(pointerOffset, cni, 0, 8) == 0)
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
            if (_page.Data.Compare(PointerOffset(0), cni, 0, 8) == 0)
            {
                // First child has no left sibling
                leftSiblingId = 0;
                return false;
            }

            
            for (i = 1, pointerOffset=PointerOffset(1); i <= KeyCount; i++, pointerOffset += 8)
            {
                if (_page.Data.Compare(pointerOffset, cni, 0, 8) == 0)
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
        /// <param name="txnId"></param>
        /// <param name="leftSibling">The left-hand sibling that will provide entries</param>
        /// <param name="joinKey">The value of the key that is present in the parent node between the pointer to this node and its left sibling</param>
        /// <param name="newJoinKey">The replacement value for the join key in the parent node</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        public bool RedistributeFromLeft(ulong txnId, IInternalNode leftSibling, byte[] joinKey, byte[] newJoinKey)
        {
            InternalNode left = leftSibling as InternalNode;
            if (left == null)
            {
                throw new ArgumentException("Expected a InternalNode as left sibling", "leftSibling");
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

            EnsureWriteable(txnId);
            left.EnsureWriteable(txnId);

            // Make space for new keys and child pointers
            RightShift(required);

            _page.SetData(joinKey, 0, KeyOffset(required - 1), _config.KeySize);
            _page.SetData(left.GetData(), KeyOffset(left.KeyCount - (required - 1)),
                          KeyOffset(0), (required - 1)*_config.KeySize);
            _page.SetData(left.GetData(), PointerOffset(left.KeyCount - (required - 1)),
                          PointerOffset(0), (required)*8);
            Array.Copy(left.GetData(), KeyOffset(left.KeyCount-required), newJoinKey, 0, _config.KeySize);
            KeyCount += required;
            left.KeyCount -= required;
            return true;
        }

        public bool RedistributeFromRight(ulong txnId, IInternalNode rightSibling, byte[] joinKey, byte[] newJoinKey)
        {
            var right = rightSibling as InternalNode;
            if (right == null) throw new ArgumentException("Expected a DirectInternalNode as right sibling", "rightSibling");

            int required = _config.InternalSplitIndex - _keyCount;
            if (rightSibling.KeyCount - required < _config.InternalSplitIndex)
            {
                return false;
            }

            EnsureWriteable(txnId);
            right.EnsureWriteable(txnId);

            // Copy keys and child pointers
            _page.SetData(joinKey, 0, KeyOffset(KeyCount), _config.KeySize); // Set key[KeyCount+1] to joinKey
            _page.SetData(right.GetData(), KeyOffset(0),
                          KeyOffset(KeyCount + 1), (required - 1)*_config.KeySize);
            _page.SetData(right.GetData(), PointerOffset(0),
                          PointerOffset(KeyCount + 1), required*8);
            Array.Copy(right.GetData(), KeyOffset(required - 1),
                       newJoinKey, 0, _config.KeySize);
            right.LeftShift(required);
            KeyCount += required;
            return true;
        }

        public void SetLeftKey(ulong txnId, ulong childNodeId, byte[] childNodeKey)
        {
            if (childNodeKey == null)
            {
                throw new ArgumentNullException("childNodeKey");
            }

            byte[] cni = BitConverter.GetBytes(childNodeId);
            if (_page.Data.Compare(PointerOffset(0), cni, 0, 8) == 0)
            {
                // First pointer doesn't have a corresponding key to update
                return;
            }
            int i, pointerOffset;
            for (i = 1, pointerOffset = PointerOffset(1); i <= KeyCount; i++, pointerOffset += 8)
            {
                if (_page.Data.Compare(pointerOffset, cni, 0, 8) == 0)
                {
                    EnsureWriteable(txnId);
                    _page.SetData(childNodeKey, 0, KeyOffset(i - 1), _config.KeySize);
                }
            }
        }

        public byte[] GetKey(ulong childNodeId)
        {
            var ix = Search(childNodeId);
            if (ix >= 0) return GetKey(ix);
            return null;
        }

        public void SetKey(ulong txnId, ulong childNodeId, byte[] newKey)
        {
            var ix = Search(childNodeId);
            if (ix < 0) throw new ArgumentException("Cannot find child node " + childNodeId, "childNodeId");
            EnsureWriteable(txnId);
            _page.SetData(newKey, 0, KeyOffset(ix), _config.KeySize);
        }

        public byte[] RemoveChildPointer(ulong txnId, ulong childNodeId)
        {
            var pointerIndex = Search(childNodeId);
            if (pointerIndex < 0)
            {
                    throw new ArgumentException("Cannot find child node " + childNodeId, "childNodeId");
            }
            EnsureWriteable(txnId);
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
                if (fromKey.Compare(0, _page.Data, keyOffset, _config.KeySize) < 0)
                {
                    yield return GetPointer(i);
                    if (toKey.Compare(0, _page.Data, keyOffset, _config.KeySize) < 0)
                    {
                        yield break;
                    }
                }
                if (toKey.Compare(0, _page.Data, keyOffset, _config.KeySize) < 0)
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
            Array.Copy(_page.Data, KeyOffset(keyIx), buff, 0, _config.KeySize);
        }

        private ulong GetPointer(int pointerIx)
        {
            byte[] buff = new byte[8];
            GetPointer(pointerIx, buff);
            return BitConverter.ToUInt64(buff, 0);
        }

        private void GetPointer(int pointerIx, byte[] buff)
        {
            Array.Copy(_page.Data, PointerOffset(pointerIx), buff, 0, 8);
        }

        private int Search(byte[] key)
        {
            // TODO: replace with a binary search algorithm
            for (int i = 0, keyOffset = KeyOffset(0);
                 i < KeyCount;
                 i++, keyOffset += _config.KeySize)
            {
                var cmp = key.Compare(0, _page.Data, keyOffset, _config.KeySize);
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
                var cmp = _page.Data.Compare(pointerOffset, p, 0, 8);
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
            _page.SetData(_page.Data, lastPointerOffset, lastPointerOffset+pointerShift, 8);
            for (i = KeyCount - 1,
                 keyOffset = KeyOffset(KeyCount - 1),
                 pointerOffset = PointerOffset(KeyCount - 1);
                 i >= ix;
                 i--, keyOffset -= _config.KeySize, pointerOffset -= 8)
            {
                _page.SetData(_page.Data, keyOffset, keyOffset + keyShift, _config.KeySize);
                _page.SetData(_page.Data, pointerOffset, pointerOffset + pointerShift, 8);
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
            _page.SetData(_page.Data, KeyOffset(ix + numPlaces),
                          KeyOffset(ix), (KeyCount - ix - numPlaces)*_config.KeySize);
            _page.SetData(_page.Data, PointerOffset(ix + numPlaces),
                          PointerOffset(ix), (KeyCount - ix - numPlaces + 1)*8);
            KeyCount -= numPlaces;
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
