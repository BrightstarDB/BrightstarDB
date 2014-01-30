using BrightstarDB.Client;
using BrightstarDB.Dto;

namespace BrightstarDB.Server.Modules
{
    public static class JobInfoExtensions
    {
        public static JobResponseModel MakeResponseObject(this IJobInfo arg, string storeName)
        {
            return new JobResponseModel
                {
                    JobId = arg.JobId,
                    Label = arg.Label,
                    JobStatus = GetJobStatusString(arg),
                    StatusMessage = arg.StatusMessage,
                    StoreName = storeName,
                    ExceptionInfo = arg.ExceptionInfo,
                    QueuedTime = arg.QueuedTime,
                    StartTime = arg.StartTime,
                    EndTime = arg.EndTime
                };
        }

        public static string GetJobStatusString(this IJobInfo jobInfo)
        {
            if (jobInfo.JobPending) return "Pending";
            if (jobInfo.JobStarted) return "Started";
            if (jobInfo.JobCompletedOk) return "CompletedOk";
            if (jobInfo.JobCompletedWithErrors) return "TransactionError";
            return "Unknown";
        }
    }
}
