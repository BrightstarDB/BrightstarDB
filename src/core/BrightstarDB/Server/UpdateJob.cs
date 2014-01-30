using System;
using System.IO;
using BrightstarDB.Dto;
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

        protected UpdateJob(Guid jobId, string label, StoreWorker storeWorker) : base(jobId, label, storeWorker)
        {
        }

        public abstract void LogTransactionDataToStream(Stream logStream);
        public abstract void ReadTransactionDataFromStream(Stream logStream);

        public Guid TransactionId { get { return JobId; } }
        public abstract TransactionType TransactionType { get; }

        #endregion

    }
}