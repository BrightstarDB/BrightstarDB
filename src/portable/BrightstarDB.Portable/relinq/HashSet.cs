using System;
using System.Collections.Generic;

namespace Remotion.Linq
{
    public class HashSet<T>
    {
        private readonly Dictionary<T, short> _dictionary;
 
        //private readonly Dictionary<int, List<T>> _buckets;
        //private int _count;

        public HashSet()
        {
            _dictionary = new Dictionary<T, short>();
            //_buckets = new Dictionary<int, List<T>>();
        } 

        public void Add(T value)
        {
            _dictionary.Add(value, 0);
            /*
            List<T> bucket;
            if (_buckets.TryGetValue(value.GetHashCode(), out bucket))
            {
                if (bucket.Contains(value))
                {
                    return;
                } else
                {
                    bucket.Add(value);
                    _count++;
                }
            }
            else
            {
                bucket = new List<T>(1) {value};
                _buckets[value.GetHashCode()] = bucket;
                _count++;
            }
             */
        }
        public void Clear()
        {
            _dictionary.Clear();
            /*
            _buckets.Clear();
            _count = 0;
             */
        }

        public bool Contains(T value)
        {
            return _dictionary.ContainsKey(value);
            /*
            List<T> bucket;
            if (_buckets.TryGetValue(value.GetHashCode(), out bucket))
            {
                return bucket.Contains(value);
            }
            return false;
             */
        }

        public int Count { get
        {
            //return _count;
            return _dictionary.Count;
        } }
    }

    public static class Trace

{
    public static void Assert(bool condition, string msg = null)
    {
        if (!condition) throw new Exception("Assert failed. " + msg);
    }
}
}
