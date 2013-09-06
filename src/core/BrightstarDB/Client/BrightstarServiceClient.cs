#if !REST_CLIENT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using BrightstarDB.Caching;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Main point of client access to a remote Brightstar Service
    /// </summary>
    internal class BrightstarServiceClient : IBrightstarService
    {
        private readonly IBrightstarWcfService _service;
        private readonly ICache _queryCache;

        public DateTime? LastResponseTimestamp { get; private set; }

        
        internal BrightstarServiceClient(IBrightstarWcfService wcfService, ICache queryCache)
        {
            _service = wcfService;
            var client = wcfService as BrightstarWcfServiceClient;
            if (client != null)
            {
                client.Endpoint.Behaviors.Add(new BrightstarServiceClientInspectorBehavior(HandleMessageHeader));
            }
            _queryCache = queryCache;
        }

        private void HandleMessageHeader(DateTime? timestamp)
        {
            LastResponseTimestamp = timestamp;
        }

        private static void ValidateStoreName(string storeName)
        {
            if (storeName == null)
            {
                throw new ArgumentNullException("storeName", Strings.BrightstarServiceClient_StoreNameMustNotBeNull);
            }
            if (String.Empty.Equals(storeName))
            {
                throw new ArgumentException(Strings.BrightstarServiceClient_StoreNameMustNotBeEmptyString, "storeName");
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(storeName, Constants.StoreNameRegex))
            {
                throw new ArgumentException(Strings.BrightstarServiceClient_InvalidStoreName, "storeName");
            }
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
                return _service.ListStores();
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Create a new store
        /// </summary>
        /// <param name="storeName">The name to assign to the new store</param>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="storeName"/> is NULL</exception>
        /// <exception cref="ArgumentException">Raised if <paramref name="storeName"/> is an empty string or does not match the allowed regular expression production for store names.</exception>
        /// <remarks>See <see cref="Constants.StoreNameRegex"/> for the regular expression used to validate store names.</remarks>
        public void CreateStore(string storeName)
        {
            ValidateStoreName(storeName);
            _service.CreateStore(storeName);
        }

        public void CreateStore(string storeName, PersistenceType persistenceType)
        {
            ValidateStoreName(storeName);
            _service.CreateStore(storeName); // TODO: Pass through persistence type
        }
        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName"></param>
        public void DeleteStore(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                _service.DeleteStore(storeName);
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Checks to see if the named store already exists
        /// </summary>
        /// <param name="storeName">store to check</param>
        /// <returns>true if store exists, false if not.</returns>
        public bool DoesStoreExist(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                return _service.DoesStoreExist(storeName);
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }


        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression,
                                   DateTime? ifNotModifiedSince = null, SparqlResultsFormat resultsFormat = null)
        {
            return ExecuteQuery(storeName, queryExpression, (IEnumerable<string>) null, ifNotModifiedSince,
                                resultsFormat);
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">The name of the store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="defaultGraphUri">The URI of the graph that will be the default graph for the query</param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        public Stream ExecuteQuery(string storeName, string queryExpression, string defaultGraphUri,
                                   DateTime? ifNotModifiedSince = null, SparqlResultsFormat resultsFormat = null)
        {
            if (defaultGraphUri == null)
            {
                throw new ArgumentNullException("defaultGraphUri", Strings.BrightstarServiceClient_QueryDefaultGraphUriMustNotBeNull);
            }
            return ExecuteQuery(storeName, queryExpression, new string[] {defaultGraphUri}, ifNotModifiedSince,
                                resultsFormat);
        }

        /// <summary>
        /// Query the store using a SPARQL query
        /// </summary>
        /// <param name="storeName">Store to query</param>
        /// <param name="queryExpression">SPARQL query string</param>
        /// <param name="defaultGraphUris"></param>
        /// <param name="ifNotModifiedSince">OPTIONAL : If this parameter is provided and the store has not been changed since the time specified,
        /// a BrightstarClientException will be raised with the message "Store not modified".</param>
        /// <param name="resultsFormat">OPTIONAL: The serialization format for the SPARQL results set. Defaults to <see cref="SparqlResultsFormat.Xml"/>.</param>
        /// <returns>A stream containing XML SPARQL result XML</returns>
        /// <remarks>If the <paramref name="ifNotModifiedSince"/> parameter is used by an application, then the default caching provided
        /// by this class will be bypassed</remarks>
        public Stream ExecuteQuery(string storeName, string queryExpression, IEnumerable<string> defaultGraphUris, DateTime? ifNotModifiedSince = null, SparqlResultsFormat resultsFormat = null)
        {
            ValidateStoreName(storeName);
            if (queryExpression == null)
                throw new ArgumentNullException("queryExpression", Strings.BrightstarServiceClient_QueryMustNotBeNull);
            if (String.Empty.Equals(queryExpression))
                throw new ArgumentException(Strings.BrightstarServiceClient_QueryMustNotBeEmptyString, "queryExpression");

            var g = defaultGraphUris == null ? null : defaultGraphUris.ToArray();
            string cacheKey = null;
            CachedQueryResult cachedResult = null;
            if (ifNotModifiedSince == null && _queryCache != null)
            {
                cacheKey = storeName + "_" + queryExpression.GetHashCode();
                if (defaultGraphUris != null)
                {
                    cacheKey = cacheKey + "_" + String.Join(",", g).GetHashCode();
                }
                cachedResult = _queryCache.Lookup<CachedQueryResult>(cacheKey);
                if (cachedResult != null)
                {
                    ifNotModifiedSince = cachedResult.Timestamp;
                }
            }

            try
            {
                var mediaType = resultsFormat == null
                                    ? SparqlResultsFormat.Xml.MediaTypes.First() + "; charset=utf-8"
                                    : resultsFormat.ToString();
                var resultStream = _service.ExecuteQuery(storeName, queryExpression, defaultGraphUris == null ? null : g.ToArray(), ifNotModifiedSince, mediaType);
                if (_queryCache != null && cacheKey != null && LastResponseTimestamp.HasValue)
                {
                    using (var streamReader = new StreamReader(resultStream))
                    {
                        var resultString = streamReader.ReadToEnd();
                        cachedResult = new CachedQueryResult(LastResponseTimestamp.Value, resultString);
                        _queryCache.Insert(cacheKey, cachedResult, CachePriority.Normal);
                        return new MemoryStream(streamReader.CurrentEncoding.GetBytes(cachedResult.Result));
                    }
                }
                else
                {
                    return resultStream;
                }
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                if (cachedResult != null)
                {
                    if (fault.Detail.Type.Equals("BrightstarDB.Client.BrightstarClientException") &&
                        fault.Detail.InnerException != null &&
                        fault.Detail.InnerException.Type.Equals("BrightstarDB.BrightstarStoreNotModifiedException"))
                    {
                        // Cached result is still fine
                        return new MemoryStream(
                            resultsFormat == null
                                ? Encoding.UTF8.GetBytes(cachedResult.Result)
                                : resultsFormat.Encoding.GetBytes(cachedResult.Result));
                    }
                }
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression,
                                   SparqlResultsFormat resultsFormat = null)
        {
            return ExecuteQuery(commitPoint, queryExpression, (IEnumerable<string>) null, resultsFormat);
        }

        /// <summary>
        /// Query a specific commit point of a store
        /// </summary>
        /// <param name="commitPoint">The commit point be queried</param>
        /// <param name="queryExpression">The SPARQL query string</param>
        /// <param name="defaultGraphUri">The URI of the default graph for the query</param>
        /// <param name="resultsFormat">OPTIONAL: Specifies the serialization format for the SPARQL results. Defaults to <see cref="SparqlResultsFormat.Xml"/></param>
        /// <returns>A stream containing XML SPARQL results</returns>
        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression,
                                   string defaultGraphUri, SparqlResultsFormat resultsFormat = null)
        {
            return ExecuteQuery(commitPoint, queryExpression, new[] {defaultGraphUri}, resultsFormat);
        }

        public Stream ExecuteQuery(ICommitPointInfo commitPoint, string queryExpression, IEnumerable<string> defaultGraphUris, SparqlResultsFormat resultsFormat = null)
        {
            if (commitPoint == null) throw new ArgumentNullException("commitPoint");
            if (queryExpression == null)
                throw new ArgumentNullException("queryExpression", Strings.BrightstarServiceClient_QueryMustNotBeNull);
            if (String.Empty.Equals(queryExpression))
                throw new ArgumentException(Strings.BrightstarServiceClient_QueryMustNotBeEmptyString, "queryExpression");
            try
            {
                return _service.ExecuteQueryOnCommitPoint(
                    new CommitPointInfo
                        {
                            Id = commitPoint.Id,
                            StoreName = commitPoint.StoreName,
                            CommitTime = commitPoint.CommitTime,
                            JobId = commitPoint.JobId
                        },
                        queryExpression, 
                        defaultGraphUris == null ? null : defaultGraphUris.ToArray(),
                        resultsFormat == null ? SparqlResultsFormat.Xml.MediaTypes.First() + "; charset=utf-8" : resultsFormat.ToString());
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Execute an update transaction.
        /// </summary>
        /// <param name="storeName">The name of the store to modify</param>
        /// <param name="preconditions">NTriples that must be in the store in order for the transaction to execute</param>
        /// <param name="deletePatterns">The delete patterns that will be removed from the store</param>
        /// <param name="insertData">The NTriples data that will be inserted into the store.</param>
        /// <param name="defaultGraphUri">The URI of the default graph that the transaction will be applied to.</param>
        /// <param name="waitForCompletion">If set to true the method will block until the transaction completes</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo ExecuteTransaction(string storeName, string preconditions, 
            string deletePatterns, string insertData, string defaultGraphUri, bool waitForCompletion = true)
        {
            ValidateStoreName(storeName);
            try
            {
                if (!waitForCompletion)
                {
                    return new JobInfoWrapper(_service.ExecuteTransaction(storeName, preconditions, deletePatterns, insertData, defaultGraphUri));
                }
                else
                {
                    var jobInfo = _service.ExecuteTransaction(storeName, preconditions, deletePatterns, insertData, defaultGraphUri);
                    while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
                    {
                        Thread.Sleep(50);
                        jobInfo = _service.GetJobInfo(storeName, jobInfo.JobId);
                    }
                    return new JobInfoWrapper(jobInfo);
                }
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
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
            ValidateStoreName(storeName);
            try
            {
                if (!waitForCompletion)
                {
                    return new JobInfoWrapper(_service.ExecuteUpdate(storeName, updateExpression));
                }
                else
                {
                    var jobInfo = _service.ExecuteUpdate(storeName, updateExpression);
                    while (!jobInfo.JobCompletedOk && !jobInfo.JobCompletedWithErrors)
                    {
                        Thread.Sleep(50);
                        jobInfo = _service.GetJobInfo(storeName, jobInfo.JobId);
                    }
                    return new JobInfoWrapper(jobInfo);
                }
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /*
        /// <summary>
        /// Returns all the triples in the named store
        /// </summary>
        /// <param name="storeName">Store to retrieve triples from</param>
        /// <returns>A stream containing triples encoded using NTriples format</returns>
        public Stream GetStoreData(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                return _service.GetStoreData(storeName);
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }
        */

        /// <summary>
        /// Gets the information about a job. Including status and any messages.
        /// </summary>
        /// <param name="storeName">Name of the store where the job is running</param>
        /// <param name="jobId">The Id of the job</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo GetJobInfo(string storeName, string jobId)
        {
            ValidateStoreName(storeName);
            if (jobId == null) throw new ArgumentNullException("jobId", Strings.BrightstarServiceClient_JobIdMustNotBeNull);
            if (String.Empty.Equals(jobId)) throw new ArgumentException(Strings.BrightstarServiceClient_JobIdMustNotBeEmptyString, "jobId");

            try
            {
                return new JobInfoWrapper(_service.GetJobInfo(storeName, jobId));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Starts an import job.
        /// </summary>
        /// <param name="store">The store to perform the import to</param>
        /// <param name="fileName">The name of the file in brighhtstar\import folder to import.</param>
        /// <param name="graphUri">OPTIONAL: The URI of the default graph to import the data into. Defaults to <see cref="Constants.DefaultGraphUri"/></param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartImport(string store, string fileName, string graphUri = Constants.DefaultGraphUri)
        {
            ValidateStoreName(store);
            if(fileName == null) throw new ArgumentNullException("fileName", Strings.BrightstarServiceClient_ImportFileNameMustNotBeNull);
            if (String.Empty.Equals(fileName)) throw new ArgumentException(Strings.BrightstarServiceClient_ImportFileNameMustNotBeEmptyString, "fileName");
            try
            {
                return new JobInfoWrapper(_service.StartImport(store, fileName, graphUri));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Starts an export job
        /// </summary>
        /// <param name="store">The store to export data from</param>
        /// <param name="fileName">The name of the file in the brightstar\import folder to write to. This file will be overwritten if it already exists.</param>
        /// <param name="graphUri">The URI of the graph to export. If NULL, all graphs are exported</param>
        /// <returns>A JobInfo instance</returns>
        public IJobInfo StartExport(string store, string fileName, string graphUri = null)
        {
            ValidateStoreName(store);
            if (fileName == null)
                throw new ArgumentNullException("fileName", Strings.BrightstarServiceClient_ExportFileNameMustNotBeNull);
            if (String.Empty.Equals(fileName))
                throw new ArgumentException(Strings.BrightstarServiceClient_ExportFileNameMustNotBeEmptyString,
                                            "fileName");
            try
            {
                return new JobInfoWrapper(_service.StartExport(store, fileName, graphUri));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Consolidates a store
        /// </summary>
        /// <param name="store">Name of store</param>
        /// <returns>JobInfo</returns>
        public IJobInfo ConsolidateStore(string store)
        {
            ValidateStoreName(store);
            try
            {
                return new JobInfoWrapper(_service.ConsolidateStore(store));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// returns commit points in batches 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="skip">How many commit points to skip over</param>
        /// <param name="take">Max allowed value is 100</param>
        /// <returns></returns>
        public IEnumerable<ICommitPointInfo> GetCommitPoints(string storeName, int skip, int take)
        {
            ValidateStoreName(storeName);
            if(take>100) throw new ArgumentOutOfRangeException("take", Strings.BrightstarServiceClient_GetCommitPoints_TakeToLarge);
            if (skip < 0) throw new ArgumentOutOfRangeException("skip", Strings.BrightstarServiceClient_SkipMustNotBeNegative);
            try
            {
                return _service.GetCommitPoints(storeName, skip, take).Select(x => new CommitPointInfoWrapper(x));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
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
            ValidateStoreName(storeName);
            if (take > 100)
                throw new ArgumentOutOfRangeException("take",
                                                      Strings.BrightstarServiceClient_GetCommitPoints_TakeToLarge);
            if (skip < 0)
                throw new ArgumentOutOfRangeException("skip", Strings.BrightstarServiceClient_SkipMustNotBeNegative);
            try
            {
                return
                    _service.GetCommitPointsInDateRange(storeName, latest, earliest, skip, take).Select(
                        x => new CommitPointInfoWrapper(x));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        public IStoreStatistics GetStatistics(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                var toWrap = _service.GetStatistics(storeName);
                return toWrap == null ? null : new StoreStatisticsWrapper(toWrap);
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        public IEnumerable<IStoreStatistics> GetStatistics(string storeName, DateTime latest, DateTime earlierst, int skip, int take)
        {
            ValidateStoreName(storeName);
            if (skip < 0) throw new ArgumentOutOfRangeException("skip", Strings.BrightstarServiceClient_SkipMustNotBeNegative);
            if (take > 100) throw new ArgumentOutOfRangeException("take", Strings.BrightstarServiceClient_GetStatistics_TakeTooLarge);
            try
            {
                return _service.GetStatisticsInDateRange(storeName, latest, earlierst, skip, take).Select(
                    x => new StoreStatisticsWrapper(x));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        public IJobInfo UpdateStatistics(string storeName)
        {
            ValidateStoreName(storeName);
            try
            {
                return new JobInfoWrapper(_service.UpdateStatistics(storeName));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
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
            ValidateStoreName(storeName);
            try
            {
                var c = _service.GetCommitPoint(storeName, timestamp);
                if (c == null) return null;
                return new CommitPointInfoWrapper(c);
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// This will make the commit point provided the new latest commit point. Blocks until the operation is complete.
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="commitPoint"></param>
        public void RevertToCommitPoint(string storeName, ICommitPointInfo commitPoint)
        {
            ValidateStoreName(storeName);
            if (commitPoint == null) throw new ArgumentNullException("commitPoint", Strings.BrightstarServiceClient_CommitPointMustNotBeNull);
            if (!(commitPoint is CommitPointInfoWrapper)) throw new ArgumentException(Strings.BrightstarServiceClient_InvalidCommitPointInfoObject, "commitPoint");
            try
            {
                _service.RevertToCommitPoint(storeName, (commitPoint as CommitPointInfoWrapper).CommitPointInfo);
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Gets a list of transations
        /// </summary>
        /// <param name="storeName">Name of store for </param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public IEnumerable<ITransactionInfo> GetTransactions(string storeName, int skip, int take)
        {
            ValidateStoreName(storeName);
            if (skip < 0) throw new ArgumentOutOfRangeException("skip", Strings.BrightstarServiceClient_SkipMustNotBeNegative);
            try
            {
                return _service.GetTransactions(storeName, skip, take).Select(x => new TransactionInfoWrapper(x));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        /// <summary>
        /// Executes a previous transaction
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="transactionInfo"></param>
        public IJobInfo ReExecuteTransaction(string storeName, ITransactionInfo transactionInfo)
        {
            ValidateStoreName(storeName);
            if(transactionInfo == null) throw  new ArgumentNullException("transactionInfo");
            if (!(transactionInfo is TransactionInfoWrapper)) throw new ArgumentException(Strings.BrightstarServiceClient_InvalidTransactionInfoObject, "transactionInfo");
            try
            {
                return
                    new JobInfoWrapper(_service.ReExecuteTransaction(storeName,
                                                                     (transactionInfo as TransactionInfoWrapper).
                                                                         TransactionInfo));
            }
            catch (FaultException<ExceptionDetail> fault)
            {
                throw new BrightstarClientException(fault);
            }
        }

        #endregion
    }
}
#endif