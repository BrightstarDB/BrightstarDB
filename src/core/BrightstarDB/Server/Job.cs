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
        private readonly string _label;
        protected StoreWorker StoreWorker;
        protected ExceptionDetailObject ExceptionDetail;

        protected Job(Guid jobId, string label, StoreWorker storeWorker)
        {
            _jobId = jobId;
            _label = label;
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

        public string Label { get { return _label; } }

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
