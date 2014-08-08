using System;
using System.Collections.Generic;

namespace BrightstarDB.Mobile.Compatibility
{
    public class ConcurrentQueue<T>
    {
        private readonly Queue<T> _queue;

        public ConcurrentQueue()
        {
            _queue = new Queue<T>();
        }

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
            }
        }

        public bool TryDequeue(out T item)
        {
            lock (_queue)
            {
                if (_queue != null && _queue.Count > 0)
                {
                    try
                    {
                        item = _queue.Dequeue();
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Suppress
                    }
                }
            }
            item = default(T);
            return false;
        }

        public bool IsEmpty
        {
            get
            {
                lock (_queue)
                {
                    return _queue.Count == 0;
                }
            }
        }
    }
}
