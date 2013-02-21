#if !REST_CLIENT
using System;

namespace BrightstarDB.Client
{
    internal class TransactionInfoWrapper : ITransactionInfo
    {
        private readonly TransactionInfo _transactionInfo;

        public TransactionInfoWrapper(TransactionInfo transactionInfo)
        {
            _transactionInfo = transactionInfo;
        }

        internal TransactionInfo TransactionInfo { get { return _transactionInfo; } }

        #region Implementation of ITransactionInfo

        /// <summary>
        /// Get the name of the store that this transaction applies to
        /// </summary>
        public string StoreName
        {
            get { return _transactionInfo.StoreName; }
        }

        /// <summary>
        /// Get the store-unique identifier for this transaction
        /// </summary>
        public ulong Id
        {
            get { return _transactionInfo.Id; }
        }

        /// <summary>
        /// Get the type of transaction
        /// </summary>
        public BrightstarTransactionType TransactionType
        {
            get { return (BrightstarTransactionType)((int)_transactionInfo.TransactionType); }
        }

        /// <summary>
        /// Get the status of the transaction
        /// </summary>
        public BrightstarTransactionStatus Status
        {
            get { return (BrightstarTransactionStatus)((int)_transactionInfo.Status); }
        }

        /// <summary>
        /// Get the unique identifier of the job that processed this transaction
        /// </summary>
        public Guid JobId
        {
            get { return _transactionInfo.JobId; }
        }

        /// <summary>
        /// Get the date/time when processing started on the transaction
        /// </summary>
        public DateTime StartTime
        {
            get { return _transactionInfo.StartTime; }
        }

        #endregion
    }
}
#endif