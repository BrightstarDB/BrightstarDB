using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Client;

namespace Compress
{
    class Compress
    {
        static void Main(string[] args)
        {
            var parsedArgs = new CompressArguments();
            if (CommandLine.Parser.ParseArgumentsWithUsage(args, parsedArgs))
            {
                try
                {
                    RunCompress(parsedArgs);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
        }

        private static void RunCompress(CompressArguments parsedArgs)
        {
            var client = TryGetClient(parsedArgs.ConnectionString);
            if (client == null)
            {
                throw new ApplicationException("Unable to open connection. Please check the connection string and try again.");
            }
            string finalMessage;
            RunCompressJob(client, parsedArgs.StoreName, out finalMessage);
        }

        static bool RunCompressJob(IBrightstarService client, string storeName, out string finalMessage)
        {
            var compressJob = client.ConsolidateStore(storeName);
            while (!(compressJob.JobCompletedOk || compressJob.JobCompletedWithErrors))
            {
                System.Threading.Thread.Sleep(1000);
                compressJob = client.GetJobInfo(storeName, compressJob.JobId);
            }
            finalMessage = compressJob.StatusMessage;
            if (compressJob.ExceptionInfo != null)
            {
                finalMessage += " Exception Detail:" + compressJob.ExceptionInfo;
            }
            return compressJob.JobCompletedOk;
        }

        static IBrightstarService TryGetClient(string connectionString)
        {
            try
            {
                return BrightstarService.GetClient(connectionString);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
