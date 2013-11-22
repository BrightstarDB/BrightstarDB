using System;
using BrightstarDB.Dto;

#if !SILVERLIGHT
using System.ServiceModel;
#endif

namespace BrightstarDB.Server
{
    internal abstract class Job
    {
        private readonly Guid _jobId;
        protected StoreWorker StoreWorker;
        protected ExceptionDetailObject ExceptionDetail;

        protected Job(Guid jobId, StoreWorker storeWorker)
        {
            _jobId = jobId;
            StoreWorker = storeWorker;
        }

        /// <summary>
        /// Logs the job data to the update log. Allows all updates to be replayed.
        /// </summary>
        protected virtual void Log()
        {
            // check server config to see if txn logging is enabled.             
        } 

        public abstract void Run();

        public Guid JobId { get { return _jobId; } }

        /// <summary>
        /// Provides an error message from the job processor
        /// </summary>
        public string ErrorMessage { get; protected set; }

        public ExceptionDetailObject ErrorInformation
        {
            get { return ExceptionDetail; }
        }

       
    }
}
