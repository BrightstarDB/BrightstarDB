using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage
{
    internal interface ITransactionLog
    {
        void LogStartTransaction(ILoggable itemToLog);
        void LogEndSuccessfulTransaction(ILoggable itemToLog);
        void LogEndFailedTransaction(ILoggable itemToLog);
        ITransactionInfo ReadTransactionInfo(int transactionNumber);
        Stream GetTransactionData(ulong dataStartPosition);
        TransactionInfoEnumerator GetTransactionList();
        /// <summary>
        /// Return an enumeration of the most recent transactions in the transaction log in reverse
        /// time order (most recent first)
        /// </summary>
        /// <param name="maxCount">The maximum number of transactions to return</param>
        /// <param name="ts">The maximum age of transaction to return</param>
        /// <returns>An enumeration of the <paramref name="maxCount"/> most recent transactions that match the age filter</returns>
        List<ITransactionInfo> GetTransactionList(int maxCount, TimeSpan ts);
    }
}
