using System;
using System.IO;
using BrightstarDB.Storage;

namespace BrightstarDB.Server
{
    /// <summary>
    /// Derived base class for jobs that perform some update on the store and need to log
    /// transaction information.
    /// </summary>
    internal abstract class UpdateJob : Job, ILoggable
    {
        #region Implementation of ILoggable

        protected UpdateJob(Guid jobId, StoreWorker storeWorker) : base(jobId, storeWorker)
        {
        }

        public abstract void LogTransactionDataToStream(Stream logStream);
        public abstract void ReadTransactionDataFromStream(Stream logStream);

        public Guid TransactionId { get { return JobId; } }
        public abstract TransactionType TransactionType { get; }

        #endregion

    }
}