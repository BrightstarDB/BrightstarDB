namespace BrightstarDB.Utils
{
    internal class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _index;
        public int Count { get; private set; }
        public int Capacity { get; private set; }
        private readonly object _lock = new object();

        public CircularBuffer(int capacity)
        {
            _buffer= new T[capacity];
            _index = 0;
            Capacity = capacity;
        }

        public int Insert(T item)
        {
            lock (_lock)
            {
                int insertIndex = _index++;
                _buffer[insertIndex] = item;
                _index = _index%Capacity;
                if (Count < Capacity) Count++;
                return insertIndex;
            }
        }

        public T this[int ix]
        {
            get { lock(_lock) {return _buffer[ix];} }
        }

        public T ItemAt(int ix)
        {
            return this[ix];
        }

        public void Clear()
        {
            lock (_lock)
            {
                _index = 0;
                Count = 0;
            }
        }
    }
}
