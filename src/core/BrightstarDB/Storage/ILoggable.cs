using System;
using System.IO;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Interface implemented by objects that can be written to a txn log
    /// </summary>
    internal interface ILoggable
    {
        /// <summary>
        /// Get the job or transaction id
        /// </summary>
        Guid TransactionId { get; }

        /// <summary>
        /// Get the type of transaction to log
        /// </summary>
        TransactionType TransactionType { get; }

        /// <summary>
        /// Write the transaction data to the log stream
        /// </summary>
        /// <param name="logStream"></param>
        void LogTransactionDataToStream(Stream logStream);

        /// <summary>
        /// Read the transaction data from the log stream
        /// </summary>
        /// <param name="logStream"></param>
        void ReadTransactionDataFromStream(Stream logStream);
        
    }
}
