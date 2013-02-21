using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class Entry<T> : IComparer<Entry<T>>, IStorable  where T : class, IStorable
    {
        private ulong _key;        
        private T _value;

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            // dataStream.Write(_key);
            var count = SerializationUtils.WriteVarint(dataStream, _key); 
    
            // val could be null so write a status bool
            dataStream.Write(_value != null);

            var valueBytesWritten = 0;
            if (_value != null)
            {
                valueBytesWritten = _value.Save(dataStream);
            }

            return count + 1 + valueBytesWritten;
        }

        public void Read(BinaryReader dataStream)
        {
            // _key = dataStream.ReadInt64();
            _key = SerializationUtils.ReadVarint(dataStream);
            bool hasData = dataStream.ReadBoolean();
            if (hasData)
            {
                var obj = Activator.CreateInstance<T>();
                obj.Read(dataStream);
                _value = obj;
            }
        }

        public Entry(ulong key, T value)
        {
            _key = key;
            _value = value;
        }

        public Entry(ulong key)
        {
            _key = key;
        }

        public Entry()
        {
        }

        public IStore Store
        {
            get;
            set;
        }

        public override int GetHashCode()
        {
            return _key.GetHashCode();
        }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <returns>
        /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, as shown in the following table.Value Meaning Less than zero<paramref name="x"/> is less than <paramref name="y"/>.Zero<paramref name="x"/> equals <paramref name="y"/>.Greater than zero<paramref name="x"/> is greater than <paramref name="y"/>.
        /// </returns>
        /// <param name="x">The first object to compare.</param><param name="y">The second object to compare.</param>
        public int Compare(Entry<T> x, Entry<T> y)
        {
            return x._key.CompareTo(y._key);
        }

        
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            var e = obj as Entry<T>;
            if (e != null)
            {
                return e._key == _key;
            }

            return false;
        }

        public ulong Key
        {
            get { return _key; }
        }

        public virtual T Value
        {
            get
            {
                return _value;
            }
        }

     }
}
