using System;
using System.Diagnostics;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Web.Script.Serialization;
using System.Xml;
using BrightstarDB.Azure.Common;
using BrightstarDB.Client;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using JobInfo = BrightstarDB.Azure.Common.JobInfo;
using JobStatus = BrightstarDB.Azure.Common.JobStatus;

namespace BrightstarDB.Azure.StoreWorker
{
    public class WorkerRole : RoleEntryPoint
    {

        private ServiceHost _blockUpdateService;
        private ServiceHost _storeWorkerService;
        private const int IdleSleepPeriod = 1000;
        private const int BusySleepPeriod = 100; // At busy times a short sleep to prevent starvation of queries
        private IJobQueue _jobQueue;
        private bool _stopping;

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.WriteLine("StoreWorker entry point called", "Information");
            string lastWriteStore = null;
            while (!_stopping)
            {
                try
                {
                    var jobInfo = _jobQueue.NextJob(lastWriteStore);
                    if (jobInfo == null)
                    {
                        if (lastWriteStore == null)
                        {
                            Thread.Sleep(IdleSleepPeriod);
                        }
                        else
                        {
                            lastWriteStore = null;
                        }
                    }
                    else
                    {
                        try
                        {
                            lastWriteStore = jobInfo.StoreId;
                            ProcessJob(jobInfo);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error processing job {0}. Cause: {1}.", jobInfo.Id, ex);
                            _jobQueue.FailWithException(jobInfo.Id,
                                                        "Unexpected internal exception during job processing. ", ex);
                            lastWriteStore = null;
                        }
                        Thread.Sleep(BusySleepPeriod);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error in run loop: {0}", ex);
                }
            }
        }

        private static IStoreManager GetStoreManager()
        {
            var config = StoreConfiguration.DefaultStoreConfiguration;
            config.DisableBackgroundWrites = true;
            return new AzureStoreManager(config);
        }

        private void ProcessJob(JobInfo jobInfo)
        {
            switch (jobInfo.JobType)
            {
                case JobType.Transaction:
                    {
                        try
                        {
                            Trace.TraceInformation("ProcessJob: Starting processing on Transaction job {0}", jobInfo.Id);
                            var storeManager = GetStoreManager();
                            var worker = new Server.StoreWorker(jobInfo.StoreId, storeManager);
                            var transactionData = jobInfo.Data.Split(new string[] {AzureConstants.TransactionSeparator},
                                                                     StringSplitOptions.None);
                            var update = new UpdateTransaction(Guid.Parse(jobInfo.Id), worker, transactionData[0],
                                                               transactionData[1], transactionData[2]);
                            update.Run();
                            _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedOk, "Transaction completed successfully");
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("ProcessJob: Transaction job failed with exception {0}", ex);
                            _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedWithErrors,
                                                  String.Format("Transaction failed: {0}", ex));
                        }
                        break;
                    }

                case JobType.Import:
                    {
                        try
                        {
                            Trace.TraceInformation("ProcessJob: Starting processing on Import job {0}", jobInfo.Id);
                            var storeManager = GetStoreManager();
                            var worker = new Server.StoreWorker(jobInfo.StoreId, storeManager);
                            BlobImportSource importSource;
                            if (!TryDeserialize(jobInfo.Data, out importSource))
                            {
                                importSource = new BlobImportSource {BlobUri = jobInfo.Data};
                            }
                            var import = new AzureImportJob(jobInfo.Id, worker, importSource,
                                                            (jobId, statusMessage) =>
                                                            _jobQueue.UpdateStatus(jobId, statusMessage));
                            import.Run();
                            if (import.Errors)
                            {
                                Trace.TraceError("ProcessJob: Import job {0} failed with error: {1}. Marking job as CompletedWithErrors", jobInfo.Id,
                                                 import.ErrorMessage);
                                _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedWithErrors, import.ErrorMessage);
                            }
                            else
                            {
                                _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedOk, "Import completed successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("ProcessJob: Import job {0} failed with exception {1}", jobInfo.Id, ex);
                            _jobQueue.FailWithException(jobInfo.Id, "Import failed", ex);
                        }
                        break;
                    }

                case JobType.Export:
                    {
                        try
                        {
                            Trace.TraceInformation("ProcessJob: Starting processing on Export job {0}", jobInfo.Id);
                            var storeManager = GetStoreManager();
                            var worker = new Server.StoreWorker(jobInfo.StoreId, storeManager);
                            BlobImportSource exportSource;
                            if (!TryDeserialize(jobInfo.Data, out exportSource))
                            {
                                exportSource = new BlobImportSource {BlobUri = jobInfo.Data};
                            }
                            var export = new AzureExportJob(jobInfo.Id, worker, exportSource);
                            export.Run();
                            _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedOk, "Export completed successfully");
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("ProcessJob: Export job {0} failed with exception {1}", jobInfo.Id, ex);
                            _jobQueue.FailWithException(jobInfo.Id, "Export failed", ex);
                        }
                        break;
                    }

                case JobType.DeleteStore:
                    {
                        try
                        {
                            Trace.TraceInformation("ProcessJob: Starting processing on Delete job {0}", jobInfo.Id);
                            var storeManager = GetStoreManager();
                            storeManager.DeleteStore(jobInfo.StoreId);
                            _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedOk, "Store deleted.");
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("ProcessJob: DeleteStore {0} failed with exception {1}", jobInfo.StoreId,
                                             ex);
                            _jobQueue.FailWithException(jobInfo.Id, "Delete failed", ex);
                        }
                        break;
                    }

                default:
                    // TODO: Implement me
                    Thread.Sleep(1000);
                    _jobQueue.UpdateStatus(jobInfo.Id, "20% Complete");
                    Thread.Sleep(1000);
                    _jobQueue.UpdateStatus(jobInfo.Id, "40% Complete");
                    Thread.Sleep(1000);
                    _jobQueue.UpdateStatus(jobInfo.Id, "60% Complete");
                    Thread.Sleep(1000);
                    _jobQueue.UpdateStatus(jobInfo.Id, "80% Complete");
                    Thread.Sleep(1000);
                    _jobQueue.CompleteJob(jobInfo.Id, JobStatus.CompletedOk, "Completed without any processing");
                    break;
            }
        }

        private static bool TryDeserialize<T>(string jsonData, out T deserializedObject)
        {
            try
            {
                var ser = new JavaScriptSerializer();
                deserializedObject = ser.Deserialize<T>(jsonData);
                return true;
            }
            catch (Exception)
            {
                deserializedObject = default(T);
                return false;
            }
        }

        public override void OnStop()
        {
            Trace.TraceInformation("Role OnStop invoked.");
            _stopping = true;
            AzureBlockStore.Instance.Shutdown();
            base.OnStop();
        }

        public override bool OnStart()
        {
            try
            {
                // Set the maximum number of concurrent connections 
                ServicePointManager.DefaultConnectionLimit = 12;

                RoleStartupHelper.StartDiagnostics();
                InitializeBlockStorage();
                StartBlockUpdateService();
                StartStoreWorkerService();
                StoreConfiguration.DefaultStoreConfiguration.StoreManagerType = typeof (AzureStoreManager);
                var jobQueueConnectionString = RoleEnvironment.GetConfigurationSettingValue("ManagementDatabaseConnectionString");
                Trace.TraceInformation("Initializing SQL job queue");
                _jobQueue = new SqlJobQueue(jobQueueConnectionString, RoleEnvironment.CurrentRoleInstance.Id);
                Trace.TraceInformation("Job queue initialized");
                EventLog.WriteEntry("AzureEventLogs",
                    "Worker role started",
                    EventLogEntryType.Information);
                return base.OnStart();
            }
            catch (Exception ex)
            {
                Trace.TraceError("WorkerRole OnStart failed: " + ex);
                EventLog.WriteEntry("AzureEventLogs",
                                    "OnStart failed for StoreWorker role: Exception:" + ex.GetType().FullName +
                                    ", Message:" + ex.Message,
                                    EventLogEntryType.Error, 0);
                Trace.TraceError("Sleeping for long enough to allow errors to flush");
                Thread.Sleep(TimeSpan.FromMinutes(1.5));
                Trace.TraceError("Aborting role startup");
                return false;
            }
        }

        private void StartBlockUpdateService()
        {
            try
            {
                Trace.TraceInformation("Starting BlockUpdateService listener");
                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["BlockUpdateService"].IPEndpoint;
                var baseAddress = new Uri(String.Format("http://{0}", endpoint));
                Trace.TraceInformation("BlockUpdateService base address: " + baseAddress);
                var basicHttpBinding = new BasicHttpContextBinding
                                           {
                                               TransferMode = TransferMode.StreamedResponse,
                                               MaxReceivedMessageSize = Int32.MaxValue,
                                               SendTimeout = TimeSpan.FromMinutes(10),
                                               ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                                               HostNameComparisonMode = HostNameComparisonMode.Exact,
                                           };
                _blockUpdateService = new ServiceHost(new BlockUpdateService(), baseAddress);
                Trace.TraceInformation("Created service host");
                _blockUpdateService.AddServiceEndpoint(typeof (IBlockUpdateService), basicHttpBinding, String.Empty);
                Trace.TraceInformation("Added service endpoint");
                var throttlingBehavior = new ServiceThrottlingBehavior { MaxConcurrentCalls = int.MaxValue };
                _blockUpdateService.Description.Behaviors.Add(throttlingBehavior);
                Trace.TraceInformation("Updated throttling behavior");
                _blockUpdateService.Open();
                Trace.TraceInformation("BlockUpdateService started listening on endpoint http://{0}", endpoint);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting BlockUpdateService: " + ex);
                throw;
            }
        }

        private void StartStoreWorkerService()
        {
            try
            {
                Trace.TraceInformation("Starting StoreWorkerService listener");
                var endpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["StoreWorkerService"].IPEndpoint;
                var baseAddress = new Uri(String.Format("http://{0}", endpoint));
                var basicHttpBinding = new BasicHttpContextBinding
                                           {
                                               TransferMode = TransferMode.StreamedResponse,
                                               MaxReceivedMessageSize = Int32.MaxValue,
                                               SendTimeout = TimeSpan.FromMinutes(10),
                                               ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                                               HostNameComparisonMode = HostNameComparisonMode.Exact
                                           };
                _storeWorkerService = new ServiceHost(new StoreWorkerService(), baseAddress);
                _storeWorkerService.AddServiceEndpoint(typeof (IStoreWorkerService), basicHttpBinding, String.Empty);
                var throttlingBehavior = new ServiceThrottlingBehavior { MaxConcurrentCalls = int.MaxValue };
                _storeWorkerService.Description.Behaviors.Add(throttlingBehavior);
                _storeWorkerService.Open();
                Trace.TraceInformation("StoreWorkerService started listening on endpoint http://{0}", endpoint);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error starting StoreWorkerService: " + ex);
                throw;
            }
        }

        private static void InitializeBlockStorage()
        {
            try
            {
                Trace.TraceInformation("Initializing block storage");
                AzureBlockStore.Initialize(GetBlockStoreConfiguration());
                Trace.TraceInformation("BlockStorage initialized.");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error initializing block storage: {0}", ex);
                throw;
            }
        }

        private static AzureBlockStoreConfiguration GetBlockStoreConfiguration()
        {
            try
            {
                var connectionString =
                    RoleEnvironment.GetConfigurationSettingValue(AzureConstants.BlockStoreConnectionStringName);
                var szMemoryCache =
                    RoleEnvironment.GetConfigurationSettingValue(AzureConstants.BlockStoreMemoryCacheSizeName);
                int memoryCacheSize = Convert.ToInt32(szMemoryCache);
                return new AzureBlockStoreConfiguration
                           {
                               LocalStorageKey = AzureConstants.BlockStoreLocalStorageName,
                               MemoryCacheInMB = memoryCacheSize,
                               ConnectionString = connectionString
                           };
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error initializing block store configuration: {0}", ex);
                throw;
            }
        }
    }
}
