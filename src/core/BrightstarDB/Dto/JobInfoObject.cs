using BrightstarDB.Client;

namespace BrightstarDB.Dto
{
    internal class JobInfoObject : IJobInfo
    {
        public bool JobPending { get { return JobStatus == JobStatus.Pending; } }
        public bool JobStarted { get {return JobStatus == JobStatus.Started; } }

        public bool JobCompletedWithErrors
        {
            get
            {
                return JobStatus == JobStatus.TransactionError || JobStatus == JobStatus.NotRegistered ||
                       JobStatus == JobStatus.Unknown;
            }
        }

        public bool JobCompletedOk { get { return JobStatus == JobStatus.CompletedOk; } }

        public string StatusMessage { get; set; }
        public string JobId { get; set; }
        public ExceptionDetailObject ExceptionInfo { get; set; }
        public JobStatus JobStatus { get; set; }


    }
}
