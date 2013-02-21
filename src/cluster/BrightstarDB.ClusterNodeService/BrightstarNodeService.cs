using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using BrightstarDB.Client;
using BrightstarDB.ClusterNode;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using CommitPointInfo = BrightstarDB.Service.CommitPointInfo;
using IBrightstarService = BrightstarDB.Service.IBrightstarService;
using JobInfo = BrightstarDB.Service.JobInfo;
using TransactionInfo = BrightstarDB.Service.TransactionInfo;

namespace BrightstarDB.ClusterNodeService
{
    [ServiceBehavior(Namespace = "http://www.networkedplanet.com/services/core/",
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        IncludeExceptionDetailInFaults = true)]
    internal class BrightstarNodeService : IBrightstarService
    {
        private static int ClusterNodePort;
        private static string StoreLocation;

        private static NodeCore _nodeCore;
        private static readonly object _lock = new object();

        private static NodeCore NodeCore
        {
            get
            {
                lock (_lock)
                {
                    if (_nodeCore == null)
                    {
                        if (StoreLocation != null && ClusterNodePort != 0)
                        {
                            _nodeCore = new NodeCore(StoreLocation);
                            _nodeCore.Start(ClusterNodePort);
                        }
                        else
                        {
                            if (Configuration.StoreLocation == null)
                            {
                                throw new Exception("No store configuration defined.");
                            }
                            _nodeCore = new NodeCore(Configuration.StoreLocation);
                            _nodeCore.Start(Configuration.ClusterNodePort);
                        }
                    }
                    return _nodeCore;
                }
            }
        }

        public BrightstarNodeService(string storeLocation, int port)
        {
            Logging.LogInfo("Starting Node Core on port {0}", port);
            StoreLocation = storeLocation;
            ClusterNodePort = port;
            // call this to get the core properly started
            var nd = NodeCore;
        }

        public BrightstarNodeService()
        {
            var nd = NodeCore;
        }

        public void Stop()
        {
            NodeCore.Stop();
        }

        public IEnumerable<string> ListStores()
        {
            try {
                return NodeCore.ListStores();
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Listing Stores");
                throw new BrightstarClientException("Error listing stores." + ex.Message, ex);
            }
        }

        public void CreateStore(string storeName)
        {
            try
            {
                if (string.IsNullOrEmpty(storeName))
                {
                    throw new BrightstarClientException("Store name cannot be NULL or an empty string.");
                }
                if (!Regex.IsMatch(storeName, Constants.StoreNameRegex))
                {
                    throw new BrightstarClientException(Strings.BrightstarServiceClient_InvalidStoreName);
                }
                NodeCore.CreateStore(storeName);
                Logging.LogInfo("Created store {0} with persistence type {1}", storeName, Configuration.PersistenceType);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Creating Store {0}", storeName);
                throw new BrightstarClientException("Error creating store " + storeName + ". " + ex.Message, ex);
            }
        }

        public void CreateStoreWithPersistenceType(string storeName, PersistenceType persistenceType)
        {
            throw new NotImplementedException();
        }

        public void DeleteStore(string storeName)
        {
            try {
                NodeCore.DeleteStore(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Deleting Store {0}", storeName);
                throw new BrightstarClientException("Error deleting store " + storeName + ". " + ex.Message, ex);
            }
        }

        public bool DoesStoreExist(string storeName)
        {
            return NodeCore.DoesStoreExist(storeName);
        }

        public Stream ExecuteQuery(string storeName, string queryExpression, DateTime? ifNotModifiedSince, string resultsMediaType = null)
        {
            try {
                var resultsFormat = resultsMediaType == null ? SparqlResultsFormat.Xml : SparqlResultsFormat.GetResultsFormat(resultsMediaType);
                var pStream = new MemoryStream();
                NodeCore.ProcessQuery(storeName, queryExpression, ifNotModifiedSince, resultsFormat, pStream);
                var cStream = new MemoryStream(pStream.GetBuffer(), 0, (int)pStream.Length);
                return cStream;
            }
            catch (AggregateException aggregateException)
            {
                if (aggregateException.InnerException is BrightstarStoreNotModifiedException)
                {
                    throw new BrightstarClientException("Store not modified", aggregateException.InnerException);
                }
                throw new BrightstarClientException("Error executing query", aggregateException.InnerException);
            }
            catch (BrightstarStoreNotModifiedException ex)
            {
                throw new BrightstarClientException("Store not modified", new ExceptionDetail(ex));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error Executing Query {0} {1}", storeName,
                                 queryExpression);
                throw new BrightstarClientException(
                    "Error querying store " + storeName + " with expression " + queryExpression + ". " + ex.Message, ex);
            }
        }

        public Stream ExecuteQueryOnCommitPoint(CommitPointInfo commitPoint, string queryExpression, string resultsMediaType = null)
        {
            throw new NotImplementedException();
        }

        public JobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns, string insertData)
        {
            try
            {
                var ctxn = new ClusterUpdateTransaction()
                               {
                                   StoreId = storeName,
                                   Deletes = deletePatterns,
                                   Inserts = insertData,
                                   Preconditions = preconditions
                               };
                var jobId = NodeCore.ProcessTransaction(ctxn);
                return new JobInfo { JobId = jobId.ToString(), JobPending = true };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error queueing transaction {0} {1} {2}", storeName, deletePatterns, insertData);
                throw new BrightstarClientException("Error queueing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }

        public JobInfo GetJobInfo(string storeName, string jobId)
        {
            try
            {
                var jobStatus = NodeCore.GetJobStatus(storeName, jobId);
                return new JobInfo
                {
                    JobId = jobId,
                    StatusMessage = jobStatus.Information,
                    ExceptionInfo = jobStatus.ExceptionDetail,
                    JobPending = (jobStatus.JobStatus == JobStatus.Pending),
                    JobCompletedOk = (jobStatus.JobStatus == JobStatus.CompletedOk),
                    JobCompletedWithErrors = (jobStatus.JobStatus == JobStatus.TransactionError),
                    JobStarted = (jobStatus.JobStatus == JobStatus.Started)
                };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error getting data job info {0} {1}", storeName, jobId);
                throw new BrightstarClientException("Error getting job info in store " + storeName + ". " + ex.Message, ex);
            }
        }

        public JobInfo StartImport(string store, string fileName, string graphUri = Constants.DefaultGraphUri)
        {
            throw new NotImplementedException();
        }

        public JobInfo StartExport(string store, string fileName, string graphUri = null)
        {
            throw new NotImplementedException();
        }

        public JobInfo ExecuteUpdate(string storeName, string updateExpression)
        {
            try
            {
                var ctxn = new ClusterSparqlTransaction()
                {
                    StoreId = storeName,
                    Expression = updateExpression
                };
                var jobId = NodeCore.ProcessUpdate(ctxn);
                return new JobInfo { JobId = jobId.ToString(), JobPending = true };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error queueing sparql transaction {0} {1}", storeName, updateExpression);
                throw new BrightstarClientException("Error queueing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }

        public JobInfo ConsolidateStore(string store)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CommitPointInfo> GetCommitPoints(string storeName, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<CommitPointInfo> GetCommitPointsInDateRange(string storeName, DateTime latest, DateTime earliest, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public CommitPointInfo GetCommitPoint(string storeName, DateTime timestamp)
        {
            throw new NotImplementedException();
        }

        public void RevertToCommitPoint(string storeName, CommitPointInfo commitPoint)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<TransactionInfo> GetTransactions(string storeName, int skip, int take)
        {
            throw new NotImplementedException();
        }

        public JobInfo ReExecuteTransaction(string storeName, TransactionInfo transactionInfo)
        {
            throw new NotImplementedException();
        }

        ~BrightstarNodeService()
        {
            NodeCore.Stop();
        }
    }
}
