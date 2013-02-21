using System;
using System.Collections.Concurrent;
using System.Linq;
using BrightstarDB.Utils;

namespace BrightstarDB.Storage.Persistence
{
    internal class CircularBufferPageCache : IPageCache
    {
        private readonly object _partitionIdLock = new object();
        private int _nextPartitionId;
        private readonly ConcurrentDictionary<string, int> _partitionIds; 
        private readonly IndexedCircularBuffer<PartitionPageKey, IPageCacheItem> _pages;

        public CircularBufferPageCache(int pageCapacity)
        {
            _partitionIds = new ConcurrentDictionary<string, int>();
             _pages = new IndexedCircularBuffer<PartitionPageKey, IPageCacheItem>(pageCapacity);   
        }

        #region Implementation of IPageCache

        public void InsertOrUpdate(string partition, IPageCacheItem page)
        {
            var ppk = new PartitionPageKey(AssertPartitionId(partition), page.Id);
            _pages.Insert(ppk, page);
        }

        public IPageCacheItem Lookup(string partition, ulong pageId)
        {
            IPageCacheItem page;
            var ppk = new PartitionPageKey(AssertPartitionId(partition), pageId);
            return _pages.TryGetValue(ppk, out page) ? page : null;
        }

        public void Clear(string partition)
        {
            var partitionId = AssertPartitionId(partition);
            foreach(var k in _pages.Keys.Where(k=>k.PartitionId == partitionId).ToList())
            {
                _pages.Remove(k);
            }
        }

        #endregion

        private int AssertPartitionId(string partition)
        {
            return _partitionIds.GetOrAdd(partition, p =>
                                                  {
                                                      lock (_partitionIdLock)
                                                      {
                                                          return _nextPartitionId++;
                                                      }
                                                  });
        }
        class PartitionPageKey
        {
            public int PartitionId { get; private set; }
            public ulong PageId { get; set; }

            public PartitionPageKey(int partitionId, ulong pageId)
            {
                PartitionId = partitionId;
                PageId = pageId;
            }

            public override int GetHashCode()
            {
                return PartitionId ^ BitConverter.ToInt32(BitConverter.GetBytes(PageId), 0);
            }

            public override bool Equals(object obj)
            {
                var other = obj as PartitionPageKey;
                if (other != null)
                {
                    return other.PartitionId.Equals(PartitionId) && other.PageId.Equals(PageId);
                }
                return false;
            }
        }
    }
}
