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
                JobStatus = GetJobStatusString(arg),
                StatusMessage = arg.StatusMessage,
                StoreName = storeName
                // TODO: Extend IJobInfo with date/time stamp properties
                // TODO: Extend with job exception info
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
