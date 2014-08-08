using System;
using System.Configuration;
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
        public bool EnableVirtualizedQueries { get; set; }

        #region AppSetting Property Names
        private const string PageCacheSizePropertyName = "BrightstarDB.PageCacheSize";
        private const string TxnFlushTriggerPropertyName = "BrightstarDB.TxnFlushTripleCount";
        private const string ResourceCacheLimitName = "BrightstarDB.ResourceCacheLimit";
        private const string ConnectionStringPropertyName = "BrightstarDB.ConnectionString";
        private const string EnableQueryCacheName = "BrightstarDB.EnableQueryCache";
        private const string QueryCacheMemoryName = "BrightstarDB.QueryCacheMemory";
        private const string PersistenceTypeName = "BrightstarDB.PersistenceType";
        private const string PersistenceTypeAppendOnly = "appendonly";
        private const string PersistenceTypeRewrite = "rewrite";
        private const string StatsUpdateTransactionCountName = "BrightstarDB.StatsUpdate.TransactionCount";
        private const string StatsUpdateTimeSpanName = "BrightstarDB.StatsUpdate.TimeSpan";
        private const string CachePreloadRatioName = "BrightstarDB.PageCachePreload.Ratio";
        private const string CachePreloadEnabledName = "BrightstarDB.PageCachePreload.Enabled";
        #endregion

        #region Default Values and constants
        private const PersistenceType DefaultPersistenceType = PersistenceType.AppendOnly;
        private const int DefaultQueryCacheMemory = 256; // in MB
        private const int DefaultPageCacheSize = 256; // in MB
        private const int DefaultResourceCacheLimit = 1000000; // number of entries
        private const int DefaultTransactionFlushTripleCount = 10000;
        private const long MegabytesToBytes = 1024 * 1024;
        #endregion

        public ConfigurationProvider()
        {
            var appSettings = ConfigurationManager.AppSettings;

            // Connection String
            ConnectionString = appSettings.Get(ConnectionStringPropertyName);

            // Page Cache Size
            PageCacheSize = GetApplicationSetting(PageCacheSizePropertyName, DefaultPageCacheSize);

            // ResourceCacheLimit
            ResourceCacheLimit = GetApplicationSetting(ResourceCacheLimitName, DefaultResourceCacheLimit);

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

            // Query Caching
            var enableQueryCacheString = appSettings.Get(EnableQueryCacheName);
            var enableQueryCache = true;
            if (!string.IsNullOrEmpty(enableQueryCacheString))
            {
                enableQueryCache = bool.Parse(enableQueryCacheString);
            }
            var queryCacheMemory = GetApplicationSetting(QueryCacheMemoryName, DefaultQueryCacheMemory);
            QueryCache = GetQueryCache(enableQueryCache, queryCacheMemory);

            // StatsUpdate properties
            StatsUpdateTransactionCount = GetApplicationSetting(StatsUpdateTransactionCountName, 0);
            StatsUpdateTimespan = GetApplicationSetting(StatsUpdateTimeSpanName, 0);

            // Transaction Flushing
            TransactionFlushTripleCount = GetApplicationSetting(TxnFlushTriggerPropertyName, DefaultTransactionFlushTripleCount);

            // Cache Preload Configuration
            PreloadConfiguration = new PageCachePreloadConfiguration
            {
                DefaultCacheRatio = GetApplicationSetting(CachePreloadRatioName, 0.5m),
                Enabled = GetApplicationSetting(CachePreloadEnabledName, false)
            };

        }


        private static string GetApplicationSetting(string key)
        {
            return ConfigurationManager.AppSettings.Get(key);
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

        private static decimal GetApplicationSetting(string key, decimal defaultValue)
        {
            var setting = GetApplicationSetting(key);
            decimal value;
            if (!String.IsNullOrEmpty(setting) && Decimal.TryParse(setting, out value))
            {
                return value;
            }
            return defaultValue;
        }

        private static bool GetApplicationSetting(string key, bool defaultValue)
        {
            var setting = GetApplicationSetting(key);
            bool value;
            if (!String.IsNullOrEmpty(setting) && Boolean.TryParse(setting, out value))
            {
                return value;
            }
            return defaultValue;
        }

        private static ICache GetQueryCache(bool enableQueryCache, int queryCacheMemory)
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
