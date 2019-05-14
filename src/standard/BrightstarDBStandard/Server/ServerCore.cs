using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Caching;
using BrightstarDB.Client;
using BrightstarDB.Config;
using BrightstarDB.Query;
using BrightstarDB.Storage;
using System.Threading;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using ITransactionInfo = BrightstarDB.Storage.ITransactionInfo;
using TransactionType = BrightstarDB.Dto.TransactionType;
using Triple = BrightstarDB.Model.Triple;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Server
{
    internal delegate void JobCompletedDelegate(object sender, JobCompletedEventArgs e);

    internal class ServerCore
    {
        private readonly string _baseLocation;

        // maps logical store names to a store worker
        private readonly Dictionary<string, StoreWorker> _stores;

        private readonly IStoreManager _storeManager;

        private readonly ICache _queryCache;

        private readonly bool _enableTransactionLogging;

        public ServerCore(string baseLocation, ICache queryCache, PersistenceType persistenceType, bool enableTransactionLoggingOnNewStores)
        {
            Logging.LogInfo("ServerCore Initialised {0}", baseLocation);
            _baseLocation = baseLocation;
            _stores = new Dictionary<string, StoreWorker>();
            var configuration = StoreConfiguration.DefaultStoreConfiguration.Clone() as StoreConfiguration;
            configuration.PersistenceType = persistenceType;
            _storeManager = StoreManagerFactory.GetStoreManager(configuration);
            _queryCache = queryCache;
            _enableTransactionLogging = enableTransactionLoggingOnNewStores;
        }

        /// <summary>
        /// Event invoked when a transaction has completed processing successfully but
        /// begore the job status is updated to completed OK
        /// </summary>
        public event JobCompletedDelegate JobCompleted;

        public void Shutdown(bool completeJobs)
        {
            foreach (var worker in _stores.Values)
            {
                worker.Shutdown(completeJobs);
            }
        }

        public void ShutdownStore(string storeName, bool completeJobs)
        {
            var storeWorker = GetStoreWorker(storeName);
            storeWorker.Shutdown(true);
        }

        public IEnumerable<string> ListStores()
        {
            Logging.LogInfo("List Stores");
            return _storeManager.ListStores(_baseLocation);
        }

        /// <summary>
        /// Creates a new store and returns the store id
        /// </summary>
        /// <returns></returns>
        public string CreateStore()
        {
            Logging.LogInfo("Create Store");
            var sid = Guid.NewGuid().ToString();
            _storeManager.CreateStore(Path.Combine(_baseLocation, sid), true, _enableTransactionLogging);
            Logging.LogInfo("Store id is {0}", sid);
            return sid;
        }

        public string CreateStore(string storeName, PersistenceType persistenceType)
        {
            Logging.LogInfo("Create Store");
            var store = _storeManager.CreateStore(Path.Combine(_baseLocation, storeName), persistenceType, true, _enableTransactionLogging);
            store.Close();
            Logging.LogInfo("Store id is {0}", storeName);
            return storeName;
        }

        public bool DoesStoreExist(string storeName)
        {
            Logging.LogInfo("check store exists store {0}", storeName);
            return _storeManager.DoesStoreExist(Path.Combine(_baseLocation, storeName));
        }

#if PORTABLE
        public void DeleteStore(string storeName)
        {
            Logging.LogInfo("Delete store {0}", storeName);
            var storeWorker = GetStoreWorker(storeName);
            // remove store worker from collection
            RemoveStoreWorker(storeName);
            storeWorker.Shutdown(false, () => _storeManager.DeleteStore(Path.Combine(_baseLocation, storeName)));            
        }
#else
        public void DeleteStore(string storeName, bool waitForCompletion = true)
        {
            Logging.LogInfo("Delete store {0}", storeName);
            var storeWorker = GetStoreWorker(storeName);
            // remove store worker from collection
            RemoveStoreWorker(storeName);
            storeWorker.Shutdown(false, () => _storeManager.DeleteStore(_baseLocation + "\\" + storeName));

            if (waitForCompletion) 
            {
                while (DoesStoreExist(storeName))
                {
                    Thread.Sleep(10);
                }
            }
        }
#endif

        private void RemoveStoreWorker(string storeName)
        {
            lock (_stores)
            {
                if (_stores.ContainsKey(_baseLocation + "\\" + storeName))
                {
                    _stores.Remove(_baseLocation + "\\" + storeName);
                }
            }
        }

        private StoreWorker GetStoreWorker(string storeName)
        {
            lock (_stores)
            {
                StoreWorker result = null;
                if (_stores.TryGetValue(_baseLocation + "\\" + storeName, out result))
                {
                    return result;
                }
                if (!DoesStoreExist(storeName))
                {
                    throw new NoSuchStoreException(storeName);
                }
                // create store manager
                var storeWorker = new StoreWorker(_baseLocation , storeName);
                storeWorker.JobCompleted += HandleStoreWorkerJobCompleted;
                _stores.Add(_baseLocation + "\\" + storeName, storeWorker);
                storeWorker.Start();
                return storeWorker;
            }
        }

        private void HandleStoreWorkerJobCompleted(object sender, JobCompletedEventArgs e)
        {
            if (JobCompleted != null)
            {
                JobCompleted(this, e);
            }
        }

        // operations
        public ISerializationFormat Query(string storeName, string queryExpression, IEnumerable<string> defaultGraphUris,
                                          DateTime? ifNotModifiedSince,
                                          SparqlResultsFormat sparqlResultFormat, RdfFormat graphFormat,
                                          Stream responseStream)
        {
            Logging.LogDebug("Query {0} {1}", storeName, queryExpression);
            var commitPoint =
                _storeManager.GetMasterFile(Path.Combine(_baseLocation, storeName)).GetCommitPoints().First();
            if (ifNotModifiedSince.HasValue && ifNotModifiedSince > commitPoint.CommitTime)
            {
                throw new BrightstarStoreNotModifiedException();
            }
            var g = defaultGraphUris == null ? null : defaultGraphUris.Where(x=>!string.IsNullOrEmpty(x)).ToArray();
            if (g != null && g.Length == 0) g = null;
            var query = ParseSparql(queryExpression);
            var targetFormat = QueryReturnsGraph(query) ? (ISerializationFormat) graphFormat : sparqlResultFormat;

            var cacheKey = MakeQueryCacheKey(storeName, commitPoint.CommitTime.Ticks, queryExpression, g, targetFormat);
            var cachedResult = GetCachedResult(cacheKey);
            if (cachedResult == null)
            {
                // Not in the cache so execute the query on the StoreWorker
                var storeWorker = GetStoreWorker(storeName);
                var cacheStream = new MemoryStream();
                storeWorker.Query(query, targetFormat, cacheStream, g);
                cachedResult = CacheResult(cacheKey, cacheStream.ToArray());
            }
            cachedResult.WriteTo(responseStream);
            return targetFormat;
        }

        public ISerializationFormat Query(string storeName, ulong commitPointId, string queryExpression,
                                          IEnumerable<string> defaultGraphUris,
                                          SparqlResultsFormat sparqlResultFormat, RdfFormat graphFormat,
                                          Stream responseStream)
        {
            Logging.LogDebug("Query {0}@{1} {2}", storeName, commitPointId, queryExpression);
            var g = defaultGraphUris == null ? null : defaultGraphUris.ToArray();
            var storeLocation = Path.Combine(_baseLocation, storeName);
            var masterFile = _storeManager.GetMasterFile(storeLocation);
            if (masterFile.PersistenceType == PersistenceType.Rewrite)
            {
                throw new BrightstarClientException(
                    "Query of past commit points is not supported by the binary page persistence type");
            }
            var commitPoint =
                masterFile.GetCommitPoints().FirstOrDefault(c => c.LocationOffset == commitPointId);
            if (commitPoint == null)
            {
                throw new InvalidCommitPointException(String.Format("Could not find commit point {0} for store {1}.",
                                                                    commitPointId, storeName));
            }

            var query = ParseSparql(queryExpression);
            var targetFormat = QueryReturnsGraph(query) ? (ISerializationFormat)graphFormat : sparqlResultFormat;
            var cacheKey = MakeQueryCacheKey(storeName, commitPoint.CommitTime.Ticks, queryExpression, g, targetFormat);
            var cachedResult = GetCachedResult(cacheKey);
            if (cachedResult == null)
            {
                var storeWorker = GetStoreWorker(storeName);
                var cacheStream = new MemoryStream();
                storeWorker.Query(commitPointId, query, targetFormat, cacheStream, g);
                cachedResult = CacheResult(cacheKey, cacheStream.ToArray());
            }

            cachedResult.WriteTo(responseStream);
            return targetFormat;
        }

        private static bool QueryReturnsGraph(SparqlQuery query)
        {
            return (query.QueryType == SparqlQueryType.Construct ||
                    query.QueryType == SparqlQueryType.Describe ||
                    query.QueryType == SparqlQueryType.DescribeAll);
        }

        #region Query Caching

        
        private string MakeQueryCacheKey(string storeName, long commitTime, string query, string[] defaultGraphUris, ISerializationFormat targetFormat)
        {
            var graphHashCode = defaultGraphUris == null ? 0 : String.Join(",", defaultGraphUris.OrderBy(s=>s)).GetHashCode();
            return storeName + "_" + commitTime + "_" + query.GetHashCode() + "_" + graphHashCode + "_" + targetFormat;
        }



        private QueryCacheEntry GetCachedResult(string key)
        {
            var cacheData = _queryCache.Lookup(key);
            return cacheData == null ? null : new QueryCacheEntry(_queryCache.Lookup(key));
        }

        private QueryCacheEntry CacheResult(string key, byte[] data)
        {
            _queryCache.Insert(key, data, CachePriority.Normal);
            return new QueryCacheEntry(data);
        }

        #endregion


        public Guid ProcessTransaction(string storeName, string preconditions, string notExistsPreconditions, string deletePatterns, string insertData, string defaultGraphUri, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(storeName);
            return storeWorker.ProcessTransaction(preconditions, notExistsPreconditions, deletePatterns, insertData, defaultGraphUri, "nt", jobLabel);
        }

        public Guid Import(string storeName, string contentFileName, string graphUri, RdfFormat importFormat = null, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(storeName);
            return storeWorker.Import(contentFileName, graphUri, importFormat, jobLabel);
        }

        internal IEnumerable<JobExecutionStatus> GetJobs(string storeName)
        {
            var storeWorker = GetStoreWorker(storeName);
            return storeWorker.GetJobs();
        }

        public JobExecutionStatus GetJobStatus(string storeId, string jobId)
        {
            var storeWorker = GetStoreWorker(storeId);
            return storeWorker.GetJobStatus(jobId);
        }

        public Guid Export(string storeName, string fileName, string graphUri, RdfFormat exportFormat, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(storeName);
            return storeWorker.Export(fileName, graphUri, exportFormat, jobLabel);
        }

        public IEnumerable<Triple> GetResourceStatements(string storeId, string resourceUri)
        {
            var storeWorker = GetStoreWorker(storeId);
            return storeWorker.GetResourceStatements(resourceUri);                        
        }

        public IEnumerable<CommitPoint> GetCommitPoints(string storeId)
        {
            return _storeManager.GetMasterFile(Path.Combine(_baseLocation, storeId)).GetCommitPoints();
        }

        public void RevertToCommitPoint(string storeId, ulong commitPointLocation)
        {
            // TODO: Validate that this is a proper commit point location
            //var commitPoint = _storeManager.GetCommitPoint(_baseLocation + "\\" + storeId, commitPointLocation);
            var lastCommit = _storeManager.GetMasterFile(Path.Combine(_baseLocation, storeId)).GetLatestCommitPoint();
            var commitPoint = new CommitPoint(commitPointLocation, lastCommit.NextCommitNumber, DateTime.UtcNow, Guid.Empty);
            var storeWorker = GetStoreWorker(storeId);
            storeWorker.WriteStore.RevertToCommitPoint(commitPoint);
            storeWorker.InvalidateReadStore();
        }

        public IEnumerable<ITransactionInfo> GetTransactions(string storeId)
        {
            var transactionLog = _storeManager.GetTransactionLog(_baseLocation + "\\" + storeId);
            var transactionList = transactionLog.GetTransactionList();
            while (transactionList.MoveNext())
            {
                yield return transactionList.Current;
            }
        }

        /// <summary>
        /// Return an enumeration of the most recent transactions in the transaction log in 
        /// time order (oldest first)
        /// </summary>
        /// <param name="storeId">The store to read from</param>
        /// <param name="maxCount">The maximum number of transactions to return</param>
        /// <param name="ts">The maximum age of transaction to return</param>
        /// <returns>An enumeration of the <paramref name="maxCount"/> most recent transactions that match the age filter</returns>
        public List<ITransactionInfo> GetRecentTransactions(string storeId, int maxCount, TimeSpan ts)
        {
            var transactionLog = _storeManager.GetTransactionLog(Path.Combine(_baseLocation, storeId));
            return transactionLog.GetTransactionList(maxCount, ts);
        } 

        public Guid ReExecuteTransaction(string storeId, ulong dataStartPosition, TransactionType transactionType, string jobLabel=null)
        {
            var storeWorker = GetStoreWorker(storeId);
            var transactionLog = _storeManager.GetTransactionLog(_baseLocation + "\\" + storeId);
            var jobId = Guid.NewGuid();

            switch (transactionType)
            {
                case TransactionType.ImportJob:
                    var importJob = new ImportJob(jobId, jobLabel, storeWorker);
                    importJob.ReadTransactionDataFromStream(transactionLog.GetTransactionData(dataStartPosition));
                    storeWorker.QueueJob(importJob);
                    break;
                case TransactionType.UpdateTransaction:
                    var updateJob = new UpdateTransaction(jobId,jobLabel, storeWorker);
                    updateJob.ReadTransactionDataFromStream(transactionLog.GetTransactionData(dataStartPosition));
                    storeWorker.QueueJob(updateJob);
                    break;
                case TransactionType.SparqlUpdateTransaction:
                    var sparqlUpdateJob = new SparqlUpdateJob(jobId, jobLabel, storeWorker, null);
                    sparqlUpdateJob.ReadTransactionDataFromStream(transactionLog.GetTransactionData(dataStartPosition));
                    storeWorker.QueueJob(sparqlUpdateJob);
                    break;
            }
            return jobId;
        }

        public CommitPoint GetCommitPoint(string storeName, ulong commitOffset)
        {
            return GetCommitPoints(storeName).FirstOrDefault(x => x.LocationOffset.Equals(commitOffset));
        }

        public CommitPoint GetCommitPoint(string storeName, DateTime timestamp)
        {
            var utcTimestamp = timestamp.ToUniversalTime();
            return GetCommitPoints(storeName).FirstOrDefault(x => x.CommitTime <= utcTimestamp);
        }

        public Guid Consolidate(string store, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(store);
            var jobId = Guid.NewGuid();
            storeWorker.QueueJob(new ConsolidateJob(jobId, jobLabel, storeWorker));
            return jobId;
        }

        public Guid ExecuteUpdate(string store, string updateExpression, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(store);
            var jobId = Guid.NewGuid();
            storeWorker.QueueJob(new SparqlUpdateJob(jobId, jobLabel, storeWorker, updateExpression));
            return jobId;
        }

        public void QueueUpdate(Guid jobId, string store, string updateExpression, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(store);
            storeWorker.QueueJob(new SparqlUpdateJob(jobId, jobLabel, storeWorker, updateExpression));
        }

        public Job LoadTransaction(string storeId, ITransactionInfo txn, string jobLabel = null)
        {
            var transactionLog = _storeManager.GetTransactionLog(_baseLocation + "\\" + storeId);

            var jobId = Guid.NewGuid();
            switch (txn.TransactionType)
            {
                case TransactionType.ImportJob:
                    var importJob = new ImportJob(jobId, jobLabel, null);
                    importJob.ReadTransactionDataFromStream(transactionLog.GetTransactionData(txn.DataStartPosition));
                    return importJob;
                case TransactionType.UpdateTransaction:
                    var updateJob = new UpdateTransaction(jobId, jobLabel, null);
                    updateJob.ReadTransactionDataFromStream(transactionLog.GetTransactionData(txn.DataStartPosition));
                    return updateJob;
                case TransactionType.SparqlUpdateTransaction:
                    var sparqlUpdateJob = new SparqlUpdateJob(jobId, jobLabel, null, null);
                    sparqlUpdateJob.ReadTransactionDataFromStream(transactionLog.GetTransactionData(txn.DataStartPosition));
                    return sparqlUpdateJob;
            }
            return null;
        }

        public IEnumerable<Storage.Statistics.StoreStatistics> GetStatistics(string store)
        {
            var storeWorker = GetStoreWorker(store);
            return storeWorker.StoreStatistics.GetStatistics();
        }

        public Guid UpdateStatistics(string storeName, string jobLabel = null)
        {
            var storeWorker = GetStoreWorker(storeName);
            return storeWorker.UpdateStatistics(jobLabel);
        }

        public Guid CreateSnapshot(string sourceStoreName, string targetStoreName, PersistenceType persistenceType, ulong sourceCommitPointId, string jobLabel = null)
        {

            var storeWorker = GetStoreWorker(sourceStoreName);
            return storeWorker.QueueSnapshotJob(targetStoreName, persistenceType, sourceCommitPointId, jobLabel);
        }

        private static SparqlQuery ParseSparql(string exp)
        {
            var parser = new SparqlQueryParser(SparqlQuerySyntax.Extended);
            var expressionFactories = parser.ExpressionFactories.ToList();
            expressionFactories.Add(new BrightstarFunctionFactory());
            parser.ExpressionFactories = expressionFactories;
            var query = parser.ParseFromString(exp);
            return query;
        }

        public void Warmup(PageCachePreloadConfiguration preloadConfiguration)
        {
            if (preloadConfiguration != null && preloadConfiguration.Enabled)
            {
                ThreadPool.QueueUserWorkItem(RunServerCoreWarmup, preloadConfiguration);
            }
        }

        private void RunServerCoreWarmup(object state)
        {
            var preloadConfiguration = state as PageCachePreloadConfiguration;
            if (preloadConfiguration == null) return;

            var warmupInfos = GetStoreWarmupInfo(preloadConfiguration);
            warmupInfos.RemoveAll(x => x.DataSize == 0ul || x.CacheRatio == 0.0m);
            decimal totalRatio = warmupInfos.Sum(x => x.CacheRatio);
            foreach (var warmupInfo in warmupInfos.OrderBy(x => x.CacheRatio).ThenBy(x => x.DataSize))
            {
                var storeWorker = GetStoreWorker(warmupInfo.StoreName);
                storeWorker.WarmupStore(warmupInfo.CacheRatio/totalRatio);
                totalRatio -= warmupInfo.CacheRatio;
                if (totalRatio <= 0) break;
            }
        }

        private List<WarmupInfo> GetStoreWarmupInfo(PageCachePreloadConfiguration preloadConfiguration)
        {
            var warmupInfos = new List<WarmupInfo>();
            foreach (var storeName in ListStores())
            {
                StorePreloadConfiguration storeConfiguration;
                string storeLocation = Path.Combine(_baseLocation, storeName);
                if (preloadConfiguration.StorePreloadConfigurations != null &&
                    preloadConfiguration.StorePreloadConfigurations.TryGetValue(storeName, out storeConfiguration))
                {
                    if (storeConfiguration.CacheRatio > 0)
                    {
                        warmupInfos.Add(new WarmupInfo(storeName, storeConfiguration.CacheRatio,
                                                       GetStoreDataSize(storeLocation)));
                    }
                    else if (preloadConfiguration.DefaultCacheRatio > 0)
                    {
                        warmupInfos.Add(new WarmupInfo(storeName, preloadConfiguration.DefaultCacheRatio,
                                                       GetStoreDataSize(storeLocation)));
                    }
                }
                else if (preloadConfiguration.DefaultCacheRatio > 0)
                {
                    warmupInfos.Add(new WarmupInfo(storeName, preloadConfiguration.DefaultCacheRatio,
                                                   GetStoreDataSize(storeLocation)));
                }
            }
            return warmupInfos;
        }

        private ulong GetStoreDataSize(string storeName)
        {
            return _storeManager.GetDataSize(storeName);
        }

        private class WarmupInfo : IComparable
        {
            public WarmupInfo(string storeName, decimal cacheRatio, ulong dataSize)
            {
                StoreName = storeName;
                CacheRatio = cacheRatio;
                DataSize = dataSize;
            }

            public string StoreName { get; set; }
            public decimal CacheRatio { get; set; }
            public ulong DataSize { get; set; }
            public int CompareTo(object obj)
            {
                var other = obj as WarmupInfo;
                if (other == null) return 1;
                int result = this.CacheRatio.CompareTo(other.CacheRatio);
                if (result == 0) result= this.DataSize.CompareTo(other.DataSize);
                return result;
            }
        }

        public IEnumerable<string> ListNamedGraphs(string storeName)
        {
            var storeWorker = GetStoreWorker(storeName);
            return storeWorker.ListNamedGraphs();
        }
    }
}
