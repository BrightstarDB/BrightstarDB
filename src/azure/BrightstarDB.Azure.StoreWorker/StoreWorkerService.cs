using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.Common.Logging;
using BrightstarDB.Storage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.StoreWorker
{

    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class StoreWorkerService : IStoreWorkerService
    {

        #region Implementation of IStoreWorkerService

        /// <summary>
        /// Create a new B* store.
        /// </summary>
        /// <param name="storeId">The new store ID</param>
        /// <returns>True if the store was created successfully, false otherwise</returns>
        public bool CreateStore(string storeId)
        {
            try
            {
                IStoreManager storeManager = GetStoreManager();
                storeManager.CreateStore(storeId, false).Close();
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(String.Format("StoreWorkerService.CreateStore failed with exception: {0}", ex));
                throw;
            }
        }

        private static IStoreManager GetStoreManager()
        {
            var config = StoreConfiguration.DefaultStoreConfiguration;
            config.DisableBackgroundWrites = true;
            return new AzureStoreManager(config);
        }

        /// <summary>
        /// Execute a SPARQL query against a B* store
        /// </summary>
        /// <param name="storeId">The ID of the store to be queried</param>
        /// <param name="query">The SPARQL query to be executed.</param>
        /// <returns>The SPARQL query result</returns>
        public string ExecuteQuery(string storeId, string query)
        {
            try
            {
                IStoreManager storeManager = GetStoreManager();
                var store = storeManager.OpenStore(storeId, true);
                var start = DateTime.UtcNow;
                long rowCount;
                var ret = store.ExecuteSparqlQuery(query, out rowCount);
                var end = DateTime.UtcNow;
                var queryLogEntity = new QueryLogEntity(storeId, query, rowCount, end.Subtract(start).TotalSeconds);
                var logTask = new Task(LogQuery, queryLogEntity);
                logTask.Start();
                return ret;
            } catch(Exception ex)
            {
                Trace.TraceError(String.Format("StoreWorkerService.ExecuteQuery failed with exception: {0}", ex));
                throw;
            }
        }

        public void LogQuery(object state)
        {
            try
            {
                var logEntity = state as QueryLogEntity;
                if (logEntity != null)
                {
                    var connectionString =
                        RoleEnvironment.GetConfigurationSettingValue(AzureConstants.DiagnosticsConnectionStringName);
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                    CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                    tableClient.CreateTableIfNotExist("queries");
                    TableServiceContext serviceContext = tableClient.GetDataServiceContext();
                    serviceContext.AddObject("queries", logEntity);
                    serviceContext.SaveChangesWithRetries();
                }
            }
            catch (Exception ex)
            {
                // Log error in logging query stats
                Trace.TraceError("StoreWorkerService.LogQuery: Error logging query to table storage: {0}", ex);
            }
        }

        public bool DeleteStore(string storeId)
        {
            try
            {
                IStoreManager storeManager = GetStoreManager();
                storeManager.DeleteStore(storeId);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("StoreWorkerService.DeleteStore failed with exception: {0}", ex);
                return false;
            }
        }


        #endregion

    }
}