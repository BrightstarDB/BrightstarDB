using BrightstarDB.Caching;
using BrightstarDB.Config;
using BrightstarDB.Storage;

namespace BrightstarDB
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public string ConnectionString { get; set; }
        public int PageCacheSize { get; set; }
        public int ResourceCacheLimit { get; set; }
        public PersistenceType PersistenceType { get; set; }
        public ICache QueryCache { get; set; }
        public int StatsUpdateTimespan { get; set; }
        public int StatsUpdateTransactionCount { get; set; }
        public int TransactionFlushTripleCount { get; set; }
        public EmbeddedServiceConfiguration EmbeddedServiceConfiguration { get; set; }
        public bool EnableVirtualizedQueries { get; set; }


        public const PersistenceType DefaultPersistenceType = PersistenceType.AppendOnly;
        public const int DefaultQueryCacheMemory = 4; // in MB
        public const int DefaultPageCacheSize = 4; // in MB
        public const int DefaultResourceCacheLimit = 1000; // number of entries
        public const int DefaultTransactionFlushTripleCount = 10000;
        public const int DefaultStatsUpdateTransactionCount = 0;
        public const int DefaultStatsUpdateTimespan = 0;
        public const decimal DefaultPreloadCacheRatio = 0.0m;
        public const bool DefaultPreloadCacheEnabled = false;

        private const long MegabytesToBytes = 1024 * 1024;

        public ConfigurationProvider()
        {
            // Transaction Flushing
            TransactionFlushTripleCount = DefaultTransactionFlushTripleCount;

            // ResourceCacheLimit
            ResourceCacheLimit = DefaultResourceCacheLimit;

            // Connection String
            ConnectionString = null;

            // Query Caching
            QueryCache = GetQueryCache(true, DefaultQueryCacheMemory);

            // Persistence Type
            PersistenceType = DefaultPersistenceType;

            // Page Cache Size
            PageCacheSize = DefaultPageCacheSize;

            // StatsUpdate properties
            StatsUpdateTransactionCount = DefaultStatsUpdateTransactionCount;
            StatsUpdateTimespan = DefaultStatsUpdateTimespan;

            // Cache Preload Configuration
            var preloadConfig = new PageCachePreloadConfiguration
            {
                DefaultCacheRatio = DefaultPreloadCacheRatio,
                Enabled = DefaultPreloadCacheEnabled
            };
            EmbeddedServiceConfiguration = new EmbeddedServiceConfiguration(preloadConfig, false);

        }

        public static ICache GetQueryCache(bool enableQueryCache, int queryCacheMemory)
        {
            if (enableQueryCache == false)
            {
                return new NullCache();
            }
            ICache memoryCache = new MemoryCache(MegabytesToBytes * queryCacheMemory, new LruCacheEvictionPolicy());
            return memoryCache;
        }

    }
}