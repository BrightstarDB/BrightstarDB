using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.Mobile.Compatibility
{
    /// <summary>
    /// A replacement for the .NET Framework ConcurrentDictionary
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <remarks>This implementation is more of a "BlockingDictionary" than a concurrent dictionary. It 
    /// maintains thread-safe access to an underlying dictionary through liberal use of lock() sections.
    /// </remarks>
    internal class ConcurrentDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dict;

        public ConcurrentDictionary()
        {
            _dict = new Dictionary<TKey, TValue>();
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                lock (_dict)
                {
                    return _dict.Keys.ToArray();
                }
            }
        } 

        public IEnumerable<TValue> Values
        {
            get
            {
                lock (_dict)
                {
                    return _dict.Values.ToArray();
                }
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (_dict)
            {
                if (_dict.TryGetValue(key, out value))
                {
                    _dict.Remove(key);
                    return true;
                }
                return false;
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_dict)
            {
                return _dict.ContainsKey(key);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_dict)
            {
                return _dict.TryGetValue(key, out value);
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (_dict)
                {
                    return _dict[key];
                }
            }
            set
            {
                lock (_dict)
                {
                    _dict[key] = value;
                }
            }
        }

        public void AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            lock (_dict)
            {
                TValue existing;
                if (_dict.TryGetValue(key, out existing))
                {
                    _dict[key] = updateValueFactory(key, existing);
                }
                else
                {
                    _dict[key] = addValue;
                }
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            lock (_dict)
            {
                TValue value;
                if (!_dict.TryGetValue(key, out value))
                {
                    value = valueFactory(key);
                    _dict[key] = value;
                }
                return value;
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            lock (_dict)
            {
                try
                {
                    _dict.Add(key, value);
                    return true;
                }
                catch (ArgumentNullException)
                {
                    throw;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }
        }

        public void Clear()
        {
            lock (_dict)
            {
                _dict.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_dict)
                {
                    return _dict.Count;
                }
            }
        }
    }
}
