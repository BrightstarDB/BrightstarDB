#if !SILVERLIGHT
using System;
using System.Collections.Generic;

namespace BrightstarDB.Analysis
{
    /// <summary>
    /// An analyzer that simply builds up an in-memory structure of the information reported by the crawler
    /// </summary>
    public class StoreDataGatherer: IStoreAnalyzer
    {
        private StoreReport _report;
        private BTreeReport _btreeReport;
        private BTreeReport _relatedResourceListReport;
        private Stack<NodeReport> _nodeReportStack;

        /// <summary>
        /// Returns the store report gathered from the crawler
        /// </summary>
        public StoreReport Report { get { return _report; } }

        #region Implementation of IStoreAnalyzer

        /// <summary>
        /// Invoked when the crawler initially opens the store
        /// </summary>
        /// <param name="storeId">The id of the store root object</param>
        /// <param name="storePath">The path to the store directory</param>
        /// <param name="nextObjectId">The next available object id</param>
        /// <param name="commitTime">The date/time of the last commit</param>
        public void OnStoreStart(ulong storeId, string storePath, ulong nextObjectId, DateTime commitTime)
        {
            _report = new StoreReport(storePath, DateTime.Now, storeId, nextObjectId, commitTime);
            _nodeReportStack = new Stack<NodeReport>();
        }

        /// <summary>
        /// Invoked when the crawler encounters a BTree
        /// </summary>
        /// <param name="btreeName">The internal name for the btree</param>
        /// <param name="btreeId">The id of the btree root object</param>
        /// <param name="branchingFactor">The BTree's branching factor (maximum keys per node)</param>
        /// <param name="minimizationFactor">The BTree's minimization factor (minimum keys per node)</param>
        public void OnBTreeStart(string btreeName, ulong btreeId, int branchingFactor, int minimizationFactor)
        {
            _btreeReport = new BTreeReport(btreeName, btreeId, branchingFactor, minimizationFactor);
        }

        ///<summary>
        /// Invoked when the crawler encounters a predicate -> btree index
        ///</summary>
        ///<param name="indexName">The index name</param>
        ///<param name="entryCount">The number of entries in the index</param>
        public void OnPredicateIndexStart(string indexName, int entryCount)
        {
            _report.PredicateCount = entryCount;
        }

        /// <summary>
        /// Invoked when the crawler has finished processing a predicate -> btree index and its contents
        /// </summary>
        /// <param name="indexName"></param>
        public void OnPredicateIndexEnd(string indexName)
        {
            
        }

        /// <summary>
        /// Invoked when the crawler has finished processing a BTree and its contents
        /// </summary>
        /// <param name="btreeName"></param>
        /// <param name="btreeId"></param>
        public void OnBTreeEnd(string btreeName, ulong btreeId)
        {
            _report.BTrees.Add(_btreeReport);      
        }

        /// <summary>
        /// Invoked when the crawler encounters a BTree node
        /// </summary>
        /// <param name="objectId">The node id</param>
        /// <param name="currentDepth">The depth of the node in the BTree</param>
        /// <param name="keyCount">The number of keys on the node</param>
        /// <param name="childNodeCount">The number of child nodes on the node</param>
        public void OnNodeStart(ulong objectId, int currentDepth, int keyCount, int childNodeCount)
        {
            var nodeReport = new NodeReport(objectId, currentDepth, keyCount, childNodeCount);
            var btreeReport = _relatedResourceListReport ?? _btreeReport;
            if (currentDepth == 0)
            {
                btreeReport.RootNode = nodeReport;
            }
            else
            {
                if (btreeReport.Depth < (currentDepth + 1))
                {
                    btreeReport.Depth = currentDepth + 1;
                }
                _nodeReportStack.Peek().Children.Add(nodeReport);
            }
            _nodeReportStack.Push(nodeReport);
        }

        /// <summary>
        /// Invoked when the crawler has finished processing a BTree node and its subtree.
        /// </summary>
        /// <param name="objectId">The node id</param>
        public void OnNodeEnd(ulong objectId)
        {
            _nodeReportStack.Pop();
        }

        ///<summary>
        /// Invoked when the crawler has finished processing a store and all of its contents
        ///</summary>
        ///<param name="storeId"></param>
        public void OnStoreEnd(ulong storeId)
        {
            
        }

        /// <summary>
        /// Invoked when the crawler finds a related resource list inside a BTree node
        /// </summary>
        /// <param name="listName"></param>
        /// <param name="listId"></param>
        /// <param name="branchingFactor"></param>
        /// <param name="minimizationFactor"></param>
        /// <remarks>On finding a related resource list, the crawler will crawl all the nodes in the related resource list btree, generating
        /// OnNodeStart and OnNodeEnd events</remarks>
        public void OnRelatedResourceListStart(string listName, ulong listId, int branchingFactor, int minimizationFactor)
        {
            _relatedResourceListReport = new BTreeReport(listName, listId, branchingFactor, minimizationFactor);
            _nodeReportStack.Peek().RelatedResourceLists.Add(_relatedResourceListReport);
        }

        /// <summary>
        /// Invoked when the crawler finishes processing a related resource list and all of its contents
        /// </summary>
        /// <param name="listId"></param>
        public void OnRelatedResourceListEnd(ulong listId)
        {
            _relatedResourceListReport = null;
        }

        #endregion
    }
}
#endif