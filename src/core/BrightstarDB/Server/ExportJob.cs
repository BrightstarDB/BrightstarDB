using System;
using System.IO;
using System.Threading;
#if PORTABLE
using BrightstarDB.Portable.Adaptation;
using BrightstarDB.Portable.Compatibility;
using BrightstarDB.Storage;
#endif
using BrightstarDB.Rdf;

namespace BrightstarDB.Server
{
    internal class ExportJob
    {
        private readonly Guid _jobId;
        private readonly string _label;
        private readonly StoreWorker _storeWorker;
        private readonly string _outputFileName;
        private readonly string _graphUri;
        private Action<Guid, Exception> _errorCallback;
        private Action<Guid> _successCallback;

        public ExportJob(Guid jobId, string label, StoreWorker storeWorker, string outputFileName, string graphUri)
        {
            _jobId = jobId;
            _label = label;
            _storeWorker = storeWorker;
            _outputFileName = outputFileName;
            _graphUri = graphUri;
        }

        public void Run(Action<Guid, Exception> errorCallback, Action<Guid> successCallback)
        {
            _errorCallback = errorCallback;
            _successCallback = successCallback;
            ThreadPool.QueueUserWorkItem(RunExport, this);
        }

        private static void RunExport(object jobData)
        {
            var exportJob = jobData as ExportJob;
            if (exportJob == null) return;
            try
            {
                var storeDirectory = exportJob._storeWorker.WriteStore.DirectoryPath;
                var exportDirectory = Path.Combine(Path.GetDirectoryName(storeDirectory), "import");
#if PORTABLE
                var persistenceManager = PlatformAdapter.Resolve<IPersistenceManager>();
                if (!persistenceManager.DirectoryExists(exportDirectory)) persistenceManager.CreateDirectory(exportDirectory);
                var filePath = Path.Combine(exportDirectory, exportJob._outputFileName);
                using (var stream = persistenceManager.GetOutputStream(filePath, FileMode.Create))
#else
                if (!Directory.Exists(exportDirectory)) Directory.CreateDirectory(exportDirectory);
                var filePath = Path.Combine(exportDirectory, exportJob._outputFileName);
                Logging.LogDebug("Export file path calculated as '{0}'", filePath);
                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write))
#endif
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
#if !PORTABLE
                    stream.Flush(true);
                    stream.Close();
#endif
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
