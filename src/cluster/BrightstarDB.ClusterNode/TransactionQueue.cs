using System;
using System.Collections.Generic;

namespace BrightstarDB.ClusterNode
{
    internal class TransactionQueue
    {
        public TransactionQueue(string baseLocation)
        {

        }

        public void LogTransaction(ClusterUpdateTransaction txn)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Return in-order enumeration of all transactions logged after the one with id fromTxnId
        /// </summary>
        /// <param name="storeId"></param>
        /// <param name="fromTxnId"></param>
        /// <returns></returns>
        public IEnumerable<ClusterUpdateTransaction> GetTransactions(string storeId, Guid fromTxnId)
        {
            throw new NotImplementedException();
        }
    }
}