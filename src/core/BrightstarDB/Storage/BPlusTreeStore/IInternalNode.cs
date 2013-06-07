using System.Collections.Generic;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal interface IInternalNode : INode
    {
        /// <summary>
        /// Get the boolean flag that indicates if this node has fewer entries than the minimum allowed
        /// </summary>
        bool NeedJoin { get; }

        /// <summary>
        /// Attempt to merge this node with the specified sibling node
        /// </summary>
        /// <param name="s">The sibling to merge with</param>
        /// <param name="joinKey">The key that separates this node from the sibling</param>
        /// <returns>True if the merge completed successfully, false otherwise</returns>
        bool Merge(INode s, byte[] joinKey);

        /// <summary>
        /// Returns the ID of the right-most child of this node that contains keys less than <paramref name="key"/>
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>The child node ID</returns>
        ulong GetChildNodeId(byte[] key);

        /// <summary>
        /// Splits this node into two internal nodes, creating a new node for the right-hand (upper) half of the keys
        /// </summary>
        /// <param name="rightNodeId">The ID of the page reserved to receive the newly created internal node</param>
        /// <param name="splitKey">Receives the value of the key used for the split</param>
        /// <returns>The new right-hand node</returns>
        IInternalNode Split(ulong rightNodeId, out byte[] splitKey);

        /// <summary>
        /// Inserts a new child pointer into this internal node
        /// </summary>
        /// <param name="key">The key for the child node</param>
        /// <param name="childPointer">The child node ID</param>
        /// <exception cref="NodeFullException">Raised if this node already contains the maximum number of child pointers allowed</exception>
        /// <exception cref="DuplicateKeyException">Raised if this node already contains a child node pointer with the same key</exception>
        void Insert(byte[] key, ulong childPointer);

        /// <summary>
        /// Updates the node ID pointed to be a child pointer
        /// </summary>
        /// <param name="oldChildPointer">The old child node ID to be replaced</param>
        /// <param name="newChildPointer">The new child node ID</param>
        void UpdateChildPointer(ulong oldChildPointer, ulong newChildPointer);

        /// <summary>
        /// Checks if a child node has a right-hand sibling and if so returns the siblings ID
        /// </summary>
        /// <param name="childNodeId">The child node to check</param>
        /// <param name="rightSiblingId">Receives the ID of the right-hand sibling</param>
        /// <returns>True if the child node has a right-hand sibling, false otherwise</returns>
        bool GetRightSiblingId(ulong childNodeId, out ulong rightSiblingId);

        /// <summary>
        /// Checks if a child node has a left-hand sibling and if so returns the siblings ID
        /// </summary>
        /// <param name="childNodeId">The child node to check</param>
        /// <param name="leftSiblingId">Receives the ID of the left-hand sibling</param>
        /// <returns>True if the child node has a left-hand sibling, false otherwise</returns>
        bool GetLeftSibling(ulong childNodeId, out ulong leftSiblingId);

        /// <summary>
        /// Attempts to ensure that the minimum size for this node is achieved by transferring entries from the left-hand sibling
        /// </summary>
        /// <param name="leftSibling">The left-hand sibling that will provide entries</param>
        /// <param name="joinKey">The value of the key that is present in the parent node between the pointer to this node and its left sibling</param>
        /// <param name="newJoinKey">The replacement value for the join key in the parent node</param>
        /// <returns>True if the node achieves its minimum size by the redistribution process, false otherwise</returns>
        bool RedistributeFromLeft(IInternalNode leftSibling, byte[] joinKey, byte[] newJoinKey);

        bool RedistributeFromRight(IInternalNode rightSibling, byte[] joinKey, byte [] newJoinKey);

        /// <summary>
        /// Modifies the key that is immediately before the one that indexes the specified child node
        /// </summary>
        /// <param name="childNodeId">The child node pointer</param>
        /// <param name="childNodeKey">The new key value</param>
        /// <remarks>If <paramref name="childNodeId"/> is the ID of the first child node, this operation returns without modifying this node at all</remarks>
        void SetLeftKey(ulong childNodeId, byte[] childNodeKey);

        byte[] GetKey(ulong childNodeId);
        void SetKey(ulong  childNodeId, byte[] newKey);

        /// <summary>
        /// Removes the pointer to the specified child node
        /// </summary>
        /// <param name="childNodeId">The child node pointer to be removed</param>
        /// <remarks>The key that indexes the child node is also removed</remarks>
        byte[] RemoveChildPointer(ulong childNodeId);

        /// <summary>
        /// Determine if this node contains the specified key
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>True if this node contains the specified key, false otherwise</returns>
        bool ContainsKey(byte[] key);

        /// <summary>
        /// Returns the range of child node pointers for keys start from fromKey up to toKey
        /// </summary>
        /// <param name="fromKey"></param>
        /// <param name="toKey"></param>
        /// <returns></returns>
        IEnumerable<ulong> Scan(byte[] fromKey, byte[] toKey);

        IEnumerable<ulong> Scan();

        ulong GetChildPointer(int ix);
    }
}