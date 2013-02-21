#if !REST_CLIENT
#if !SILVERLIGHT
using System.ServiceModel;
#endif
namespace BrightstarDB.Client
{
    internal class JobInfoWrapper : IJobInfo
    {
        private readonly JobInfo _jobInfo;

        internal JobInfo JobInfo { get { return _jobInfo; } }

        public JobInfoWrapper(JobInfo jobInfo)
        {
            _jobInfo = jobInfo;
        }

        public bool JobPending
        {
            get { return _jobInfo.JobPending; }
        }

        public bool JobStarted
        {
            get { return _jobInfo.JobStarted; }
        }

        public bool JobCompletedWithErrors
        {
            get { return _jobInfo.JobCompletedWithErrors; }
        }

        public bool JobCompletedOk
        {
            get { return _jobInfo.JobCompletedOk; }
        }

        public string StatusMessage
        {
            get { return _jobInfo.StatusMessage; }
        }

        public string JobId
        {
            get { return _jobInfo.JobId; }
        }

        public ExceptionDetail ExceptionInfo
        {
            get { return _jobInfo.ExceptionInfo; }
        }
    }
}
#endif