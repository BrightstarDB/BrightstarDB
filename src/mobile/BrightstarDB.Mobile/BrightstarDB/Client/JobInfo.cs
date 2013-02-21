#if !SILVERLIGHT

#endif


namespace BrightstarDB.Client
{
    public class JobInfo : IJobInfo
    {
        public bool JobPending { get; internal set; }
        public bool JobStarted { get; internal set; }
        public bool JobCompletedWithErrors { get; internal set; }
        public bool JobCompletedOk { get; internal set; }
        public string StatusMessage { get; internal set; }
        public string JobId { get; internal set; }
        public ExceptionDetail ExceptionInfo { get; internal set; }
    }
}
