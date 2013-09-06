using System;
using System.IO;
using BrightstarDB.Caching;
using BrightstarDB.Storage;

#if !PORTABLE
using System.Configuration;
#endif

#if SILVERLIGHT
using System.IO.IsolatedStorage;
#endif

namespace BrightstarDB
{
    internal class Configuration
    {
        private const string StoreLocationPropertyName = "BrightstarDB.StoreLocation";
        private const string LogLevelPropertyName = "BrightstarDB.LogLevel";
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
        private const string HttpPortName = "BrightstarDB.HttpPort";
        private const string TcpPortName = "BrightstarDB.TcpPort";
        private const string NetNamedPipeName = "BrightstarDB.NetNamedPipeName";
        private const string PersistenceTypeName = "BrightstarDB.PersistenceType";
        private const string ClusterNodePortName = "BrightstarDB.ClusterNodePort";
        private const string ResourceCacheLimitName = "BrightstarDB.ResourceCacheLimit";
        private const string StatsUpdateTransactionCountName = "BrightstarDB.StatsUpdate.TransactionCount";
        private const string StatsUpdateTimeSpanName = "BrightstarDB.StatsUpdate.TimeSpan";

        private const string PersistenceTypeAppendOnly = "appendonly";
        private const string PersistenceTypeRewrite = "rewrite";
        private const PersistenceType DefaultPersistenceType = PersistenceType.AppendOnly;

        private const int DefaultQueryCacheDiskSpace = 2048; // in MB
        private const long MegabytesToBytes = 1024*1024;

        private const int DefaultHttpPort = 8090;
        private const int DefaultTcpPort = 8095;

#if WINDOWS_PHONE
        private const int DefaultPageCacheSize = 4; // in MB
        private const int DefaultResourceCacheLimit = 10000; // number of entries
#else
        private const int DefaultPageCacheSize = 2048; // in MB
        private const int DefaultQueryCacheMemory = 256; // in MB
        private const int DefaultResourceCacheLimit = 1000000; // number of entries
#endif

        static Configuration()
        {
#if WINDOWS_PHONE
            var store = IsolatedStorageFile.GetUserStoreForApplication();
            if (!store.DirectoryExists("brightstar"))
            {
                store.CreateDirectory("brightstar");
            }
            StoreLocation = "brightstar";
            LogLevel = "Error";
            TransactionFlushTripleCount = 1000;
            QueryCache = new NullCache();
#elif PORTABLE
            StoreLocation = "brightstar";
            LogLevel = "Error";
            TransactionFlushTripleCount = 1000;
            PageCacheSize = DefaultPageCacheSize;
            ResourceCacheLimit = DefaultResourceCacheLimit;
            EnableOptimisticLocking = false;
            EnableQueryCache = false;
            PersistenceType = DefaultPersistenceType;
            QueryCache = new NullCache();
#else
            var appSettings = ConfigurationManager.AppSettings;
            StoreLocation = appSettings.Get(StoreLocationPropertyName);
            LogLevel = appSettings.Get(LogLevelPropertyName);

            // Port Numbers
            HttPort = GetApplicationSetting(HttpPortName, DefaultHttpPort);
            TcpPort = GetApplicationSetting(TcpPortName, DefaultTcpPort);

            // Named Pipe Name
            var namedPipeValue = appSettings.Get(NetNamedPipeName);
            NamedPipeName = !string.IsNullOrEmpty(namedPipeValue) ? namedPipeValue : "brightstar";

            // Transaction Flushing
            TransactionFlushTripleCount = GetApplicationSetting(TxnFlushTriggerPropertyName, 10000);

            // Read Store cache
            // TODO : Remove this if it is no longer in use.
            var readStoreObjectCacheSizeString = appSettings.Get(ReadStoreObjectCacheSizeName);
            ReadStoreObjectCacheSize = 10000;
            if (!string.IsNullOrEmpty(readStoreObjectCacheSizeString))
            {
                int val;
                if (int.TryParse(readStoreObjectCacheSizeString, out val))
                {
                    if (val > 0)
                    {
                        ReadStoreObjectCacheSize = val;
                    }
                }
            }

            // ResourceCacheLimit
            ResourceCacheLimit = GetApplicationSetting(ResourceCacheLimitName, DefaultResourceCacheLimit);

            // Connection String
            ConnectionString = appSettings.Get(ConnectionStringPropertyName);

            // Query Caching
            var enableQueryCacheString = appSettings.Get(EnableQueryCacheName);
            EnableQueryCache = true;
            if (!string.IsNullOrEmpty(enableQueryCacheString))
            {
                EnableQueryCache = bool.Parse(enableQueryCacheString);
            }
            QueryCacheMemory = GetApplicationSetting(QueryCacheMemoryName, DefaultQueryCacheMemory);
            QueryCacheDiskSpace = GetApplicationSetting(QueryCacheDiskSpaceName, DefaultQueryCacheDiskSpace);
            QueryCacheDirectory = appSettings.Get(QueryCacheDirectoryName);
            QueryCache = GetQueryCache();

            // Persistence Type
            var persistenceTypeSetting = appSettings.Get(PersistenceTypeName);
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

            // StatsUpdate properties
            StatsUpdateTransactionCount = GetApplicationSetting(StatsUpdateTransactionCountName, 0);
            StatsUpdateTimespan = GetApplicationSetting(StatsUpdateTimeSpanName, 0);

#endif
#if !PORTABLE
            var pageCacheSizeSetting = GetApplicationSetting(PageCacheSizeName);
            int pageCacheSize;
            if (!String.IsNullOrEmpty(pageCacheSizeSetting) && Int32.TryParse(pageCacheSizeSetting, out pageCacheSize))
            {
                PageCacheSize = pageCacheSize;
            }
            else
            {
                PageCacheSize = DefaultPageCacheSize;
            }

            var clusterNodePortSetting = GetApplicationSetting(ClusterNodePortName);
            int clusterNodePort;
            if (!String.IsNullOrEmpty(clusterNodePortSetting) &&
                Int32.TryParse(clusterNodePortSetting, out clusterNodePort))
            {
                ClusterNodePort = clusterNodePort;
            }
            else
            {
                ClusterNodePort = 10001;
            }
#endif
        }

        public static string StoreLocation { get; set; }

        public static string LogLevel { get; set; }

        public static int TransactionFlushTripleCount { get; set; }

        public static string ConnectionString { get; set; }

        /// <summary>
        /// Size of the object cache (in # objects)
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

        public static bool EnableOptimisticLocking { get; set; }

        public static bool EnableQueryCache { get; set; }

        public static string QueryCacheDirectory { get; set; }

        /// <summary>
        /// Maximum size of query memory cache in MB
        /// </summary>
        public static int QueryCacheMemory { get; set; }

        /// <summary>
        /// Maximum size of query disk cache in MB
        /// </summary>
        public static int QueryCacheDiskSpace { get; set; }

        public static ICache QueryCache { get; private set; }

        public static int HttPort { get; set; }
        public static int TcpPort { get; set; }
        public static string NamedPipeName { get; set; }

        public static int ClusterNodePort { get; set; }

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

#if !PORTABLE
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
#endif

#if !PORTABLE
        private static string GetApplicationSetting(string key)
        {
#if WINDOWS_PHONE
            string value;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(key, out value))
            {
                return value;
            }
            return null;
#else
            return ConfigurationManager.AppSettings.Get(key);
#endif
        }
#endif

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

    }
}
