using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class ObjectLocationContainer : IStorable
    {
        private readonly List<ObjectIdOffsetEntry> _objectIdOffsetIndex;

        public bool IsModified { get; set; }

        public IEnumerable<ObjectIdOffsetEntry> ObjectOffsets
        {
            get { return _objectIdOffsetIndex; }
        }

        public ObjectLocationContainer()
        {
            _objectIdOffsetIndex = new List<ObjectIdOffsetEntry>(1000);
        }

        public void SetObjectOffset(ulong objectId, ulong offset, ulong type, ulong version)
        {
            var entry = new ObjectIdOffsetEntry(objectId, offset, type, version);
            var loc = _objectIdOffsetIndex.BinarySearch(entry);
            if (loc < 0)
            {
                // doesn't exist
                _objectIdOffsetIndex.Insert(~loc, entry);
            } else
            {
                _objectIdOffsetIndex[loc] = entry;
            }
            IsModified = true;
        }

        public ulong GetObjectOffset(ulong objectId)
        {
            var testTuple = new ObjectIdOffsetEntry(objectId, 0, 0, 0);
            var loc = _objectIdOffsetIndex.BinarySearch(testTuple);
            if (loc < 0)
            {
                throw new BrightstarInternalException("No offset for object id " + objectId);
            }
            return _objectIdOffsetIndex[loc].Offset;
        }

        internal struct ObjectIdOffsetEntry : IComparable
        {
            public ulong ObjectId;
            public ulong Offset;
            public ulong Type;
            public ulong Version;
            
            public ObjectIdOffsetEntry(ulong objectId, ulong offset, ulong type, ulong version)
            {
                ObjectId = objectId;
                Offset = offset;
                Type = type;
                Version = version;
            }

            public int CompareTo(object obj)
            {
                var entry = (ObjectIdOffsetEntry) obj;
                return ObjectId.CompareTo(entry.ObjectId);
            }

            public override bool Equals(object obj)
            {
                var entry = (ObjectIdOffsetEntry) obj;
                return ObjectId == entry.ObjectId;
            }

            public override int GetHashCode()
            {
                return ObjectId.GetHashCode();
            }
        }

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            // write number of elements
            var count = SerializationUtils.WriteVarint(dataStream, (ulong)_objectIdOffsetIndex.Count);
            foreach (var o in _objectIdOffsetIndex)
            {
                count += SerializationUtils.WriteVarint(dataStream, o.ObjectId);
                count += SerializationUtils.WriteVarint(dataStream, o.Offset);
                count += SerializationUtils.WriteVarint(dataStream, o.Type);
                count += SerializationUtils.WriteVarint(dataStream, o.Version);
            }
            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            var count = (int)SerializationUtils.ReadVarint(dataStream);
            for (int i=0;i < count; i++)
            {
                var objectId = SerializationUtils.ReadVarint(dataStream);
                var offset = SerializationUtils.ReadVarint(dataStream);
                var type = SerializationUtils.ReadVarint(dataStream);
                var version = SerializationUtils.ReadVarint(dataStream);
                var entry = new ObjectIdOffsetEntry(objectId, offset, type, version);
                _objectIdOffsetIndex.Add(entry);
            }
        }

        public void DeleteObjectOffset(ulong objectId)
        {
            var entry = new ObjectIdOffsetEntry(objectId, 0, 0, 0);
            var loc = _objectIdOffsetIndex.BinarySearch(entry);
            if (loc < 0) return;
            _objectIdOffsetIndex.RemoveAt(loc);
            IsModified = true;
        }
    }
}
