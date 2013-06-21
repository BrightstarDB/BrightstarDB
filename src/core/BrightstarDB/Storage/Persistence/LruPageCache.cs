using System;
using System.Collections.Generic;

namespace BrightstarDB.Storage.Persistence
{
    internal class LruPageCache: IPageCache
    {
        private object _updateLock = new object();
        private int _count;
        private readonly int _highWaterMark;
        private readonly int _lowWaterMark;

        private readonly Dictionary<string, LinkedListNode<KeyValuePair<string, IPageCacheItem>>> _cacheItems;
        private readonly LinkedList<KeyValuePair<string, IPageCacheItem>> _accessList;
 
        public event PreEvictionDelegate BeforeEvict;
        public event PostEvictionDelegate AfterEvict;

        public LruPageCache(int limit)
        {
            _highWaterMark = (int) (limit*0.95);
            _lowWaterMark = (int) (limit*0.80);
            _cacheItems = new Dictionary<string, LinkedListNode<KeyValuePair<string, IPageCacheItem>>>(limit);
            _accessList = new LinkedList<KeyValuePair<string, IPageCacheItem>>();
        }

        public void InsertOrUpdate(string partition, IPageCacheItem page)
        {
            var cacheKey = MakeCacheKey(partition, page.Id);
            var cacheEntry = new KeyValuePair<string, IPageCacheItem>(partition, page);
            LinkedListNode<KeyValuePair<string, IPageCacheItem>> cacheNode;
            lock (_updateLock)
            {
                if (_cacheItems.TryGetValue(cacheKey, out cacheNode))
                {
                    cacheNode.Value = cacheEntry;
                    _accessList.Remove(cacheNode);
                    _accessList.AddFirst(cacheNode);
                }
                else
                {
                    var listNode = new LinkedListNode<KeyValuePair<string, IPageCacheItem>>(cacheEntry);
                    _accessList.AddFirst(listNode);
                    _cacheItems[cacheKey] = listNode;
                    _count++;
                    if (_count > _highWaterMark)
                    {
                        EvictItems();
                    }
                }
            }
        }

        public IPageCacheItem Lookup(string partition, ulong pageId)
        {
            var cacheKey = MakeCacheKey(partition, pageId);
            lock (_updateLock)
            {
                LinkedListNode<KeyValuePair<string, IPageCacheItem>> cacheNode;
                if (_cacheItems.TryGetValue(cacheKey, out cacheNode))
                {
                    _accessList.Remove(cacheNode);
                    _accessList.AddFirst(cacheNode);
                    return cacheNode.Value.Value;
                }
            }
            return null;
        }

        public void Clear(string partition)
        {
            lock (_updateLock)
            {
                var p = _accessList.First;
                while (p != null)
                {
                    if (p.Value.Key.Equals(partition))
                    {
                        var cacheKey = MakeCacheKey(partition, p.Value.Value.Id);
                        var tmp = p.Next;
                        _accessList.Remove(p);
                        _cacheItems.Remove(cacheKey);
                        p = tmp;
                    }
                    else
                    {
                        p = p.Next;
                    }
                }
            }
        }

        private void EvictItems()
        {
            var evictPointer = _accessList.Last;
            while (evictPointer != null && _count > _lowWaterMark)
            {
                var partition = evictPointer.Value.Key;
                var pageId = evictPointer.Value.Value.Id;
                var evictionArgs = new EvictionEventArgs(partition, pageId);
                FireBeforeEvict(evictionArgs);
                if (!evictionArgs.CancelEviction)
                {
                    var cacheKey = MakeCacheKey(partition, pageId);
                    var tmp = evictPointer.Previous;
                    _accessList.Remove(evictPointer);
                    _cacheItems.Remove(cacheKey);
                    _count--;
                    FireAfterEvict(evictionArgs);
                    evictPointer = tmp;
                }
                else
                {
                    evictPointer = evictPointer.Previous;
                }
            }
        }

        private void FireAfterEvict(EvictionEventArgs evictionArgs)
        {
            if (AfterEvict != null) AfterEvict(this, evictionArgs);
        }

        private void FireBeforeEvict(EvictionEventArgs evictionArgs)
        {
            if (BeforeEvict != null) BeforeEvict(this, evictionArgs);
        }

        private string MakeCacheKey(string partition, ulong pageId)
        {
            return String.Format("{0}:{1}", partition, pageId);
        }


    }

}
