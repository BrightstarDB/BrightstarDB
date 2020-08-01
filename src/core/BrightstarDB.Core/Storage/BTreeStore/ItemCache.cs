using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class ItemCache
    {
        private readonly ulong _dictCount;
        private readonly int _maxEntriesPerDict;
        private readonly Dictionary<ulong , WeakReference>[] _itemDictionaries;
        private readonly bool _clearAtMax;

        public ItemCache(int dictCount, int maxCount, bool clearAtMax)
        {
            _clearAtMax = clearAtMax;
            _dictCount = (ulong)dictCount;
            _maxEntriesPerDict = maxCount;
            _itemDictionaries = new Dictionary<ulong, WeakReference>[dictCount];
            for(int i = 0; i < dictCount; i++)
            {
                _itemDictionaries[i] = new Dictionary<ulong, WeakReference>(maxCount/10);
            }
        }

        public int CachedItemCount
        {
            get
            {
                return _itemDictionaries.Sum(itemDictionary => itemDictionary.Count);
            }            
        }

        public void Add(IPersistable p)
        {
            var dict = _itemDictionaries[p.ObjectId%_dictCount];
            if (_clearAtMax && dict.Count >= _maxEntriesPerDict)
            {
                dict.Clear();
                return;
            }
            try
            {
                if (!dict.ContainsKey(p.ObjectId))
                {
                    dict.Add(p.ObjectId, new WeakReference(p));
                }
            }
            catch (OutOfMemoryException)
            {
                if (_clearAtMax)
                {
                    dict.Clear();
                    dict.Add(p.ObjectId, new WeakReference(p));
                }
                else
                {
                    throw;
                }
            }
        }

        public bool TryGetValue(ulong index, out IPersistable p)
        {
            WeakReference weakRef;
            var dict = _itemDictionaries[index%_dictCount];
            if (dict.TryGetValue(index, out weakRef))
            {
                if (weakRef.IsAlive)
                {
                    p = weakRef.Target as IPersistable;
                    if (p != null) return true;
                }
                // Either weakref is not live any more or the item it resolves to is not an IPersistable
                // In either case we want to clear out this cache entry and return false.
                dict.Remove(index);
                p = null;
                return false;
            }
            p = null;
            return false;
        }

        public void Clear()
        {
            foreach(var dict in _itemDictionaries)
            {
                dict.Clear();
            }
        }
    }
}