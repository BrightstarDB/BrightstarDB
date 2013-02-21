namespace BrightstarDB.Storage.Persistence
{
    static class PageCache
    {
        private static readonly IPageCache PageCacheInstance = CreatePageCache();

        /// <summary>
        /// Get the page cache instance
        /// </summary>
        public static IPageCache Instance { get { return PageCacheInstance; } }

        private const int MBytesToBytes = 1048576;

        private static IPageCache CreatePageCache()
        {
            int cacheCapacity = (MBytesToBytes/BPlusTreeStore.BPlusTreeStoreManager.PageSize) * Configuration.PageCacheSize;
            return new CircularBufferPageCache(cacheCapacity);
        }
    }
}
