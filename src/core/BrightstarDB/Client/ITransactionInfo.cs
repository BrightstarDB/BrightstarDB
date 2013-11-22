using System;
using BrightstarDB.Dto;

namespace BrightstarDB.Client
{
    ///<summary>
    /// ITransationInfo contains information about a specific transaction.
    ///</summary>
    public interface ITransactionInfo
    {
        /// <summary>
        /// Get the name of the store that this transaction applies to
        /// </summary>
        string StoreName { get; }

        /// <summary>
        /// Get the store-unique identifier for this transaction
        /// </summary>
        ulong Id { get; }
        
        /// <summary>
        /// Get the type of transaction
        /// </summary>
        TransactionType TransactionType { get; }

        /// <summary>
        /// Get the status of the transaction
        /// </summary>
        TransactionStatus Status { get; }
        
        /// <summary>
        /// Get the unique identifier of the job that processed this transaction
        /// </summary>
        Guid JobId { get; }

        /// <summary>
        /// Get the date/time when processing started on the transaction
        /// </summary>
        DateTime StartTime { get; }
    }
}
