using System;
using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class RelatedResource : IComparable, IStorable
    {
        public List<ulong> Graph;

        public ulong Rid;

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            var count = SerializationUtils.WriteVarint(dataStream, Rid);
            count += SerializationUtils.WriteVarint(dataStream, (ulong)Graph.Count);

            foreach (var l in Graph)
            {
                count += SerializationUtils.WriteVarint(dataStream, l);
            }
            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            Rid = SerializationUtils.ReadVarint(dataStream);

            var graphCount = SerializationUtils.ReadVarint(dataStream);

            Graph = new List<ulong>();
            for (ulong i=0;i < graphCount;i++)
            {
                Graph.Add(SerializationUtils.ReadVarint(dataStream));
            }
        }

        public override bool Equals(object obj)
        {
            var robl = obj as RelatedResource;
            if (robl != null && robl.Rid == Rid)
            {
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Rid.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            var other = (RelatedResource) obj;
            return Rid.CompareTo(other.Rid);
        }

    }
}
