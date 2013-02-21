namespace BrightstarDB.Storage.Persistence
{
    internal interface IPageCache
    {
        void InsertOrUpdate(string partition, IPageCacheItem page);
        IPageCacheItem Lookup(string partition, ulong pageId);
        void Clear(string partition);
    }
}
