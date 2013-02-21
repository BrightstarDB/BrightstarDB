using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class ObjectRef : IStorable
    {
        private ulong _objectId;

        /// <summary>
        /// Needed for serialization
        /// </summary>
        public ObjectRef()
        {
        }

        public ObjectRef(ulong objectId)
        {
            _objectId = objectId;
        }

        public ulong ObjectId
        {
             get { return _objectId; }
        }

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            return SerializationUtils.WriteVarint(dataStream, _objectId);
        }

        public void Read(BinaryReader dataStream)
        {
            _objectId = SerializationUtils.ReadVarint(dataStream);
        }       
    }
}
