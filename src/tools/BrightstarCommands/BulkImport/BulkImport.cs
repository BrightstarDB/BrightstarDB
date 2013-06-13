using System;
using System.Diagnostics;
using System.IO;
using BrightstarDB.Client;

namespace BulkImport
{
    class BulkImport
    {
        static void Main(string[] args)
        {
            var parsedArgs = new BulkImportArguments();
            if (CommandLine.Parser.ParseArgumentsWithUsage(args, parsedArgs))
            {
                try
                {
                    RunImport(parsedArgs);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }
        }

        static void RunImport(BulkImportArguments parsedArgs)
        {
            var client = TryGetClient(parsedArgs.ConnectionString);
            if (client == null)
            {
                throw new ApplicationException("Unable to open connection. Please check the connection string and try again.");
            }

            FileStream logFile;
            try
            {
                logFile = File.OpenWrite(parsedArgs.LogFile);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(String.Format("Error opening log file {0} for writing: {1}",
                                                             parsedArgs.LogFile, ex.Message));
            }

            var importDirectory = new DirectoryInfo(parsedArgs.ImportDirectory);
            if (!importDirectory.Exists)
            {
                throw new ApplicationException(String.Format("Cannot find import directory '{0}'", importDirectory.FullName));
            }
            DirectoryInfo importedDirectory = null;
            if (!String.IsNullOrEmpty(parsedArgs.MoveTo))
            {
                importedDirectory = new DirectoryInfo(Path.Combine(importDirectory.FullName, parsedArgs.MoveTo));
                if (!importedDirectory.Exists)
                {
                    importedDirectory.Create();
                }
            }

            using (var logWriter = new StreamWriter(logFile))
            {
                if (!client.DoesStoreExist(parsedArgs.StoreName))
                {
                    logWriter.Write("Creating new store with name '{0}'", parsedArgs.StoreName);
                    client.CreateStore(parsedArgs.StoreName);
                }
                var timer = new Stopwatch();
                foreach (var file in importDirectory.EnumerateFiles(parsedArgs.FilePattern))
                {
                    string finalMessage;
                    timer.Reset();
                    timer.Start();
                    var importSuccessful = RunImportJob(client, parsedArgs.StoreName, file.Name, out finalMessage);
                    timer.Stop();
                    if (importSuccessful)
                    {
                        logWriter.WriteLine("Imported file '{0}' in {1} seconds", file.Name, timer.Elapsed.TotalSeconds);
                        if (importedDirectory != null)
                        {
                            file.MoveTo(Path.Combine(importedDirectory.FullName, file.Name));
                        }
                    }
                    else
                    {
                        logWriter.WriteLine("Import of file '{0}' failed. Last message was: {1}", file.FullName, finalMessage);
                    }
                }
            }

            BrightstarService.Shutdown();

        }

        static bool RunImportJob(IBrightstarService client, string storeName, string fileName, out string finalMessage )
        {
            var importJobInfo = client.StartImport(storeName, fileName);
            while(!(importJobInfo.JobCompletedOk || importJobInfo.JobCompletedWithErrors))
            {
                System.Threading.Thread.Sleep(1000);
                importJobInfo = client.GetJobInfo(storeName, importJobInfo.JobId);
            }
            finalMessage = importJobInfo.StatusMessage;
            if (importJobInfo.ExceptionInfo != null)
            {
                finalMessage += " Exception Detail:" + importJobInfo.ExceptionInfo;
            }
            return importJobInfo.JobCompletedOk;
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
