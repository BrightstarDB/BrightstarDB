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
        public PageCachePreloadConfiguration PreloadConfiguration { get; set; }

        private const int DefaultPageCacheSize = 4; // in MB
        private const int DefaultResourceCacheLimit = 1000;

        public ConfigurationProvider()
        {
            PageCacheSize = DefaultPageCacheSize;
            ResourceCacheLimit = DefaultResourceCacheLimit;
            PersistenceType = PersistenceType.AppendOnly;
            QueryCache = new NullCache();
            StatsUpdateTimespan = 0;
            StatsUpdateTransactionCount = 0;
            TransactionFlushTripleCount = 1000;
            PreloadConfiguration = new PageCachePreloadConfiguration {Enabled = false};
        }
    }

}