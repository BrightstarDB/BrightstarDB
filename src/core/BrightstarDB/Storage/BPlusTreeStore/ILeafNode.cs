using System.Collections.Generic;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal interface ILeafNode : INode
    {
        /// <summary>
        /// Returns an enumeration of all key-value pairs in this leaf node that fall in the range <paramref name="fromKey"/> to <paramref name="toKey"/> (inclusive)
        /// </summary>
        /// <param name="fromKey">The lowest key to return in the enumeration</param>
        /// <param name="toKey">The highest key to return in the enumeration</param>
        /// <returns>An enumeration of key value pairs</returns>
        IEnumerable<KeyValuePair<byte[], byte[]>> Scan(byte[] fromKey, byte[] toKey);

        /// <summary>
        /// Boolean flag indicating if the number of elements in this leaf node has fallen below the minimum allowed
        /// </summary>
        bool NeedsJoin { get; }

        /// <summary>
        /// Get the ID of the next leaf node in the btree
        /// </summary>
        ulong Next { get; }

        /// <summary>
        /// Get the ID of the previous leaf node in the btree
        /// </summary>
        ulong Prev { get; }

        
        /// <summary>
        /// Attempt to merge this node with the specified sibling node
        /// </summary>
        /// <param name="s">The sibling to merge with</param>
        /// <returns>True if the merge completed successfully, false otherwise</returns>
        bool Merge(INode s);


        /// <summary>
        /// Inserts a key-value pair into this leaf node
        /// </summary>
        /// <param name="key">The key to be inserted</param>
        /// <param name="value">The value to be inserted</param>
        /// <param name="overwrite">Boolean flag indicating if an existing value with the same key should be overwritten</param>
        /// <param name="profiler"></param>
        /// <exception cref="NodeFullException">Raised if this leaf node is currently full</exception>
        /// <exception cref="DuplicateKeyException">Raised if a value already exists for <paramref name="key"/> and <paramref name="overwrite"/> is set to false</exception>
        void Insert(byte[] key, byte[] value, bool overwrite = false, BrightstarProfiler profiler = null);

        /// <summary>
        /// Split this leaf node into two
        /// </summary>
        /// <param name="newNodeId">The ID of the page reserved to store the new node</param>
        /// <param name="splitKey">Receives the key that was used for the split</param>
        /// <returns>The new node created by the split</returns>
        /// <remarks>The split operation always creates a new node for the upper (right-hand) half of the keys and keeps the lower (left-hand) half in this leaf node.</remarks>
        ILeafNode Split(ulong newNodeId, out byte[] splitKey);

        /// <summary>
        /// Retrieves the value associated with the specified key in this leaf node
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <param name="buffer">Receives the value associated with <paramref name="key"/></param>
        /// <returns>True if an entry was found for <paramref name="key"/> in this node, false otherwise</returns>
        bool GetValue(byte[] key, byte[] buffer);

        /// <summary>
        /// Removes the key-value pair indexed by <paramref name="key"/> from this node
        /// </summary>
        /// <param name="key">The key of the entry to be removed</param>
        void Delete(byte[] key);

        /// <summary>
        /// Attempts to ensure that the minimum size for this node is achieved by transferring entries from the left-hand sibling
        /// </summary>
        /// <param name="leftNode">The left-hand sibling that will provide entries</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        bool RedistributeFromLeft(ILeafNode leftNode);

        /// <summary>
        /// Attempts to ensure that the minimum size for this node is achieved by transferring entries from the right-hand sibling
        /// </summary>
        /// <param name="rightNode">The right-hand sibling that will provide entries</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        bool RedistributeFromRight(ILeafNode rightNode);

        IEnumerable<KeyValuePair<byte[], byte []>> Scan();
    }
}