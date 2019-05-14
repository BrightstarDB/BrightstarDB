using System;
using System.IO;
using BrightstarDB.Caching;
using BrightstarDB.Config;
using BrightstarDB.Storage;
using System.Configuration;
using BrightstarDB.Portable.Adaptation;


namespace BrightstarDB
{
    /// <summary>
    /// Provides the global default configuration for the BrightstarDB service and client
    /// </summary>
    public class Configuration
    {
        private static IConfigurationProvider _instance;

        private static IConfigurationProvider Instance
        {
            get { return _instance ?? (_instance = PlatformAdapter.Resolve<IConfigurationProvider>()); }
        }

        /// <summary>
        /// The default BrightstarDB connection string
        /// </summary>
        public static string ConnectionString { get { return Instance.ConnectionString; } set { Instance.ConnectionString = value; } }
        public static int PageCacheSize { get { return Instance.PageCacheSize; } set { Instance.PageCacheSize = value; } }
        public static int ResourceCacheLimit { get { return Instance.ResourceCacheLimit; } set { Instance.ResourceCacheLimit = value; } }

        /// <summary>
        /// The default BrightstarDB store persistence type
        /// </summary>
        public static PersistenceType PersistenceType { get { return Instance.PersistenceType; } set { Instance.PersistenceType = value; } }
        public static ICache QueryCache { get { return Instance.QueryCache; } set { Instance.QueryCache = value; } }
        /// <summary>
        /// Get or set the maximum number of seconds to wait between 
        /// updates of store stats.
        /// </summary>
        /// <remarks>
        /// <para>If this property is set to 0 then only the <see cref="StatsUpdateTransactionCount"/>
        /// property will be used to determine when to update stats. If both this property
        /// and <see cref="StatsUpdateTransactionCount"/> are set to 0, then store stats will never 
        /// be updated.</para>
        /// </remarks>
        public static int StatsUpdateTimespan { get { return Instance.StatsUpdateTimespan; } set { Instance.StatsUpdateTimespan = value; } }

        /// <summary>
        /// Get or set the maximum number of transactions to allow between
        /// updates of store stats
        /// </summary>
        /// <remarks>If this property is set to 0, then only the <see cref="StatsUpdateTimespan"/>
        /// property will be used to determine when to update stats. If both this property 
        /// and <see cref="StatsUpdateTimespan"/> are set to 0, then store stats will never
        /// be updated.</remarks>
        public static int StatsUpdateTransactionCount { get { return Instance.StatsUpdateTransactionCount; } set { Instance.StatsUpdateTransactionCount = value; } }
        public static int TransactionFlushTripleCount { get { return Instance.TransactionFlushTripleCount; } set { Instance.TransactionFlushTripleCount = value; } }

        /// <summary>
        /// Get or set the additional configuration options for running an embedded service
        /// </summary>
        public static EmbeddedServiceConfiguration EmbeddedServiceConfiguration { get { return Instance.EmbeddedServiceConfiguration; } set { Instance.EmbeddedServiceConfiguration = value; } }
        /// <summary>
        /// Set this property to true to enable the use of virtual nodes in SPARQL
        /// queries. This an experimental option that may improve query performance.
        /// </summary>
        public static bool EnableVirtualizedQueries { get { return Instance.EnableVirtualizedQueries; } set { Instance.EnableVirtualizedQueries = value; } }


        private static bool _enableQueryCache;
        private static string _queryCacheDirectory;
        private static int _queryCacheMemory;
        private static int _queryCacheDiskSpace;
        private static ICache _queryCache;
        private const string StoreLocationPropertyName = "BrightstarDB.StoreLocation";
        private const string TxnFlushTriggerPropertyName = "BrightstarDB.TxnFlushTripleCount";
        private const string ConnectionStringPropertyName = "BrightstarDB.ConnectionString";

        /// <summary>
        /// The size of the page cache managed by the server (in MB)
        /// </summary>
        private const string PageCacheSizeName = "BrightstarDB.PageCacheSize";

        /// <summary>
        /// OBSOLETE: The number of objects to hold in the BTreeStore's object cache.
        /// </summary>
        private const string ReadStoreObjectCacheSizeName = "BrightstarDB.ReadStoreObjectCacheSize";

        private const string EnableQueryCacheName = "BrightstarDB.EnableQueryCache";
        private const string QueryCacheDirectoryName = "BrightstarDB.QueryCacheDirectory";
        private const string QueryCacheMemoryName = "BrightstarDB.QueryCacheMemory";
        private const string QueryCacheDiskSpaceName = "BrightstarDB.QueryCacheDisk";
        private const string PersistenceTypeName = "BrightstarDB.PersistenceType";
        private const string ClusterNodePortName = "BrightstarDB.ClusterNodePort";
        private const string ResourceCacheLimitName = "BrightstarDB.ResourceCacheLimit";
        private const string StatsUpdateTransactionCountName = "BrightstarDB.StatsUpdate.TransactionCount";
        private const string StatsUpdateTimeSpanName = "BrightstarDB.StatsUpdate.TimeSpan";
        private const string QueryExecutionTimeoutName = "BrightstarDB.QueryExecutionTimeout";
        private const string UpdateExecutionTimeoutName = "BrightstarDB.UpdateExecutionTimeout";
        private const string EnableVirtualizedQueriesName = "BrightstarDB.EnableVirtualizedQueries";

        private const string PersistenceTypeAppendOnly = "appendonly";
        private const string PersistenceTypeRewrite = "rewrite";
        private const PersistenceType DefaultPersistenceType = PersistenceType.AppendOnly;

        private const int DefaultQueryCacheDiskSpace = 2048; // in MB
        private const long MegabytesToBytes = 1024*1024;

        private const long DefaultQueryExecutionTimeout =  180000;
        private const long DefaultUpdateExecutionTimeout = 180000;
        
        private const int DefaultPageCacheSize = 2048; // in MB
        private const int DefaultQueryCacheMemory = 256; // in MB
        private const int DefaultResourceCacheLimit = 1000000; // number of entries

        static Configuration()
        {
            IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);
            StoreLocation = "brightstar";
            TransactionFlushTripleCount = 1000;
            PageCacheSize = DefaultPageCacheSize;
            ResourceCacheLimit = DefaultResourceCacheLimit;
            EnableOptimisticLocking = false;
            EnableQueryCache = false;
            PersistenceType = DefaultPersistenceType;
            QueryCache = new NullCache();


            QueryExecutionTimeout = GetApplicationSetting(QueryExecutionTimeoutName, DefaultQueryExecutionTimeout);
            UpdateExecutionTimeout = GetApplicationSetting(UpdateExecutionTimeoutName, DefaultUpdateExecutionTimeout);
        }

        /// <summary>
        /// Default path to the directory containing BrightstarDB stores
        /// </summary>
        public static string StoreLocation { get; set; }

        /// <summary>
        /// The threshold number of triples to import in a batch before 
        /// flushing indexes to disk.
        /// </summary>
        //public static int TransactionFlushTripleCount { get; set; }

        
        /// <summary>
        /// NO LONGER IN USE
        /// </summary>
        public static int ReadStoreObjectCacheSize { get; set; }

        /// <summary>
        /// Size of the page cache in MB
        /// </summary>
       // public static int PageCacheSize { get; set; }

        /// <summary>
        /// The size of resource cache (in number of entries) for each store opened.
        /// </summary>
        //public static int ResourceCacheLimit { get; set; }

        /// <summary>
        /// The default setting for optimistic locking in the Data Objects
        /// and Entity Framework contexts
        /// </summary>
        public static bool EnableOptimisticLocking { get; set; }

        /// <summary>
        /// Enable or disable server-side SPARQL query cache
        /// </summary>
        public static bool EnableQueryCache
        {
            get { return _enableQueryCache; }
            set
            {
                if (value == _enableQueryCache) return;
                _enableQueryCache = value;
                QueryCache = null;
            }
        }

        /// <summary>
        /// The path to the directory to use for the server-side SPARQL query disk cache
        /// </summary>
        public static string QueryCacheDirectory
        {
            get { return _queryCacheDirectory; }
            set
            {
                if (value == _queryCacheDirectory) return;
                _queryCacheDirectory = value;
                QueryCache = null;
            }
        }

        /// <summary>
        /// Maximum size of server-side SPARQL query memory cache in MB
        /// </summary>
        public static int QueryCacheMemory
        {
            get { return _queryCacheMemory; }
            set
            {
                if (value == _queryCacheMemory) return;
                _queryCacheMemory = value;
                QueryCache = null;
            }
        }

        /// <summary>
        /// Maximum size of the server-side SPARQL query disk cache in MB
        /// </summary>
        public static int QueryCacheDiskSpace
        {
            get { return _queryCacheDiskSpace; }
            set
            {
                if (value == _queryCacheDiskSpace) return;
                _queryCacheDiskSpace = value;
                QueryCache = null;
            }
        }

        /// <summary>
        /// Retrieves the SPARQL query cache
        /// </summary>
        /*
        public static ICache QueryCache
        {
            get { return _queryCache ?? (_queryCache = GetQueryCache()); }
            private set { _queryCache = value; }
        }
        */

        /// <summary>
        /// The port number used for clustered BrightstarDB service communication
        /// </summary>
        public static int ClusterNodePort { get; set; }

        
       
        
        
        
        /// <summary>
        /// Boolean flag that is set to true when the Mono runtime is detected.
        /// </summary>
        public static bool IsRunningOnMono { get; private set; }

        /// <summary>
        /// Get or set the SPARQL query execution timeout (in milliseconds)
        /// </summary>
        /// <remarks>This configuration value applies only when running against an embedded
        /// BrightstarDB store. For client-server connections, the timeout will be determined
        /// by the server.</remarks>
        public static long QueryExecutionTimeout
        {
            get { return VDS.RDF.Options.QueryExecutionTimeout; }
            set { VDS.RDF.Options.QueryExecutionTimeout = value; }
        }

        /// <summary>
        /// Get or set the SPARQL update execution timeout (in milliseconds)
        /// </summary>
        /// <remarks>This configuration value applies only when running against an embedded
        /// BrightstarDB store. For client-server connections, the timeout will be determined
        /// by the server.</remarks>
        public static long UpdateExecutionTimeout
        {
            get { return VDS.RDF.Options.UpdateExecutionTimeout; }
            set { VDS.RDF.Options.UpdateExecutionTimeout = value; }
        }
        private static ICache GetQueryCache()
        {
            if (EnableQueryCache == false)
            {
                return new NullCache();
            }
            var cacheDir = QueryCacheDirectory;
            if (String.IsNullOrEmpty(cacheDir) && (!String.IsNullOrEmpty(StoreLocation)))
            {
                cacheDir = Path.Combine(StoreLocation, "_bscache");
            }
            if (!String.IsNullOrEmpty(cacheDir))
            {
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }
                ICache directoryCache = new DirectoryCache(cacheDir, MegabytesToBytes*QueryCacheDiskSpace,
                                                           new LruCacheEvictionPolicy()),
                       memoryCache = new MemoryCache(MegabytesToBytes*QueryCacheMemory, new LruCacheEvictionPolicy());
                return new TwoLevelCache(memoryCache, directoryCache);
            }
            else
            {
                ICache memoryCache = new MemoryCache(MegabytesToBytes*QueryCacheMemory, new LruCacheEvictionPolicy());
                return memoryCache;
            }
        }

        private static string GetApplicationSetting(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
        }

        private static bool GetApplicationSetting(string key, bool defaultValue)
        {
            var setting = GetApplicationSetting(key);
            bool value;
            if (!string.IsNullOrEmpty(setting) && bool.TryParse(setting, out value)) return value;
            return defaultValue;
        }

        private static int GetApplicationSetting(string key, int defaultValue)
        {
            var setting = GetApplicationSetting(key);
            int intValue;
            if (!String.IsNullOrEmpty(setting) && Int32.TryParse(setting, out intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        private static long GetApplicationSetting(string key, long defaultValue)
        {
            var setting = GetApplicationSetting(key);
            long longValue;
            if (!String.IsNullOrEmpty(setting) && Int64.TryParse(setting, out longValue))
            {
                return longValue;
            }
            return defaultValue;
        }


    }

    public interface IConfigurationProvider
    {
        string ConnectionString { get; set; }
        int PageCacheSize { get; set; }
        int ResourceCacheLimit { get; set; }
        PersistenceType PersistenceType { get; set; }
        ICache QueryCache { get; set; }

        int StatsUpdateTimespan { get; set; }
        int StatsUpdateTransactionCount { get; set; }

        int TransactionFlushTripleCount { get; set; }

        EmbeddedServiceConfiguration EmbeddedServiceConfiguration { get; set; }

        bool EnableVirtualizedQueries { get; set; }
    }
}
