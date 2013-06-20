using System.Collections.Generic;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal interface INode
    {
        /// <summary>
        /// Get or set the ID of the page where this node is persisted
        /// </summary>
        ulong PageId { get; }

        /// <summary>
        /// Get or set the boolean flag that indicates if this node has been modified since it was loaded
        /// </summary>
        bool IsDirty { get;}

        /// <summary>
        /// Get the boolean flag that indicates if this node is a leaf node
        /// </summary>
        bool IsLeaf { get; }

        /// <summary>
        /// Get the boolean flag that indicates if this node has reached the limit for the number of keys it can contain
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Get the highest key stored in this node
        /// </summary>
        byte[] RightmostKey { get; }

        /// <summary>
        /// Get the lowest key stored in this node
        /// </summary>
        byte[] LeftmostKey { get; }

        /// <summary>
        /// Get the serialized representation of this node
        /// </summary>
        /// <returns>The serialized node representation as a byte array</returns>
        byte[] GetData();

        /// <summary>
        /// Get the current count of keys stored in this node
        /// </summary>
        int KeyCount { get; }

        /// <summary>
        /// Dump a trace of the structure of this node to the console
        /// </summary>
        /// <param name="tree">The tree that contains this node</param>
        /// <param name="indentLevel">The indent level to use when writing the structure</param>
        void DumpStructure(BPlusTree tree, int indentLevel);

        /// <summary>
        /// Dump the structure of this node to a string.
        /// </summary>
        /// <returns>The node structure dumped as a string</returns>
        /// <remarks>This method does not recurse into child nodes - it returns only the structure of the node it is called on. To dump
        /// the entire subtree, use the <see cref="DumpStructure"/> method.</remarks>
        string Dump();
    }
}