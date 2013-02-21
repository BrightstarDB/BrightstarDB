using System;
using System.IO;
using System.Threading;
using BrightstarDB.Rdf;

namespace BrightstarDB.Server
{
    internal class ExportJob
    {
        private readonly Guid _jobId;
        private readonly StoreWorker _storeWorker;
        private readonly string _outputFileName;
        private readonly string _graphUri;
        private Action<Guid, Exception> _errorCallback;
        private Action<Guid> _successCallback;

        public ExportJob(Guid jobId, StoreWorker storeWorker, string outputFileName, string graphUri)
        {
            _jobId = jobId;
            _storeWorker = storeWorker;
            _outputFileName = outputFileName;
            _graphUri = graphUri;
        }

        public void Run(Action<Guid, Exception> errorCallback, Action<Guid> successCallback)
        {
            _errorCallback = errorCallback;
            _successCallback = successCallback;
            var writerThread = new Thread(RunExport);
            try
            {
                writerThread.Start(this);
            }
            catch (Exception ex)
            {
                errorCallback(_jobId, ex);
            }
        }

        private static void RunExport(object jobData)
        {
            var exportJob = jobData as ExportJob;
            if (exportJob == null) return;
            try
            {
                var storeDirectory = exportJob._storeWorker.WriteStore.DirectoryPath;
                var exportDirectory = Path.Combine(storeDirectory, ".." + Path.DirectorySeparatorChar + "import");
                if (!Directory.Exists(exportDirectory)) Directory.CreateDirectory(exportDirectory);
                var filePath = Path.Combine(exportDirectory, exportJob._outputFileName);
                Logging.LogDebug("Export file path calculated as '{0}'", filePath);
                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
                {
                    string[] graphs = String.IsNullOrEmpty(exportJob._graphUri)
                                          ? null
                                          : new[] {exportJob._graphUri};
                    var triples = exportJob._storeWorker.ReadStore.Match(null, null, null, graphs:graphs);
                    var sw = new StreamWriter(stream);
                    var nw = new BrightstarTripleSinkAdapter(new NTriplesWriter(sw));
                    foreach (var triple in triples)
                    {
                        nw.Triple(triple);
                    }
                    sw.Flush();
                    stream.Flush(true);
                    stream.Close();
                }
                exportJob._successCallback(exportJob._jobId);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ExportDataError, "Error Exporting Data {0} {1}", ex.Message, ex.StackTrace);
                exportJob._errorCallback(exportJob._jobId, ex);
            }

        }
    }
}
