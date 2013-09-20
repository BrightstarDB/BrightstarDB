namespace BrightstarDB.Storage.Persistence
{
    internal delegate void PreEvictionDelegate(object sender, EvictionEventArgs args);
    internal delegate void PostEvictionDelegate(object sender, EvictionEventArgs args);

    internal interface IPageCache
    {
        event PreEvictionDelegate BeforeEvict;
        event PostEvictionDelegate AfterEvict;

        void InsertOrUpdate(string partition, IPageCacheItem page);
        IPageCacheItem Lookup(string partition, ulong pageId);
        void Clear(string partition);
    }
}
