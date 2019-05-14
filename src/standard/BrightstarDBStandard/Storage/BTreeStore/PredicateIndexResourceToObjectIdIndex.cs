using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class PredicateIndexResourceToObjectIdIndex : IStorable
    {
        public struct Entry : IComparable
        {
            public readonly ulong ResourceId;
            public readonly ulong IndexObjectId;

            public Entry(ulong resourceId, ulong indexObjectId)
            {
                ResourceId = resourceId;
                IndexObjectId = indexObjectId;
            }

            public override bool Equals(object obj)
            {
                var e = (Entry) obj;
                return ResourceId.Equals(e.ResourceId);
            }

            public override int GetHashCode()
            {
                return ResourceId.GetHashCode();
            }

            #region Implementation of IComparable

            /// <summary>
            /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
            /// </summary>
            /// <returns>
            /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than <paramref name="obj"/>. Zero This instance is equal to <paramref name="obj"/>. Greater than zero This instance is greater than <paramref name="obj"/>. 
            /// </returns>
            /// <param name="obj">An object to compare with this instance. </param><exception cref="T:System.ArgumentException"><paramref name="obj"/> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
            public int CompareTo(object obj)
            {
                var e = (Entry) obj;
                return ResourceId.CompareTo(e.ResourceId);
            }

            #endregion
        }

        /// <summary>
        /// Ordered list of entries.
        /// </summary>
        private List<Entry> _entries;

        public PredicateIndexResourceToObjectIdIndex()
        {
            _entries = new List<Entry>(100);
        }

        public void InsertEntry(ulong resourceId, ulong objectId)
        {
            var entry = new Entry(resourceId, objectId);
            var loc = _entries.BinarySearch(entry);
            if (loc < 0)
            {
                _entries.Insert(~loc, entry);
                return;
            }

            throw new BrightstarInternalException("Entry already in index");
        }

        public ulong GetObjectId(ulong resourceId)
        {
            var entry = new Entry(resourceId, 0);
            var loc = _entries.BinarySearch(entry);
            if (loc >= 0)
            {
                return _entries[loc].IndexObjectId;
            }

            throw new BrightstarInternalException("Key not in index");                        
        }

        public IEnumerable<Entry> Entries
        {
            get { return _entries; }
        }

        public bool TryGetValue(ulong resourceId, out ulong objectId)
        {
            var entry = new Entry(resourceId, 0);
            var loc = _entries.BinarySearch(entry);
            if (loc >= 0)
            {
                objectId = _entries[loc].IndexObjectId;
                return true;
            }
            objectId = StoreConstants.NullUlong;
            return false;
        } 

        #region Implementation of IStorable

        /// <summary>
        /// Stores the objects into the stream returning the number of bytes written
        /// </summary>
        /// <param name="dataStream">The stream</param>
        /// <param name="offset">Passed through in some situations where the internal serialiser needs to know</param>
        /// <returns>Total number of bytes written</returns>
        public int Save(BinaryWriter dataStream, ulong offset)
        {
            // output the count
            var count = SerializationUtils.WriteVarint(dataStream, (ulong) _entries.Count);

            // output all the entries
            foreach (var entry in _entries)
            {
                count += SerializationUtils.WriteVarint(dataStream, entry.ResourceId);
                count += SerializationUtils.WriteVarint(dataStream, entry.IndexObjectId);                
            }

            return count;
        }

        /// <summary>
        /// Load the state data from the stream provided.
        /// </summary>
        /// <param name="dataStream">Datastream containing the data</param>
        public void Read(BinaryReader dataStream)
        {
            var count = SerializationUtils.ReadVarint(dataStream);

            _entries = new List<Entry>();

            for (ulong i=0;i< count;i++)            
            {
                var resourceId = SerializationUtils.ReadVarint(dataStream);
                var objectId = SerializationUtils.ReadVarint(dataStream);
                _entries.Add(new Entry(resourceId, objectId));
            }
        }

        #endregion
    }
}
