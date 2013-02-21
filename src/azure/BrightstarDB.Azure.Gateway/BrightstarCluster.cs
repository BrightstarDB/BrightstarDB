using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.StoreWorkerClient;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.ServiceModel;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.Gateway
{
    public class BrightstarCluster
    {
        private readonly List<Tuple<string, IStoreWorkerService>> _clients;
        private readonly CloudBlobClient _blobClient;
        private readonly IJobQueue _jobQueue;
        private int _nextClient;
        private const long MegaBytesToBytes = 1024*1024;

        public static BrightstarCluster Instance { get; private set; }

        static BrightstarCluster()
        {
            Instance = new BrightstarCluster();
        }

        private BrightstarCluster()
        {
            try
            {
                CloudStorageAccount.SetConfigurationSettingPublisher((key, publisher) => publisher(RoleEnvironment.GetConfigurationSettingValue(key)));
                _clients = new List<Tuple<string, IStoreWorkerService>>();
                UpdateClientList();
                var storageAccount = CloudStorageAccount.FromConfigurationSetting(AzureConstants.BlockStoreConnectionStringName);
                _blobClient = storageAccount.CreateCloudBlobClient();
                RoleEnvironment.Changing += HandleRoleEnvironmentChanging;
                _jobQueue =
                    new SqlJobQueue(
                        RoleEnvironment.GetConfigurationSettingValue(AzureConstants.ManagementDatabaseConnectionStringName),
                        RoleEnvironment.CurrentRoleInstance.Id);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error initializing BrightstarCluster: {0}", ex);
            }
        }

        private void HandleRoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            foreach(var c in e.Changes.OfType<RoleEnvironmentTopologyChange>())
            {
                if (c.RoleName.Equals(AzureConstants.StoreWorkerRoleName))
                {
                    Trace.TraceInformation("BrightstarCluster: Received RoleEnvironmentTopologyChange event. Updating store worker client list");
                    UpdateClientList();
                }
            }
        }

        private void UpdateClientList()
        {
            foreach (var workerInstance in RoleEnvironment.Roles[AzureConstants.StoreWorkerRoleName].Instances)
            {
                AddClient(workerInstance);
            }            
        }

        private void AddClient(RoleInstance workerInstance)
        {
            var binding = new BasicHttpContextBinding
                              {
                                  TransferMode = TransferMode.StreamedResponse,
                                  MaxReceivedMessageSize = Int32.MaxValue,
                                  SendTimeout = TimeSpan.FromMinutes(10),
                                  ReaderQuotas = XmlDictionaryReaderQuotas.Max,
                                  HostNameComparisonMode = HostNameComparisonMode.Exact
                              };
            var endpointUri = String.Format("http://{0}", workerInstance.InstanceEndpoints["StoreWorkerService"].IPEndpoint);
            var endpointAddress = new EndpointAddress(endpointUri);
            var client = new StoreWorkerServiceClient(binding, endpointAddress);
            lock (_clients)
            {
                _clients.RemoveAll(t => t.Item1.Equals(workerInstance.Id));
                _clients.Add(new Tuple<string, IStoreWorkerService>(workerInstance.Id, client));
            }
        }

        private IStoreWorkerService NextClient()
        {
            lock(_clients)
            {
                _nextClient++;
                if (_nextClient >= _clients.Count)
                {
                    _nextClient = 0;
                }
                return _clients[_nextClient].Item2;
            }
        }

        public string ExecuteQuery(string storeId, string query)
        {
            int retries = 3;
            Exception lastException = null;
            while (retries > 0)
            {
                try
                {
                    var client = NextClient();
                    return client.ExecuteQuery(storeId, query);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    retries--;
                    if (retries > 0)
                    {
                        Trace.TraceError("Error executing query : {0}. Retrying...", ex);
                    }
                    else
                    {
                        Trace.TraceError("Query execution failed: {0}. No more retries. Propagating exception", ex);
                    }
                }
            }
            throw lastException;
        }

        public List<string> GetStores()
        {
            return _blobClient.ListContainers(AzureConstants.StoreContainerPrefix).Select(c => c.Name.Substring(AzureConstants.StoreContainerPrefix.Length)).ToList();
        }

        public void CreateStore(string storeId)
        {
            var client = NextClient();
            client.CreateStore(storeId);
        }

        public bool DeleteStore(string storeId)
        {
            var client = NextClient();
            return client.DeleteStore(storeId);
        }

        /// <summary>
        /// Creates a copy of a store as a new, separate store
        /// </summary>
        /// <param name="fromStoreId"></param>
        /// <param name="toStoreId"></param>
        /// <returns></returns>
        public bool CopyStore(string fromStoreId, string toStoreId)
        {
            var fromContainer = _blobClient.GetContainerReference(AzureConstants.StoreContainerPrefix + fromStoreId);
            var toContainer = _blobClient.GetContainerReference(AzureConstants.StoreContainerPrefix + toStoreId);
            toContainer.CreateIfNotExist();
            var srcMasterBlob = fromContainer.GetPageBlobReference("masterfile.bs");
            var targetMasterBlob = toContainer.GetPageBlobReference("masterfile.bs");
            targetMasterBlob.Create(AzureConstants.StoreBlobSize);
            targetMasterBlob.CopyFromBlob(srcMasterBlob);
            var srcDataBlob = fromContainer.GetPageBlobReference("data.bs");
            var targetDataBlob = toContainer.GetPageBlobReference("data.bs");
            targetDataBlob.Create(AzureConstants.StoreBlobSize);
            targetDataBlob.CopyFromBlob(srcDataBlob);
            return true;
        }

        public JobInfo GetJobInfo(string storeId, string jobId)
        {
            return _jobQueue.GetJob(storeId, jobId);
        }

        public string StartJob(string storeId, JobType jobType, string jobData)
        {
            return _jobQueue.QueueJob(storeId, jobType, jobData, null);
        }

        public string StartUpdateTransaction(string storeId, string preconditions, string deleteTriples, string insertTriples)
        {
            var jobData = String.Join(AzureConstants.TransactionSeparator,
                                      preconditions, deleteTriples, insertTriples);
            return _jobQueue.QueueJob(storeId, JobType.Transaction,jobData, null);
        }

        public byte[] GetMasterFile(string storeId)
        {
            return GetStoreBytes(storeId, "masterfile.bs");
        }

        public byte[] GetDataFile(string storeId)
        {
            return GetStoreBytes(storeId, "data.bs");
        }

        private byte[] GetStoreBytes(string storeId, string blobName)
        {
            var blobContainer = _blobClient.GetContainerReference(AzureConstants.StoreContainerPrefix + storeId);
            var dataBlob = blobContainer.GetBlobReference(blobName);
            dataBlob.FetchAttributes();
            long blobLength = long.Parse(dataBlob.Attributes.Metadata[AzureConstants.BlobDataLengthPropertyName]);
            var buff = new byte[blobLength];
            using(var blobStream = dataBlob.OpenRead())
            {
                blobStream.Read(buff, 0, (int)blobLength);
            }
            return buff;
        }

        public DateTime GetLastModifiedDate(string storeName)
        {
            try
            {
                var blobContainer = _blobClient.GetContainerReference(AzureConstants.StoreContainerPrefix + storeName);
                var dataBlob = blobContainer.GetBlobReference("data.bs");
                dataBlob.FetchAttributes();
                return dataBlob.Attributes.Properties.LastModifiedUtc;
                /* Could use this code to take into account jobs that have completed but not yet committed data to the blob,
                 * but for now I think it makes more sense to just use the timestamp on the last persistent write to the store
                var lastJob = _jobQueue.GetLastCommit(storeName);
                if (lastJob.ProcessingCompleted.HasValue && lastJob.ProcessingCompleted.Value > lastModified)
                {
                    lastModified=lastJob.ProcessingCompleted.Value;
                }
                return lastModified;
                 */
            }
            catch (StorageClientException ex)
            {
                if (ex.ErrorCode == StorageErrorCode.BlobNotFound || 
                    ex.ErrorCode == StorageErrorCode.ContainerNotFound ||
                    ex.ErrorCode == StorageErrorCode.ResourceNotFound)
                {
                    throw new StoreNotFoundException(storeName);
                }
                Trace.TraceError("Error retrieving last modified date for store '{0}': {1}", storeName, ex);
                throw;
            }
            catch(Exception ex)
            {
                Trace.TraceError("Error retrieving last modified date for store '{0}': {1}", storeName, ex);
                throw;
            }
        }

        public IEnumerable<JobInfo> GetJobs(string storeId)
        {
            return _jobQueue.GetJobs(storeId);
        }

        public int GetStoreSize(string storeName)
        {
            int totalSize = 0;
            var blobContainer = _blobClient.GetContainerReference(AzureConstants.StoreContainerPrefix + storeName);
            foreach (var blobItem in blobContainer.ListBlobs())
            {
                if (blobItem is CloudBlob)
                {
                    var cloudBlob = blobItem as CloudBlob;
                    cloudBlob.FetchAttributes(  );
                    try
                    {
                        totalSize +=
                            (int)
                            (long.Parse(cloudBlob.Attributes.Metadata[AzureConstants.BlobDataLengthPropertyName])/
                             MegaBytesToBytes);
                    }
                    catch (Exception)
                    {
                        // No length property on the blob ?
                    }
                }
            }
            return totalSize;
        }
    }
}