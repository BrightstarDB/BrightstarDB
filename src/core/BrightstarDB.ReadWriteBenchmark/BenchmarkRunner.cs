using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using BrightstarDB.Client;
using BrightstarDB.Storage;

namespace BrightstarDB.ReadWriteBenchmark
{
    public class BenchmarkRunner
    {
        public string[] SourceFiles;
        private readonly IBrightstarService _brightstar;
        private readonly string _storeName;
        private int _triplesImported;
        private readonly List<string> _activeJobList = new List<string>();
        private const int MaxExportJobCount = 4;

        public BenchmarkRunner(BenchmarkArgs args)
        {
            SourceFiles = Directory.GetFiles(args.TestDataDirectory, "*.nt", SearchOption.AllDirectories);
            BenchmarkLogging.Info("Found {0} source NTriples files for benchmark test", SourceFiles.Length);
            if (args.MaxNumberFiles > 0 && args.MaxNumberFiles < SourceFiles.Length)
            {
                SourceFiles = SourceFiles.Take(args.MaxNumberFiles).ToArray();
                BenchmarkLogging.Info("Limited number of source files to be processed to {0} (from command-line options)", SourceFiles.Length);
            }
            _brightstar = new EmbeddedBrightstarService(".");
            _storeName = "BenchmarkStore_" + DateTime.Now.ToString("yyMMdd_HHmmss");
        }

        public void Run()
        {
            _brightstar.CreateStore(_storeName, PersistenceType.AppendOnly);
            for (int i = 0; i < SourceFiles.Length; i++)
            {
                ImportSourceFile(i);
                VerifyTriples(_triplesImported);
            }
            WaitForAllExportsToComplete();
        }

        private void ImportSourceFile(int sourceFileIndex)
        {
            var fullText = File.ReadAllText(SourceFiles[sourceFileIndex]);
            var lineCount = CountLines(SourceFiles[sourceFileIndex]);
            var sw = Stopwatch.StartNew();
            _brightstar.ExecuteTransaction(_storeName, new UpdateTransactionData {InsertData = fullText});
            var importTime = sw.ElapsedMilliseconds;
            BenchmarkLogging.Info("IMPORT: {0},{1},{2}", _triplesImported, _triplesImported+lineCount, importTime);
            _triplesImported += lineCount;
        }

        private void VerifyTriples(int expectCount)
        {
            if (_activeJobList.Count == MaxExportJobCount)
            {
                WaitForAnExportToComplete();
            }
            var label = _storeName + "_export_" + expectCount;
            var exportJobInfo = _brightstar.StartExport(_storeName, label + ".nt", exportFormat: RdfFormat.NTriples, label:label);
            _activeJobList.Add(exportJobInfo.JobId);
        }

        private static int CountLines(string filePath)
        {
            var lineCount = 0;
            using (var rdr = new StreamReader(filePath))
            {
                while (rdr.ReadLine() != null) lineCount++;
            }
            return lineCount;
        }

        private void WaitForAllExportsToComplete()
        {
            BenchmarkLogging.Info("Waiting for final export jobs to complete.");
            while (_activeJobList.Count > 0)
            {
                var toRemove = new List<string>();
                foreach (var jobId in _activeJobList)
                {
                    var jobInfo = _brightstar.GetJobInfo(_storeName, jobId);
                    if (jobInfo.JobCompletedWithErrors)
                    {
                        BenchmarkLogging.Error("Export {0} failed. Cause: {1}. Detail {2}", jobInfo.Label,
                            jobInfo.StatusMessage, jobInfo.ExceptionInfo);
                        toRemove.Add(jobId);
                    }
                    if (jobInfo.JobCompletedOk)
                    {
                        ReportCompletedJob(jobInfo);
                        toRemove.Add(jobId);
                    }
                }
                foreach (var r in toRemove) _activeJobList.Remove(r);
                Thread.Sleep(5000);
            }
        }

        private void ReportCompletedJob(IJobInfo jobInfo)
        {
            var expectedCount = Int32.Parse(jobInfo.Label.Split('_').Last());
            BenchmarkLogging.Info("EXPORT {0}, {1}", expectedCount, jobInfo.EndTime.Subtract(jobInfo.StartTime).TotalMilliseconds);
            var actualCount = CountLines("import" + Path.DirectorySeparatorChar + jobInfo.Label + ".nt");
            if (expectedCount == actualCount)
            {
                BenchmarkLogging.Info("Validated expected number of triples exported in {0}.nt", jobInfo.Label);
            }
            else
            {
                BenchmarkLogging.Error("Exported triple count in file {0}.nt does not match the expected count. (Expected: {1}, Counted: {2})",
                    jobInfo.Label, expectedCount, actualCount);
            }
        }

        private void WaitForAnExportToComplete()
        {
            var toRemove = new List<string>();
            while (toRemove.Count == 0 && _activeJobList.Count >= MaxExportJobCount)
            {
                foreach (var jobId in _activeJobList)
                {
                    var jobInfo = _brightstar.GetJobInfo(_storeName, jobId);
                    if (jobInfo.JobCompletedWithErrors)
                    {
                        BenchmarkLogging.Error("Export {0} failed. Cause: {1}. Detail {2}", jobInfo.Label,
                            jobInfo.StatusMessage, jobInfo.ExceptionInfo);
                        toRemove.Add(jobId);
                    }
                    if (jobInfo.JobCompletedOk)
                    {
                        ReportCompletedJob(jobInfo);
                        toRemove.Add(jobId);
                    }
                }
                if (toRemove.Count == 0)
                {
                    BenchmarkLogging.Debug("Waiting for export jobs...");
                    Thread.Sleep(5000);
                }
            }
            foreach (var id in toRemove)
            {
                _activeJobList.Remove(id);
            }
        }
    }
}
