using BrightstarDB.Client;

namespace BrightstarDB.Azure.StoreWorker
{
    internal class AzureExportJob
    {
        private string _jobId;
        private readonly Server.StoreWorker _worker;
        private readonly BlobImportSource _exportSource;

        public AzureExportJob(string jobId, Server.StoreWorker worker, BlobImportSource exportSource)
        {
            _jobId = jobId;
            _worker = worker;
            _exportSource = exportSource;
        }

        public void Run()
        {
            using(var exportStream = _exportSource.OpenWrite())
            {
                _worker.ExportData(exportStream);
            }
        }
    }
}