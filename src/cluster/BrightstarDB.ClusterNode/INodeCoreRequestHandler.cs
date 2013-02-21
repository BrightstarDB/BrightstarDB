using System;
using System.Collections.Generic;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterNode
{
    /// <summary>
    /// This interface is the callback interface from the node server
    /// </summary>
    internal interface INodeCoreRequestHandler
    {
        /// <summary>
        /// A request to accept the proposed endpoint at the master node.
        /// </summary>
        /// <param name="host">The proposed master host name</param>
        /// <param name="port">The proposed master port number</param>
        /// <returns>True is this is an acceptable master</returns>
        bool SlaveOf(string host, int port);

        bool SetMaster(MasterConfiguration masterConfiguration);

        CoreState GetStatus();

        /// <summary>
        /// Handle a request to synchronize a slave to this node
        /// </summary>
        /// <param name="lastTxns">A dictionary of the stores that the slave knows about with their last committed transaction id</param>
        bool SyncSlave(Dictionary<string, string> lastTxns, SyncContext context, Func<SyncContext, Message, bool> messageSink);

        /// <summary>
        /// Handle a endsync message from a master to a slave
        /// </summary>
        /// <param name="syncStatus">Indicates the status of the sync. Should be either OK or FAIL.</param>
        void SlaveSyncCompleted(string syncStatus);

        /// <summary>
        /// Receive a transaction to process for a store
        /// </summary>
        /// <param name="transactionMessage">The transaction to process</param>
        /// <returns>True if the transaction was queued successfully, false otherwise</returns>
        bool ProcessSlaveTransaction(ClusterTransaction transactionMessage);

        /// <summary>
        /// Receive a catchup (sync state) transaction to process for a store
        /// </summary>
        /// <param name="txn">The sync transaction to process</param>
        /// <returns>True if the transaction was queued successfully, false otherwise</returns>
        bool ProcessSyncTransaction(ClusterTransaction txn);

        bool CreateStore(string storeName);
        bool DeleteStore(string storeName);
    }

}