using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server;
using BrightstarDB.Storage;

namespace BrightstarDB.Service
{
    [ServiceBehavior(Namespace = "http://www.networkedplanet.com/services/core/", 
                     InstanceContextMode = InstanceContextMode.Single, 
                     ConcurrencyMode = ConcurrencyMode.Multiple,
                     IncludeExceptionDetailInFaults = true)]
    public sealed class BrightstarService : IBrightstarService
    {
        /// <summary>
        /// The server core for handling requests
        /// </summary>
        private static ServerCore _serverCore;
        private static readonly object _lock = new object();

        private static ServerCore ServerCore
        {
            get
            {
                lock (_lock)
                {
                    if (_serverCore == null)
                    {
                        if (Configuration.StoreLocation == null)
                        {
                            throw new BrighstarConfigurationException("No store configuration defined.");
                        }
                        _serverCore = new ServerCore(Configuration.StoreLocation, Configuration.QueryCache, Configuration.PersistenceType);
                    }
                    return _serverCore;
                }
            }
        }

        public BrightstarService()
        {
        }

        // [PrincipalPermission(SecurityAction.Demand, Role = "BrightstarReader")]
        public IEnumerable<string> ListStores()
        {
            try
            {
                return ServerCore.ListStores();
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error Listing Stores");
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
                ServerCore.CreateStore(storeName, Configuration.PersistenceType);
                Logging.LogInfo("Created store {0} with persistence type {1}", storeName, Configuration.PersistenceType);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Creating Store {0}", storeName);
                throw new BrightstarClientException("Error creating store " + storeName + ". " + ex.Message, ex);                
            }
        }

        /// <summary>
        /// Create a new store with a specific persistence type
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="persistenceType"></param>
        public void CreateStoreWithPersistenceType(string storeName, PersistenceType persistenceType)
        {
            try
            {
                if (String.IsNullOrEmpty(storeName))
                {
                    throw new BrightstarClientException("Store name cannot be NULL or an empty string.");
                }
                if (!Regex.IsMatch(storeName, Constants.StoreNameRegex))
                {
                    throw new BrightstarClientException(Strings.BrightstarServiceClient_InvalidStoreName);
                }
                ServerCore.CreateStore(storeName, persistenceType);
                Logging.LogInfo("Created store {0} with persistence type {1}", storeName, persistenceType);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error Creating Store {0}", storeName);
                throw new BrightstarClientException("Error creating store " + storeName + ". " + ex.Message, ex);
            }
        }

        public void DeleteStore(string storeName)
        {
            try
            {
                ServerCore.DeleteStore(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error Deleting Store {0}", storeName);
                throw new BrightstarClientException("Error deleting store " + storeName + ". " + ex.Message, ex);
            }
        }

        public bool DoesStoreExist(string storeName)
        {
            try
            {
                return ServerCore.DoesStoreExist(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error checking if store exists. Store {0}", storeName);
                throw new BrightstarClientException("Error checking if store exists store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">Store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="defaultGraphUris">The URIs of the graphs to be treated as the default graph for this query. If NULL the built-in default graph will be used.</param>
        /// <param name="ifNotModifiedSince">If the store has not been modified since the date/time specified by this parameter, 
        /// a StoreNotModified exception will be raised.</param>
        /// <param name="resultsMediaType">OPTIONAL: The media type to use for serializing the results</param>
        /// <returns>A stream containing the serialized query results</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, string[] defaultGraphUris, DateTime? ifNotModifiedSince, string resultsMediaType = null)
        {
            try
            {
                var resultsFormat = resultsMediaType == null ? SparqlResultsFormat.Xml : SparqlResultsFormat.GetResultsFormat(resultsMediaType);
                var pStream = new MemoryStream();
                ServerCore.Query(storeName, queryExpression, defaultGraphUris, ifNotModifiedSince, resultsFormat, pStream);
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
                throw new BrightstarClientException("Store not modified", ex);
                //throw new BrightstarClientException("Store not modified", new ExceptionDetail(ex));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error Executing Query {0} {1}", storeName,
                                 queryExpression);
                throw new BrightstarClientException(
                    "Error querying store " + storeName + " with expression " + queryExpression + ". " + ex.Message, ex);
            }
        }


        /// <summary>
        /// Queries a given commit point of a store using a SPARQL query
        /// </summary>
        /// <param name="commitPoint">The store commit point to be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUris">The URIs of the graphs to be treated as the default graph for this query. If NULL the built-in default graph will be used.</param>
        /// <param name="resultsMediaType">OPTIONAL: The media type to use for serializing the results</param>
        /// <returns>A stream containing the serialized query results</returns>
        public Stream ExecuteQueryOnCommitPoint(CommitPointInfo commitPoint, string queryExpression, string[] defaultGraphUris, string resultsMediaType = null)
        {
            try
            {
                var resultsFormat = resultsMediaType == null ? SparqlResultsFormat.Xml : SparqlResultsFormat.GetResultsFormat(resultsMediaType);
                var pstream = new MemoryStream();
                var t = new Task(() => ServerCore.Query(commitPoint.StoreName, commitPoint.Id, queryExpression, defaultGraphUris, resultsFormat, pstream));
                t.Start();
                t.Wait();
                if (t.IsFaulted && t.Exception != null) throw t.Exception;
                var cStream = new MemoryStream(pstream.GetBuffer(), 0, (int) pstream.Length);
                return cStream;
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error executing query {0}@{1} {2}", commitPoint.StoreName, commitPoint.Id, queryExpression);
                throw new BrightstarClientException(
                    String.Format("Error executing query {0}@{1} {2}. {3}", commitPoint.StoreName, commitPoint.Id,
                                  queryExpression, ex.Message), ex);
            }

        }

        public JobInfo ExecuteTransaction(string storeName,string preconditions, string deletePatterns, string insertData, string graphUri)
        {
            try
            {
                var jobId = ServerCore.ProcessTransaction(storeName, preconditions, deletePatterns, insertData, graphUri);
                return new JobInfo { JobId = jobId.ToString(), JobPending = true };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error queueing transaction {0} {1} {2}", storeName, deletePatterns, insertData);
                throw new BrightstarClientException("Error queueing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }

        public JobInfo StartExport(string storeName, string fileName, string graphUri)
        {
            try
            {
                // TODO: use graphUri to limit export
                var jobId = ServerCore.Export(storeName, fileName, graphUri);
                return new JobInfo {JobId = jobId.ToString(), JobPending = true};
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error queing export job {0} {1}", storeName, fileName);
                throw new BrightstarClientException("Error queing export job in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Starts a SPARQL Update job
        /// </summary>
        /// <param name="store">The store to be updated</param>
        /// <param name="updateExpression">A SPARQL Update expression</param>
        /// <returns>A JobInfo instance to monitor the status of the job</returns>
        public JobInfo ExecuteUpdate(string store, string updateExpression)
        {
            try
            {
                var jobId = ServerCore.ExecuteUpdate(store, updateExpression);
                return new JobInfo {JobId = jobId.ToString(), JobPending = true};
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error queing update job {0} '{1}'", store,
                                 updateExpression);
                throw new BrightstarClientException("Error queing update job in store " + store + ". " + ex.Message, ex);
            }
        }

        public JobInfo ConsolidateStore(string store)
        {
            var jobId = ServerCore.Consolidate(store);
            return new JobInfo { JobId = jobId.ToString(), JobPending = true };
        }

        /*
        public Stream GetStoreData(string storeName)
        {
            try
            {
                var connStream = new ConnectionStream();
                var cStream = new ConsumerStream(connStream);
                var pStream = new ProducerStream(connStream);

                var t = new Task(() => ServerCore.ExportStoreData(storeName, pStream));
                t.Start();

                return cStream;
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting data for store {0}", storeName);
                throw new BrightstarClientException("Error getting data for store " + storeName + ". " + ex.Message, ex);
            }
        }
        */

        public JobInfo GetJobInfo(string storeName, string jobId)
        {
            try
            {
                var jobStatus  = ServerCore.GetJobStatus(storeName, jobId);
                return new JobInfo { JobId = jobId, StatusMessage = jobStatus.Information,
                                                    ExceptionInfo = jobStatus.ExceptionDetail,
                                                    JobPending = (jobStatus.JobStatus == JobStatus.Pending),
                                                    JobCompletedOk = (jobStatus.JobStatus  == JobStatus.CompletedOk),
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

        public JobInfo StartImport(string storeName, string fileName, string graphUri)
        {
            try
            {
                if (graphUri == null) graphUri = Constants.DefaultGraphUri;
                var jobId = ServerCore.Import(storeName, fileName, graphUri);
                return new JobInfo { JobId = jobId.ToString(), JobPending = true };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error queing import job {0} {1}", storeName, fileName);
                throw new BrightstarClientException("Error queing import job in store " + storeName + ". " + ex.Message, ex);
            }
        }

        public IEnumerable<CommitPointInfo> GetCommitPoints(string storeName, int skip = 0, int take = 50)
        {
            try
            {
                var commitPoints = ServerCore.GetCommitPoints(storeName).Skip(skip).Take(take);
                return commitPoints.Select(c => new CommitPointInfo() { StoreName = storeName, Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId});
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }

        public IEnumerable<CommitPointInfo> GetCommitPointsInDateRange(string storeName, DateTime latest, DateTime earliest, int skip, int take)
        {
            try
            {
                DateTime latestUtc = latest.ToUniversalTime();
                DateTime earliestUtc = earliest.ToUniversalTime();
                var commitPoints =
                    ServerCore.GetCommitPoints(storeName).SkipWhile(x => x.CommitTime > latestUtc).TakeWhile(
                        x => x.CommitTime < earliestUtc).Skip(skip).Take(take);
                return commitPoints.Select(c => new CommitPointInfo() { StoreName = storeName, Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId });
            }
            catch(Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error getting commit points for store {0} with date range {1} to {2}", storeName, latest, earliest);
                throw new BrightstarClientException(
                    "Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Returns the commit point that was effective at the specified date/time
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="timestamp">The date/time timestamp</param>
        /// <returns>The commit point that was effective at <paramref name="timestamp"/> or null if 
        /// the store was created after <paramref name="timestamp"/> or if the store history has been subsequently removed by a coalesce operation.</returns>
        public CommitPointInfo GetCommitPoint(string storeName, DateTime timestamp)
        {
            var c = ServerCore.GetCommitPoints(storeName).Where(cp => cp.CommitTime.Equals(timestamp)).FirstOrDefault();
            return new CommitPointInfo
                       {StoreName = storeName, Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId};
        }

        public void RevertToCommitPoint(string storeName, CommitPointInfo commitPoint)
        {
            try
            {
                ServerCore.RevertToCommitPoint(storeName, commitPoint.Id);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }            
        }

        public IEnumerable<TransactionInfo> GetTransactions(string storeName, int skip, int take)
        {
            try
            {
                return ServerCore.GetTransactions(storeName).Skip(skip).Take(take)
                    .Select(t => new TransactionInfo
                                     {
                                         StoreName = storeName,
                                         Id=t.DataStartPosition,
                                         StartTime = t.TransactionStartTime,
                                         JobId= t.JobId,
                                         TransactionType = t.TransactionType,
                                         Status = t.TransactionStatus,
                                     });
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }

        public JobInfo ReExecuteTransaction(string storeName, TransactionInfo transactionInfo)
        {
            try
            {
                var tInfo = transactionInfo;
                var jobId = _serverCore.ReExecuteTransaction(storeName, tInfo.Id, tInfo.TransactionType);
                return new JobInfo { JobId = jobId.ToString(), JobPending = true };
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarDB.BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }
    }
}
