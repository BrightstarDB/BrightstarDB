#if !REST_CLIENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Storage;
using BrightstarDB.Server;
#if !SILVERLIGHT
using System.ServiceModel;

#endif

namespace BrightstarDB.Client
{
    /// <summary>
    /// An implementation of the Brightstar Service that uses the local filestore.
    /// </summary>
    public class EmbeddedBrightstarService : IBrightstarService
    {
        private readonly ServerCore _serverCore;

        /// <summary>
        /// For an embedded service connection, this property is always null.
        /// </summary>
        public DateTime? LastResponseTimestamp { get { return null; } }

        /// <summary>
        /// Create a new instance of the service that attaches to the specified directory location
        /// </summary>
        /// <param name="baseLocation">The full path to the location of the directory that contains one or more Brightstar stores</param>
        /// <remarks>The embedded server is thread-safe but doesn't support concurrent access to the same base location by multiple
        /// instances. You should ensure in your code that only one EmbeddedBrightstarService instance is connected to any given base location
        /// at a given time.</remarks>
        public EmbeddedBrightstarService(string baseLocation)
        {
            _serverCore = ServerCoreManager.GetServerCore(baseLocation);
        }

        #region Implementation of IBrightstarService

        /// <summary>
        /// List the names of the stores managed by this brightstar instance
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ListStores()
        {
            try
            {
                return _serverCore.ListStores().ToArray();
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Listing Stores");
                throw new BrightstarClientException("Error listing stores." + ex.Message, ex);
            }
        }

        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName">The name of the store to create.</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">Raised if <paramref name="storeName"/> is an empty string or does not match the allowed regular expression production for store names.</exception>
        /// <remarks>See <see cref="Constants.StoreNameRegex"/> for the regular expression used to validate store names.</remarks>
        public void CreateStore(string storeName)
        {
            CreateStore(storeName, Configuration.PersistenceType);
        }

        /// <summary>
        /// Create a new store with a specific persistence type for the main indexes
        /// </summary>
        /// <param name="storeName">The name of the store to create.</param>
        /// <param name="persistenceType">The type of persistence to use for the main store indexes</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">Raised if <paramref name="storeName"/> is an empty string or does not match the allowed regular expression production for store names.</exception>
        /// <remarks>See <see cref="Constants.StoreNameRegex"/> for the regular expression used to validate store names.</remarks>
        public void CreateStore(string storeName, PersistenceType persistenceType)
        {
            try
            {
                if (storeName == null)
                    throw new ArgumentNullException("storeName", Strings.BrightstarServiceClient_StoreNameMustNotBeNull);
                if (String.IsNullOrEmpty(storeName))
                    throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString,
                                                "storeName");
                if (!System.Text.RegularExpressions.Regex.IsMatch(storeName, Constants.StoreNameRegex))
                {
                    throw new ArgumentException(Strings.BrightstarServiceClient_InvalidStoreName, "storeName");
                }
                _serverCore.CreateStore(storeName, persistenceType);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Creating Store {0}", storeName);
                throw new BrightstarClientException("Error creating store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to delete.</param>
        public void DeleteStore(string storeName)
        {
            try
            {
                _serverCore.DeleteStore(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Deleting Store {0}", storeName);
                throw new BrightstarClientException("Error deleting store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Checks to see if the named store already exists
        /// </summary>
        /// <param name="storeName">store to check</param>
        /// <returns>true if store exists, false if not.</returns>
        public bool DoesStoreExist(string storeName)
        {
            try
            {
                return _serverCore.DoesStoreExist(storeName);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error checking if store exists. Store {0}", storeName);
                throw new BrightstarClientException("Error checking if store exists store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">Store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="ifNotModifiedSince">OPTIONAL: If the store has not been modified since the time specified by this parameter, 
        /// the query is not executed and a <see cref="BrightstarStoreNotModifiedException"/> is raised instead.</param>
        /// <param name="resultsFormat">OPTIONAL: The format to use for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/>.</param>
        /// <returns>A stream containing SPARQL query results in the requested format</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, DateTime? ifNotModifiedSince = null, SparqlResultsFormat resultsFormat = null)
        {
            if (storeName == null) throw new ArgumentNullException("storeName");
            if (queryExpression == null) throw new ArgumentNullException("queryExpression");
            if (resultsFormat == null) resultsFormat = SparqlResultsFormat.Xml;

            try
            {
                var pStream = new MemoryStream();
#if SILVERLIGHT
                var t = new Thread(ExecuteQuery);
                t.Start(new QueryParams(storeName, queryExpression, ifNotModifiedSince, resultsFormat, pStream));
                t.Join();
#else
                var t = new Task(() => _serverCore.Query(storeName, queryExpression, ifNotModifiedSince, resultsFormat, pStream));
                t.Start();
                t.Wait();
                if (t.IsFaulted && t.Exception != null)
                {
                    throw t.Exception;
                }
#endif

                pStream.Seek(0, SeekOrigin.Begin);
                return pStream;
            }
            catch (BrightstarStoreNotModifiedException ex)
            {
                throw new BrightstarClientException("Store not modified", new ExceptionDetail(ex));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Executing Query {0} {1}", storeName, queryExpression);
                throw new BrightstarClientException("Error querying store " + storeName + " with expression " + queryExpression + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="resultsFormat">The format to use for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression, SparqlResultsFormat resultsFormat = null)
        {
            if (queryExpression == null) throw new ArgumentNullException("queryExpression");
            if (resultsFormat == null) resultsFormat = SparqlResultsFormat.Xml;

            try
            {
                var pStream = new MemoryStream();
#if SILVERLIGHT
                var t = new Thread(ExecuteQuery);
                t.Start(new QueryParams(commitPoint, queryExpression, resultsFormat, pStream));
                t.Join();
#else
                var t =
                    new Task(() => _serverCore.Query(commitPoint.StoreName, commitPoint.Id, queryExpression, resultsFormat, pStream));
                t.Start();
                t.Wait();
                if (t.IsFaulted && t.Exception != null)
                {
                    throw t.Exception;
                }
#endif
                pStream.Seek(0, SeekOrigin.Begin);
                return pStream;
            }
            catch (AggregateException aggregateException)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error querying store {0}@{1} with expression {2}", commitPoint.StoreName,
                                 commitPoint.Id, queryExpression);
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    throw new BrightstarClientException(
                        String.Format("Error querying store {0}@{1} with expression {2}. {3}",
                                      commitPoint.StoreName, commitPoint.Id, queryExpression,
                                      aggregateException.InnerExceptions[0].Message));
                }
                var messages = String.Join("; ", aggregateException.InnerExceptions.Select(ex => ex.Message));
                throw new BrightstarClientException(
                    String.Format(
                        "Error querying store {0}@{1} with expression {2}. Multiple errors occurred: {3}",
                        commitPoint.StoreName, commitPoint.Id, queryExpression, messages));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error querying store {0}@{1} with expression {2}", commitPoint.StoreName,
                                 commitPoint.Id, queryExpression);
                throw new BrightstarClientException(
                    String.Format("Error querying store {0}@{1} with expression {2}. {3}", commitPoint.StoreName,
                                  commitPoint.Id, queryExpression, ex.Message), ex);
            }
        }

        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <returns>Job Info</returns>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, string deletePatterns, string insertData, bool waitForCompletion = true)
        {
            try
            {
                if (!waitForCompletion)
                {
                    var jobId = _serverCore.ProcessTransaction(storeName, preconditions, deletePatterns, insertData);
                    return new JobInfoWrapper(new JobInfo { JobId = jobId.ToString(), JobPending = true });
                }
                else
                {
                    var jobId = _serverCore.ProcessTransaction(storeName,preconditions, deletePatterns, insertData);
                    JobExecutionStatus status = _serverCore.GetJobStatus(storeName, jobId.ToString());
                    while (status.JobStatus != JobStatus.CompletedOk && status.JobStatus != JobStatus.TransactionError)
                    {
                        Thread.Sleep(50);
                        status = _serverCore.GetJobStatus(storeName, jobId.ToString());
                    }
                    return new JobInfoWrapper(new JobInfo
                                                  {
                                                      JobId = jobId.ToString(),
                                                      StatusMessage = status.Information,
                                                      JobCompletedOk = (status.JobStatus == JobStatus.CompletedOk),
                                                      JobCompletedWithErrors =
                                                          (status.JobStatus == JobStatus.TransactionError)
                                                  });
                }

            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error Queing Transaction {0} {1} {2}", storeName, deletePatterns, insertData);
                throw new BrightstarClientException("Error queing transaction in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Execute a SPARQL Update expression against a store
        /// </summary>
        /// <param name="storeName">The name of the store to be updated</param>
        /// <param name="updateExpression">The SPARQL Update expression to be applied</param>
        /// <param name="waitForCompletion">If set to true, the method will block until the transaction completes</param>
        /// <returns>A <see cref="JobInfo"/> instance for monitoring the status of the job</returns>
        public IJobInfo ExecuteUpdate(string storeName, string updateExpression, bool waitForCompletion = true)
        {
            try
            {
                if (!waitForCompletion)
                {
                    var jobId = _serverCore.ExecuteUpdate(storeName, updateExpression);
                    return new JobInfoWrapper(new JobInfo{JobId=jobId.ToString(), JobPending = true});
                } else
                {
                    var jobId = _serverCore.ExecuteUpdate(storeName, updateExpression);
                    JobExecutionStatus status = _serverCore.GetJobStatus(storeName, jobId.ToString());
                    while (status.JobStatus != JobStatus.CompletedOk && status.JobStatus != JobStatus.TransactionError)
                    {
                        Thread.Sleep(50);
                        status = _serverCore.GetJobStatus(storeName, jobId.ToString());
                    }
                    return new JobInfoWrapper(new JobInfo
                    {
                        JobId = jobId.ToString(),
                        StatusMessage = status.Information,
                        JobCompletedOk = (status.JobStatus == JobStatus.CompletedOk),
                        JobCompletedWithErrors =
                            (status.JobStatus == JobStatus.TransactionError)
                    });
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing SPARQL update {0} {1}", storeName, updateExpression);
                throw new BrightstarClientException("Error queing SPARQL update in store " + storeName + ". " + ex.Message, ex);
            }
        }

#if SILVERLIGHT
        
        private void ExecuteQuery(object param)
        {
            var queryParams = param as QueryParams;
            if (queryParams != null)
            {
                if (queryParams.WithCommitPoint)
                {
                    _serverCore.Query(queryParams.StoreName, queryParams.CommitPointId, queryParams.QueryExpression, queryParams.ResultsFormat,
                                      queryParams.OutputStream);
                }
                else
                {
                    _serverCore.Query(queryParams.StoreName, queryParams.QueryExpression, queryParams.IfNotModifiedSince, queryParams.ResultsFormat, queryParams.OutputStream);
                }
            }
        }

        class QueryParams
        {
            public string StoreName { get; private set; }
            public ulong CommitPointId { get; private set; }
            public bool WithCommitPoint { get; private set; }
            public string QueryExpression { get; private set; }
            public Stream OutputStream { get; private set; }
            public DateTime? IfNotModifiedSince { get; private set; }
            public SparqlResultsFormat ResultsFormat { get; private set; }

            public QueryParams(string storeName, string queryExpression, DateTime? ifNotModifiedSince, SparqlResultsFormat resultsFormat, Stream outputStream)
            {
                StoreName = storeName;
                QueryExpression = queryExpression;
                OutputStream = outputStream;
                WithCommitPoint = false;
                IfNotModifiedSince = ifNotModifiedSince;
                ResultsFormat = resultsFormat ?? SparqlResultsFormat.Xml;
            }

            public QueryParams(ICommitPointInfo commitPoint, string queryExpression, SparqlResultsFormat resultsFormat, Stream outputStream)
            {
                StoreName = commitPoint.StoreName;
                CommitPointId = commitPoint.Id;
                WithCommitPoint = true;
                QueryExpression = queryExpression;
                OutputStream = outputStream;
                ResultsFormat = resultsFormat ?? SparqlResultsFormat.Xml;
            }
        }
#endif

        /// <summary>
        /// Gets the information about a job. Including status and any messages.
        /// </summary>
        /// <param name="storeName">Name of the store where the job is running</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo GetJobInfo(string storeName, string jobId)
        {
            try
            {
                var jobStatus = _serverCore.GetJobStatus(storeName, jobId);
                return
                new JobInfoWrapper(
                    new JobInfo
                        {
                            JobId = jobId,
                            StatusMessage = jobStatus.Information,
                            ExceptionInfo = jobStatus.ExceptionDetail,
                            JobPending = (jobStatus.JobStatus == JobStatus.Pending),
                            JobCompletedOk = (jobStatus.JobStatus == JobStatus.CompletedOk),
                            JobCompletedWithErrors = (jobStatus.JobStatus == JobStatus.TransactionError),
                            JobStarted = (jobStatus.JobStatus == JobStatus.Started)
                        });
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting data job info {0} {1}", storeName, jobId);
                throw new BrightstarClientException("Error getting job info in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Starts an import job.
        /// </summary>
        /// <param name="storeName">The store to perform the import to</param>
        /// <param name="fileName">The name of the file in brighhtstar\import folder to import.</param>
        /// <param name="graphUri">The URI of the graph that the data will be imported into.</param>
        /// <returns>An IJobInfo instance</returns>
        public IJobInfo StartImport(string storeName, string fileName, string graphUri = Constants.DefaultGraphUri)
        {
            try
            {
                var jobId = _serverCore.Import(storeName, fileName, graphUri);
                return new JobInfoWrapper(new JobInfo { JobId = jobId.ToString(), JobPending = true });
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing import job {0} {1}", storeName, fileName);
                throw new BrightstarClientException("Error queing import job in store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Starts an export job
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. This file will be overwritten if it already exists.</param>
        /// <param name="graphUri">The URI of the graph to be exported. If NULL, all graphs in the store are exported.</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartExport(string store, string fileName, string graphUri)
        {
            try
            {
                var jobId = _serverCore.Export(store, fileName, graphUri);
                return new JobInfoWrapper(new JobInfo {JobId = jobId.ToString(), JobPending = true});
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing export job {0} {1}", store,
                                 fileName);
                throw new BrightstarClientException("Error queing export job in store " + store + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Creates a new data file for the specified store that contains only the data required for the current state.
        /// </summary>
        /// <param name="store">Store name</param>
        /// <returns>An IJobInfo instance</returns>
        public IJobInfo ConsolidateStore(string store)
        {
            try
            {
                var jobId = _serverCore.Consolidate(store);
                return new JobInfoWrapper(new JobInfo { JobId = jobId.ToString(), JobPending = true });
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error queing consolidate job {0}", store);
                throw new BrightstarClientException("Error queing export job in store " + store + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// returns commit points in batches 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="skip">How many commit points to skip over</param>
        /// <param name="take">Max allowed value is 100</param>
        /// <returns>A enumeration of commit points.</returns>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, int skip = 0, int take = 100)
        {
            try
            {
                if (take > 100) take = 100;
                var commitPoints = _serverCore.GetCommitPoints(storeName).Skip(skip).Take(take);
// ReSharper disable RedundantEnumerableCastCall
// not redundant for SILVERLIGHT build
                return commitPoints.Select(c => new CommitPointInfoWrapper(new CommitPointInfo {Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId, StoreName = storeName})).Cast<ICommitPointInfo>();
// ReSharper restore RedundantEnumerableCastCall
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Get a list of commit points that lie within a specified date/time range
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="latest">The latest commit date of commit points to be retrieved</param>
        /// <param name="earliest">The earliest commit date of the commit points to be retrieved</param>
        /// <param name="skip">The offset into the results list to return from</param>
        /// <param name="take">The number of results to return</param>
        /// <returns></returns>
        /// <remarks>A maximum of 100 commit points are returned in a batch, if <paramref name="take"/> is greater than 100 its value is ignored and only 100 results returned.</remarks>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, DateTime latest, DateTime earliest, int skip, int take)
        {
            try
            {
                if (take > 100) take = 100;
                DateTime latestUtc = latest.ToUniversalTime();
                DateTime earliestUtc = earliest.ToUniversalTime();
                var commitPoints =
                    _serverCore.GetCommitPoints(storeName).SkipWhile(x => x.CommitTime > latestUtc).TakeWhile(
                        x => x.CommitTime > earliestUtc).Skip(skip).Take(take);
// ReSharper disable RedundantEnumerableCastCall
// not redundant for SILVERLIGHT build
                return commitPoints.Select(c => new CommitPointInfoWrapper(new CommitPointInfo {Id = c.LocationOffset, CommitTime = c.CommitTime, JobId = c.JobId, StoreName = storeName})).Cast<ICommitPointInfo>();
// ReSharper restore RedundantEnumerableCastCall
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting commit points for store {0}", storeName);
                throw new BrightstarClientException("Error getting commit points for store " + storeName + ". " + ex.Message, ex);
            }
        }


        /// <summary>
        /// Returns the commit point that was in effect at a given date/time
        /// </summary>
        /// <param name="storeName">The name of the store to search</param>
        /// <param name="timestamp">The time to search for</param>
        /// <returns>An ICommitPointInfo representing the latest commit point in the store that was committed before the date/time specified by <paramref name="timestamp"/>. If
        /// there is no such commit point (either because the store was created after that date/time or the store has been coalesced and historical data removed),
        /// this method will return null.
        /// </returns>
        public ICommitPointInfo GetCommitPoint(string storeName, DateTime timestamp)
        {
            try
            {
                var commitPoint = _serverCore.GetCommitPoint(storeName, timestamp);
                if (commitPoint == null) return null;
                return new CommitPointInfoWrapper(new CommitPointInfo
                                                      {
                                                          StoreName = storeName,
                                                          Id=commitPoint.LocationOffset,
                                                          CommitTime = commitPoint.CommitTime,
                                                          JobId = commitPoint.JobId
                                                      });
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException,
                                 "Error getting commit point at date/time {0} for store {1}", timestamp, storeName);
                throw new BrightstarClientException(
                    String.Format("Error getting commit point at date/time {0} for store {1}. {2}", timestamp, storeName,
                                  ex.Message), ex);
            }
        }

        /// <summary>
        /// This will make the commit point provided the new latest commit point. Blocks until the operation is complete.
        /// </summary>
        /// <param name="storeName">Name of the store</param>
        /// <param name="commitPoint">Commit point to revert to.</param>
        public void RevertToCommitPoint(string storeName, ICommitPointInfo commitPoint)
        {
            try
            {
                _serverCore.RevertToCommitPoint(storeName, commitPoint.Id);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error reverting to commit point {0} for store {1}",commitPoint.Id, storeName);
                throw new BrightstarClientException(
                    String.Format("Error reverting to commit point {0} for store {1}. {2}", commitPoint.Id, storeName,
                                  ex.Message), ex);
            }   
        }

        /// <summary>
        /// Gets a list of transations
        /// </summary>
        /// <param name="storeName">Name of store</param>
        /// <param name="skip">Number of transactions to skip</param>
        /// <param name="take">Number of transaction to return</param>
        /// <returns></returns>
        public IEnumerable<ITransactionInfo> GetTransactions(string storeName, int skip, int take)
        {
            try
            {
                return _serverCore.GetTransactions(storeName).Skip(skip).Take(take)
                    .Select(t => new TransactionInfoWrapper(
                                     new TransactionInfo
                                         {
                                             Id = t.DataStartPosition,
                                             JobId = t.JobId,
                                             StartTime = t.TransactionStartTime,
                                             StoreName = storeName,
#if SILVERLIGHT
                                             Status = (BrightstarTransactionStatus)((int)t.TransactionStatus),
                                             TransactionType = (BrightstarTransactionType)((int)t.TransactionType)
#else
                                             Status = (TransactionStatus)((int)t.TransactionStatus),
                                             TransactionType = (TransactionType)((int)t.TransactionType)
#endif
// ReSharper disable RedundantEnumerableCastCall
// Cast is not redundant for SILVERLIGHT builds
                                         })).Cast<ITransactionInfo>();
// ReSharper restore RedundantEnumerableCastCall
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error getting transactions for store {0}", storeName);
                throw new BrightstarClientException("Error getting transactions for store " + storeName + ". " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Executes a previous transaction
        /// </summary>
        /// <param name="storeName">Name of the store</param>
        /// <param name="transactionInfo">Transaction to execute.</param>
        public IJobInfo ReExecuteTransaction(string storeName, ITransactionInfo transactionInfo)
        {
            try
            {
                var tInfoWrapper = transactionInfo as TransactionInfoWrapper;
                if (tInfoWrapper == null) throw new ArgumentException("Invalid TransactionInfo object received.", "transactionInfo");
                var tInfo = tInfoWrapper.TransactionInfo;
                var jobId = _serverCore.ReExecuteTransaction(storeName, tInfo.Id, (Storage.TransactionType)((int) tInfo.TransactionType));
                return new JobInfoWrapper(new JobInfo { JobId = jobId.ToString(), JobPending = true });
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.ServerCoreException, "Error rexecuting transaction with JobId {0} for store {1}", transactionInfo.JobId, storeName);
                throw new BrightstarClientException(String.Format("Error rexecuting transaction with JobId {0} for store {1}. {2}", transactionInfo.JobId, storeName, ex.Message), ex);
            }
        }

        #endregion

        /// <summary>
        /// Shutsdown this embedded service only.
        /// </summary>
        /// <param name="completeJobs">If true then the call will block until all jobs queued by the service
        /// have been completed.</param>
        public void Shutdown(bool completeJobs)
        {
            _serverCore.Shutdown(completeJobs);
        }
    }
}
#endif