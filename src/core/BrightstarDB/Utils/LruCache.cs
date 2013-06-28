using System;
using System.Collections.Generic;

namespace BrightstarDB.Utils
{
    internal class LruCache<TKey, TValue>
    {
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _dictionary;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _accessList;
 
        private readonly int _highWatermark;
        private readonly int _lowWatermark;

        public LruCache(int limit, int highWatermark = 0, int lowWatermark = 0)
        {
            if (limit <= 0)
            {
                throw new ArgumentException("Limit must be greater than 0", "limit");
            }
            if (highWatermark <= 0)
            {
                highWatermark = (int) (limit*0.95);
            }
            if (lowWatermark <= 0)
            {
                lowWatermark = (int) (limit*0.85);
            }

            _highWatermark = highWatermark;
            _lowWatermark = lowWatermark;

            _dictionary = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>(limit);
            _accessList = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public void InsertOrUpdate(TKey key, TValue value)
        {
            lock (this)
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> existing;
                if (_dictionary.TryGetValue(key, out existing))
                {
                    existing.Value = new KeyValuePair<TKey, TValue>(key, value);
                    if (existing.Previous != null)
                    {
                        _accessList.Remove(existing);
                        _accessList.AddFirst(existing);
                    }
                }
                else
                {
                    var node = new LinkedListNode<KeyValuePair<TKey, TValue>>(new KeyValuePair<TKey, TValue>(key, value));
                    _dictionary.Add(key, node);
                    _accessList.AddFirst(node);
                }
                CheckCleanup();
            }
        }

        public bool TryLookup(TKey key, out TValue value)
        {
            lock (this)
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> cacheNode;
                if (_dictionary.TryGetValue(key, out cacheNode))
                {
                    _accessList.Remove(cacheNode);
                    _accessList.AddFirst(cacheNode);
                    value = cacheNode.Value.Value;
                    return true;
                }
            }
            value = default(TValue);
            return false;
        }

        public void Clear()
        {
            lock (this)
            {
                _dictionary.Clear();
                _accessList.Clear();
            }
        }

        public int Count { get { return _dictionary.Count; } }

        private void CheckCleanup()
        {
            if (_dictionary.Count >= _highWatermark)
            {
                Cleanup();
            }
        }

        private void Cleanup()
        {
            var evictPointer = _accessList.Last;
            while (evictPointer != null && _dictionary.Count > _lowWatermark)
            {
                var cacheKey = evictPointer.Value.Key;
                var tmp = evictPointer.Previous;
                _accessList.Remove(evictPointer);
                _dictionary.Remove(cacheKey);
                evictPointer = tmp;
            }
        }
    }
}
