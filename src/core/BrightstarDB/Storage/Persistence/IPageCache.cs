using System;

namespace BrightstarDB.Storage.Persistence
{
    internal class EvictionEventArgs : EventArgs
    {
        public string Partition { get; private set; }
        public ulong PageId { get; private set; }
        public bool CancelEviction { get; set; }

        public EvictionEventArgs(string partition, ulong pageId)
        {
            Partition = partition;
            PageId = pageId;
        }
    }

    
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
