using System;
using BrightstarDB.Caching;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class ObjectCache
    {
        //private readonly ItemCache _highPriorityItems;
        private readonly ItemCache _lowPriorityItems;

        public ObjectCache(bool clearAtMax)
        {
            //_highPriorityItems = new ItemCache(1, 1000, clearAtMax);
            _lowPriorityItems = new ItemCache(100, Configuration.ReadStoreObjectCacheSize, clearAtMax);
        }

        public int CachedObjectCount
        {
            get { return _lowPriorityItems.CachedItemCount; }
        }

        public void Add(IPersistable p, CachePriority priority = CachePriority.Normal)
        {
            //switch (priority)
            //{
            //    case CachePriority.Normal:
            //        _lowPriorityItems.Add(p);
            //        break;
            //    case CachePriority.High:
            //        _highPriorityItems.Add(p);
            //        break;
            //}
            try
            {
                _lowPriorityItems.Add(p);
            } catch(Exception ex)
            {
                Logging.LogWarning(BrightstarEventId.CachingError, "Encountered an error when adding object {0} to cache. Cause: {1}",
                    p.ObjectId, ex);
            }
        }

        public bool TryGetValue(ulong id, out IPersistable p)
        {
            return _lowPriorityItems.TryGetValue(id, out p); // _highPriorityItems.TryGetValue(id, out p); // || _lowPriorityItems.TryGetValue(id, out p);
        }

        public void Clear()
        {
            _lowPriorityItems.Clear();
        }
    }
}
