using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class Node<T> : IPersistable where T : class, IStorable
    {
        private ulong _nodeId;

        private ulong _parentNodeId;

        public List<ulong> ChildNodes;
        public List<Entry<T>> Keys;

        public Node(ulong nodeId, ulong parentNodeId, int keyCount)
        {
            _nodeId = nodeId;
            _parentNodeId = parentNodeId;
            Keys = new List<Entry<T>>(keyCount);
            ChildNodes = new List<ulong>(keyCount + 1);
        }

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            var count = SerializationUtils.WriteVarint(dataStream, _nodeId);
            count += SerializationUtils.WriteVarint(dataStream, _parentNodeId);
            count += SerializationUtils.WriteVarint(dataStream, (ulong) ChildNodes.Count);
            foreach (var childNode in ChildNodes)
            {
                count += SerializationUtils.WriteVarint(dataStream, childNode);
            }

            var entryDataWritten = 0;
            count += SerializationUtils.WriteVarint(dataStream, (ulong)Keys.Count);
            foreach (var entry in Keys)
            {
                entryDataWritten += entry.Save(dataStream);
            }

            var bytesWritten = count + entryDataWritten;
            return bytesWritten;
        }

        public void Read(BinaryReader dataStream)
        {
            _nodeId = SerializationUtils.ReadVarint(dataStream);
            _parentNodeId = SerializationUtils.ReadVarint(dataStream);
            var childNodeCount = SerializationUtils.ReadVarint(dataStream);
            ChildNodes = new List<ulong>();
            for (var i=0ul; i < childNodeCount; i++)
            {
                ChildNodes.Add(SerializationUtils.ReadVarint(dataStream));
            }

            var entryCount = (int)SerializationUtils.ReadVarint(dataStream);
            Keys = new List<Entry<T>>();
            for (var i = 0; i < entryCount; i++)
            {
                var entry = new Entry<T>();
                entry.Read(dataStream);
                entry.Store = Store;
                Keys.Add(entry);
            }
        }

        public ulong NodeId
        {
            get { return _nodeId; }
        }

        public ulong ParentNodeId
        {
            get { return _parentNodeId; }
            set { _parentNodeId = value; }
        }

        public bool IsRoot
        {
            get { return _parentNodeId == StoreConstants.NullUlong; }
        }

        /// <summary>
        /// Default constructor for serialisation
        /// </summary>
        public Node()
        {
            Keys = new List<Entry<T>>();
            ChildNodes = new List<ulong>();
        }

        public ulong ObjectId
        {
            get { return _nodeId; }
            set { _nodeId = value; }
        }

        public bool ScheduledForCommit
        {
            get; set;
        }

        private Store _store;

        public IStore Store
        {
            get { return _store; }

            set { _store = value as Store; }
        }
    }
}
