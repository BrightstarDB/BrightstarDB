using System;

namespace BrightstarDB.Storage.Persistence
{
    internal delegate void PreEvictionDelegate(object sender, EvictionEventArgs args);
    internal delegate void PostEvictionDelegate(object sender, EvictionEventArgs args);
    internal delegate void EvictionCompletedDelegate(object sender, EventArgs args);

    internal interface IPageCache
    {
        event PreEvictionDelegate BeforeEvict;
        event PostEvictionDelegate AfterEvict;
        event EvictionCompletedDelegate EvictionCompleted;

        void InsertOrUpdate(string partition, IPageCacheItem page);
        IPageCacheItem Lookup(string partition, ulong pageId);
        void Clear(string partition);
        int FreePages { get;}

        void Clear();
    }
}
