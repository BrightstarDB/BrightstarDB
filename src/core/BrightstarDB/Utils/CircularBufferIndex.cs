using System.Collections.Generic;

namespace BrightstarDB.Utils
{
    internal class IndexedCircularBuffer<TKey, TValue>
    {
        private readonly Dictionary<TKey, int> _index;
        private readonly CircularBuffer<TValue> _values;
        private readonly CircularBuffer<TKey> _keys;
        private readonly object _lock = new object();

        public IndexedCircularBuffer(int capacity)
        {
            _index = new Dictionary<TKey, int>(capacity);
            _values = new CircularBuffer<TValue>(capacity);
            _keys = new CircularBuffer<TKey>(capacity);
        }

        public IEnumerable<TKey> Keys
        {
            get { return _index.Keys; }
        }

        public void Insert(TKey key, TValue value)
        {
            lock (_lock)
            {
                var insertIndex = _values.Insert(value);
                if (insertIndex < _keys.Count)
                {
                    _index.Remove(_keys.ItemAt(insertIndex));
                }
                var keyInsertIndex = _keys.Insert(key);
                if (keyInsertIndex!=insertIndex)
                {
                    Logging.LogError(BrightstarEventId.Undefined, "Key and value buffers got out of sync. Resetting IndexedCircularBuffer");
                    this.Clear();
                }
                _index[key] = insertIndex;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
            {
                int valueIndex;
                if (_index.TryGetValue(key, out valueIndex))
                {
                    value = _values[valueIndex];
                    return true;
                }
                value = default(TValue);
                return false;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _index.Clear();
                _keys.Clear();
                _values.Clear();
            }
        }

        public void Remove(TKey key)
        {
            lock(_lock)
            {
                _index.Remove(key);
            }
        }
    }
}
