using System;

namespace BrightstarDB.Analysis
{
    /// <summary>
    /// The interface to be implemented by any class that wants to use the 
    /// <see cref="StoreCrawler"/> to introspect the full content of a store
    /// </summary>
    public interface IStoreAnalyzer
    {
        /// <summary>
        /// Invoked when the crawler initially opens the store
        /// </summary>
        /// <param name="storeId">The id of the store root object</param>
        /// <param name="storePath">The path to the store directory</param>
        /// <param name="nextObjectId">The next available object id</param>
        /// <param name="commitTime"></param>
        void OnStoreStart(ulong storeId, string storePath, ulong nextObjectId, DateTime commitTime);

        /// <summary>
        /// Invoked when the crawler encounters a BTree
        /// </summary>
        /// <param name="btreeName">The internal name for the btree</param>
        /// <param name="btreeId">The id of the btree root object</param>
        /// <param name="branchingFactor">The BTree's branching factor (maximum keys per node)</param>
        /// <param name="minimizationFactor">The BTree's minimization factor (minimum keys per node)</param>
        void OnBTreeStart(string btreeName, ulong btreeId, int branchingFactor, int minimizationFactor);

        ///<summary>
        /// Invoked when the crawler encounters a predicate -> btree index
        ///</summary>
        ///<param name="indexName">The index name</param>
        ///<param name="entryCount">The number of entries in the index</param>
        void OnPredicateIndexStart(string indexName, int entryCount);


        /// <summary>
        /// Invoked when the crawler has finished processing a predicate -> btree index and its contents
        /// </summary>
        /// <param name="indexName"></param>
        void OnPredicateIndexEnd(string indexName);

        /// <summary>
        /// Invoked when the crawler has finished processing a BTree and its contents
        /// </summary>
        /// <param name="btreeName"></param>
        /// <param name="btreeId"></param>
        void OnBTreeEnd(string btreeName, ulong btreeId);

        /// <summary>
        /// Invoked when the crawler encounters a BTree node
        /// </summary>
        /// <param name="objectId">The node id</param>
        /// <param name="currentDepth">The depth of the node in the BTree</param>
        /// <param name="keyCount">The number of keys on the node</param>
        /// <param name="childNodeCount">The number of child nodes on the node</param>
        void OnNodeStart(ulong objectId, int currentDepth, int keyCount, int childNodeCount);

        /// <summary>
        /// Invoked when the crawler has finished processing a BTree node and its subtree.
        /// </summary>
        /// <param name="objectId">The node id</param>
        void OnNodeEnd(ulong objectId);

        ///<summary>
        /// Invoked when the crawler has finished processing a store and all of its contents
        ///</summary>
        ///<param name="storeId"></param>
        void OnStoreEnd(ulong storeId);

        /// <summary>
        /// Invoked when the crawler finds a related resource list inside a BTree node
        /// </summary>
        /// <param name="listName"></param>
        /// <param name="listId"></param>
        /// <param name="branchingFactor"></param>
        /// <param name="minimizationFactor"></param>
        /// <remarks>On finding a related resource list, the crawler will crawl all the nodes in the related resource list btree, generating
        /// OnNodeStart and OnNodeEnd events</remarks>
        void OnRelatedResourceListStart(string listName, ulong listId, int branchingFactor, int minimizationFactor);

        /// <summary>
        /// Invoked when the crawler finishes processing a related resource list and all of its contents
        /// </summary>
        /// <param name="listId"></param>
        void OnRelatedResourceListEnd(ulong listId);
    }
}
