using System.Collections.Generic;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class BPlusTreeConfiguration : IComparer<byte[]>
    {
        /// <summary>
        /// The size of key in bytes
        /// </summary>
        public int KeySize { get; private set; }
        
        /// <summary>
        /// The size of value stored in leaf nodes in bytes
        /// </summary>
        public int ValueSize { get; private set; }

        /// <summary>
        /// The size of the storage page in bytes
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// The maximum number of keys per internal node
        /// </summary>
        public int InternalBranchFactor { get; private set; }

        /// <summary>
        /// The minimum number of keys per internal node, and also the index where a split occurs for full nodes
        /// </summary>
        public int InternalSplitIndex { get; private set; }

        /// <summary>
        /// The number of key/value pairs per leaf node
        /// </summary>
        public int LeafLoadFactor { get; private set; }

        /// <summary>
        /// The minimum number of keys per leaf node and also the index where a split occurs for full leaf nodes
        /// </summary>
        public int LeafSplitIndex { get; private set; }
        /// <summary>
        /// The number of bytes reserved in each internal node for the key count
        /// </summary>
        public const int InternalNodeHeaderSize = 4;

        /// <summary>
        /// The number of byte required to store each child node pointer (and prev/next pointers for leaf nodes)
        /// </summary>
        public const int NodePointerSize = 8;

        /// <summary>
        /// The number of bytes reserved in each leaf node for count and previous and next pointers
        /// </summary>
        public const int LeafNodeHeaderSize = 4 + 2 * NodePointerSize;

        public int LeafDataStartOffset { get; private set; }

        public int InternalNodeChildStartOffset { get; private set; }

        /// <summary>
        /// The maximum number of bytes that can be reserved in a leaf node for a value
        /// </summary>
        public const int MaxValueSize = 255;

        /// <summary>
        /// Get the IPageStore instance that manages the backing pages for the tree
        /// </summary>
        public IPageStore PageStore { get; private set; }

        public BPlusTreeConfiguration(IPageStore pageStore, int keySize, int valueSize, int pageSize)
        {
            PageStore = pageStore;
            KeySize = keySize;
            ValueSize = valueSize;
            PageSize = pageSize;
            InternalBranchFactor = (PageSize - InternalNodeHeaderSize - NodePointerSize)/(KeySize + NodePointerSize);
            InternalSplitIndex = InternalBranchFactor/2;
            InternalNodeChildStartOffset = InternalNodeHeaderSize + (InternalBranchFactor*KeySize);
            LeafLoadFactor = (PageSize - LeafNodeHeaderSize)/(KeySize + ValueSize + 1); // Each value requires ValueSize+1 bytes, extra byte is for the value length
            LeafSplitIndex = LeafLoadFactor/2;
            LeafDataStartOffset = LeafNodeHeaderSize + (KeySize*LeafLoadFactor);
        }

        #region Implementation of IComparer<in byte[]>

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <returns>
        /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        /// <param name="x">The first object to compare.</param><param name="y">The second object to compare.</param>
        public int Compare(byte[] x, byte[] y)
        {
            return x.Compare(y);
        }

        #endregion
    }
}
