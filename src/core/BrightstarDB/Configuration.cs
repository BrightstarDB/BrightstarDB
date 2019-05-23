
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using BrightstarDB.Caching;
using BrightstarDB.Config;
using BrightstarDB.Storage;

namespace BrightstarDB
{
    /// <summary>
    /// Provides the global default configuration for the BrightstarDB service and client
    /// </summary>
    public class Configuration
    {
        private static bool _enableQueryCache;
        private static string _queryCacheDirectory;
        private static int _queryCacheMemory;
        private static int _queryCacheDiskSpace;
        private static ICache _queryCache;
        private const string StoreLocationPropertyName = "BrightstarDB:StoreLocation";
        private const string TxnFlushTriggerPropertyName = "BrightstarDB:TxnFlushTripleCount";
        private const string ConnectionStringPropertyName = "BrightstarDB:ConnectionString";

        /// <summary>
        /// The size of the page cache managed by the server (in MB)
        /// </summary>
        private const string PageCacheSizeName = "BrightstarDB:PageCacheSize";

        /// <summary>
        /// OBSOLETE: The number of objects to hold in the BTreeStore's object cache.
        /// </summary>
        private const string ReadStoreObjectCacheSizeName = "BrightstarDB:ReadStoreObjectCacheSize";

        private const string EnableQueryCacheName = "BrightstarDB:QueryCache:Enabled";
        private const string QueryCacheDirectoryName = "BrightstarDB:QueryCache:Directory";
        private const string QueryCacheMemoryName = "BrightstarDB:QueryCache:MemoryQuota";
        private const string QueryCacheDiskSpaceName = "BrightstarDB:QueryCache:DiskQuota";
        private const string PersistenceTypeName = "BrightstarDB:PersistenceType";
        private const string ClusterNodePortName = "BrightstarDB:Cluster:NodePort";
        private const string ResourceCacheLimitName = "BrightstarDB:ResourceCacheLimit";
        private const string StatsUpdateTransactionCountName = "BrightstarDB:StatsUpdate:TransactionCount";
        private const string StatsUpdateTimeSpanName = "BrightstarDB:StatsUpdate:TimeSpan";
        private const string QueryExecutionTimeoutName = "BrightstarDB:QueryExecutionTimeout";
        private const string UpdateExecutionTimeoutName = "BrightstarDB:UpdateExecutionTimeout";
        private const string EnableVirtualizedQueriesName = "BrightstarDB:EnableVirtualizedQueries";

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
            var configuration = new ConfigurationBuilder();
            configuration.AddXmlFile("app.config",true)
                .AddJsonFile("appSettings.json", true);
            var appSettings = configuration.Build();
            StoreLocation = appSettings[StoreLocationPropertyName];

            // Transaction Flushing
            TransactionFlushTripleCount = GetApplicationSetting(appSettings, TxnFlushTriggerPropertyName, 10000);

            // Read Store cache
            // TODO : Remove this if it is no longer in use.
            var readStoreObjectCacheSizeString = appSettings[ReadStoreObjectCacheSizeName];
            ReadStoreObjectCacheSize = 10000;
            if (!string.IsNullOrEmpty(readStoreObjectCacheSizeString))
            {
                if (int.TryParse(readStoreObjectCacheSizeString, out var val))
                {
                    if (val > 0)
                    {
                        ReadStoreObjectCacheSize = val;
                    }
                }
            }

            // Connection String
            ConnectionString = appSettings[ConnectionStringPropertyName];

            // Query Caching
            var enableQueryCacheString = appSettings[EnableQueryCacheName];
            EnableQueryCache = true;
            if (!string.IsNullOrEmpty(enableQueryCacheString))
            {
                EnableQueryCache = bool.Parse(enableQueryCacheString);
            }
            QueryCacheMemory = GetApplicationSetting(appSettings, QueryCacheMemoryName, DefaultQueryCacheMemory);
            QueryCacheDiskSpace = GetApplicationSetting(appSettings, QueryCacheDiskSpaceName, DefaultQueryCacheDiskSpace);
            QueryCacheDirectory = appSettings[QueryCacheDirectoryName];
            QueryCache = GetQueryCache();

            // Virtualized queries
            EnableVirtualizedQueries = GetApplicationSetting(appSettings, EnableVirtualizedQueriesName, false);

            // StatsUpdate properties
            StatsUpdateTransactionCount = GetApplicationSetting(appSettings, StatsUpdateTransactionCountName, 0);
            StatsUpdateTimespan = GetApplicationSetting(appSettings, StatsUpdateTimeSpanName, 0);

            // Advanced embedded application settings - read from the brightstar section of the app/web.config
            EmbeddedServiceConfiguration =
                appSettings.GetSection("Brightstar:Embedded").Get<EmbeddedServiceConfiguration>() ??
                new EmbeddedServiceConfiguration();

            // ResourceCacheLimit
            ResourceCacheLimit = GetApplicationSetting(appSettings, ResourceCacheLimitName, DefaultResourceCacheLimit);

            // Persistence Type
            var persistenceTypeSetting = GetApplicationSetting(appSettings, PersistenceTypeName);
            if (!String.IsNullOrEmpty(persistenceTypeSetting))
            {
                switch (persistenceTypeSetting.ToLowerInvariant())
                {
                    case PersistenceTypeAppendOnly:
                        PersistenceType = PersistenceType.AppendOnly;
                        break;
                    case PersistenceTypeRewrite:
                        PersistenceType = PersistenceType.Rewrite;
                        break;
                    default:
                        PersistenceType = DefaultPersistenceType;
                        break;
                }
            }
            else
            {
                PersistenceType = DefaultPersistenceType;
            }

            // Page Cache Size
            var pageCacheSizeSetting = GetApplicationSetting(appSettings, PageCacheSizeName);
            if (!String.IsNullOrEmpty(pageCacheSizeSetting) && Int32.TryParse(pageCacheSizeSetting, out var pageCacheSize))
            {
                PageCacheSize = pageCacheSize;
            }
            else
            {
                PageCacheSize = DefaultPageCacheSize;
            }

            // Clustering
            var clusterNodePortSetting = GetApplicationSetting(appSettings, ClusterNodePortName);
            if (!String.IsNullOrEmpty(clusterNodePortSetting) &&
                Int32.TryParse(clusterNodePortSetting, out var clusterNodePort))
            {
                ClusterNodePort = clusterNodePort;
            }
            else
            {
                ClusterNodePort = 10001;
            }

            QueryExecutionTimeout = GetApplicationSetting(appSettings, QueryExecutionTimeoutName, DefaultQueryExecutionTimeout);
            UpdateExecutionTimeout = GetApplicationSetting(appSettings, UpdateExecutionTimeoutName, DefaultUpdateExecutionTimeout);
        }

        /// <summary>
        /// Default path to the directory containing BrightstarDB stores
        /// </summary>
        public static string StoreLocation { get; set; }

        /// <summary>
        /// The threshold number of triples to import in a batch before 
        /// flushing indexes to disk.
        /// </summary>
        public static int TransactionFlushTripleCount { get; set; }

        /// <summary>
        /// The default BrightstarDB connection string
        /// </summary>
        public static string ConnectionString { get; set; }

        /// <summary>
        /// NO LONGER IN USE
        /// </summary>
        public static int ReadStoreObjectCacheSize { get; set; }

        /// <summary>
        /// Size of the page cache in MB
        /// </summary>
        public static int PageCacheSize { get; set; }

        /// <summary>
        /// The size of resource cache (in number of entries) for each store opened.
        /// </summary>
        public static int ResourceCacheLimit { get; set; }

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
            get => _enableQueryCache;
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
            get => _queryCacheDirectory;
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
            get => _queryCacheMemory;
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
            get => _queryCacheDiskSpace;
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
        public static ICache QueryCache
        {
            get => _queryCache ?? (_queryCache = GetQueryCache());
            private set => _queryCache = value;
        }


        /// <summary>
        /// The port number used for clustered BrightstarDB service communication
        /// </summary>
        public static int ClusterNodePort { get; set; }

        /// <summary>
        /// The default BrightstarDB store persistence type
        /// </summary>
        public static PersistenceType PersistenceType { get; set; }

        /// <summary>
        /// Get or set the maximum number of transactions to allow between
        /// updates of store stats
        /// </summary>
        /// <remarks>If this property is set to 0, then only the <see cref="StatsUpdateTimespan"/>
        /// property will be used to determine when to update stats. If both this property 
        /// and <see cref="StatsUpdateTimespan"/> are set to 0, then store stats will never
        /// be updated.</remarks>
        public static int StatsUpdateTransactionCount { get; set; }

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
        public static int StatsUpdateTimespan { get; set; }

        /// <summary>
        /// Get or set the additional configuration options for running an embedded service
        /// </summary>
        public static EmbeddedServiceConfiguration EmbeddedServiceConfiguration { get; set; }

        /// <summary>
        /// Set this property to true to enable the use of virtual nodes in SPARQL
        /// queries. This an experimental option that may improve query performance.
        /// </summary>
        public static bool EnableVirtualizedQueries { get; set; }

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
            get => VDS.RDF.Options.UpdateExecutionTimeout;
            set => VDS.RDF.Options.UpdateExecutionTimeout = value;
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

        private static string GetApplicationSetting(IConfiguration config, string key)
        {
            return config[key];
        }

        private static bool GetApplicationSetting(IConfiguration config, string key, bool defaultValue)
        {
            var setting = GetApplicationSetting(config, key);
            if (!string.IsNullOrEmpty(setting) && bool.TryParse(setting, out var value)) return value;
            return defaultValue;
        }

        private static int GetApplicationSetting(IConfiguration config, string key, int defaultValue)
        {
            var setting = GetApplicationSetting(config, key);
            if (!string.IsNullOrEmpty(setting) && int.TryParse(setting, out var intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        private static long GetApplicationSetting(IConfiguration config, string key, long defaultValue)
        {
            var setting = GetApplicationSetting(config, key);
            if (!string.IsNullOrEmpty(setting) && long.TryParse(setting, out var longValue))
            {
                return longValue;
            }
            return defaultValue;
        }

    }
}
