using BrightstarDB.Client;
using BrightstarDB.Dto;

namespace BrightstarDB.Polaris
{
    public static class JobInfoHelpers
    {
        public static string ExtractJobErrorMessage(this IJobInfo jobInfo, bool stopOnFirstDetailMessage = false)
        {
            if (jobInfo.ExceptionInfo != null)
            {
                return jobInfo.StatusMessage + ": " + jobInfo.ExceptionInfo.ExtractExceptionMessages(stopOnFirstDetailMessage);
            }
            return jobInfo.StatusMessage;
        }

        public static string ExtractExceptionMessages(this ExceptionDetailObject exceptionDetailObject, bool stopOnFirstDetailMessage = false)
        {
            var msg = string.Empty;
            while (exceptionDetailObject != null)
            {
                if (!string.IsNullOrEmpty(exceptionDetailObject.Message))
                {
                    if (stopOnFirstDetailMessage)
                    {
                        return exceptionDetailObject.Message;
                    }
                    msg += exceptionDetailObject.Message;
                }
                exceptionDetailObject = exceptionDetailObject.InnerException;
            }
            return msg;
        }

    }


}
