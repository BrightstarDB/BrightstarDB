using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Caching;
using BrightstarDB.Config;
using BrightstarDB.Portable.Adaptation;
using BrightstarDB.Storage;

namespace BrightstarDB
{
    public class Configuration
    {
        private  static IConfigurationProvider _instance;

        private static IConfigurationProvider Instance
        {
            get { return _instance ?? (_instance = PlatformAdapter.Resolve<IConfigurationProvider>()); }
        }

        public static string ConnectionString { get { return Instance.ConnectionString; } set { Instance.ConnectionString = value; } }
        public static int PageCacheSize { get { return Instance.PageCacheSize; } set { Instance.PageCacheSize = value; } }
        public static int ResourceCacheLimit { get { return Instance.ResourceCacheLimit; } set { Instance.ResourceCacheLimit = value; } }
        public static PersistenceType PersistenceType { get { return Instance.PersistenceType; } set { Instance.PersistenceType = value; } }
        public static ICache QueryCache { get { return Instance.QueryCache; } set { Instance.QueryCache = value; } }
        public static int StatsUpdateTimespan { get { return Instance.StatsUpdateTimespan; } set { Instance.StatsUpdateTimespan = value; } }
        public static int StatsUpdateTransactionCount { get { return Instance.StatsUpdateTransactionCount; } set { Instance.StatsUpdateTransactionCount = value; } }
        public static int TransactionFlushTripleCount { get { return Instance.TransactionFlushTripleCount; } set { Instance.TransactionFlushTripleCount = value; } }
        public static PageCachePreloadConfiguration PreloadConfiguration { get { return Instance.PreloadConfiguration; } set { Instance.PreloadConfiguration = value; } }
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

        PageCachePreloadConfiguration PreloadConfiguration { get; set; }
    }
}
