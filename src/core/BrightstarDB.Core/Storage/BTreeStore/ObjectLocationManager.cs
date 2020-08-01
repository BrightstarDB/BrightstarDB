using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class ObjectLocationManager : IStorable
    {
        private const int ContainerSize = 1000;
        private readonly List<ulong> _containerOffsets;
        private readonly Dictionary<int, ObjectLocationContainer> _objectLocationContainers;
        private string _storeFileName;

        internal string StoreFileName { get { return _storeFileName; } set { _storeFileName = value; } }

        public int LoadedContainerCount
        {
            get { return _objectLocationContainers.Count; }
        }

        public int Save(BinaryWriter dataStream, ulong offset)
        {
            var count = 4;

            var localOffset = offset + (ulong) count;

            var skipCountPos = dataStream.BaseStream.Position;
            dataStream.Write(0);

            // save each container AND update the offset
            foreach (var kvp in _objectLocationContainers)
            {
                var containerNumber = kvp.Key;
                var container = kvp.Value;
                if (container.IsModified)
                {
                    _containerOffsets[containerNumber] = localOffset;
                    var written = container.Save(dataStream);
                    count += written;
                    localOffset += (ulong) written;
                    container.IsModified = false;
                }
            }

            // write the count to skip when reading.
            dataStream.BaseStream.Position = skipCountPos;
            dataStream.Write(count - 4);
            dataStream.Seek(0, SeekOrigin.End);

            // save all the offsets of the containers
            count += SerializationUtils.WriteVarint(dataStream, (ulong) _containerOffsets.Count);
            foreach (var containerOffset in _containerOffsets)
            {
                count += SerializationUtils.WriteVarint(dataStream, containerOffset);
            }

            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            var seekSpace = dataStream.ReadInt32();
            dataStream.BaseStream.Seek(seekSpace, SeekOrigin.Current);
            var containerOffsetCount = (int)SerializationUtils.ReadVarint(dataStream);
            for (var i = 0; i < containerOffsetCount; i++)
            {
                _containerOffsets.Add(SerializationUtils.ReadVarint(dataStream));
            }
        }

        public ObjectLocationManager()
        {
            _objectLocationContainers = new Dictionary<int, ObjectLocationContainer>();
            _containerOffsets = new List<ulong>(1000);
        }

        public int NumberOfContainers
        {
            get { return _objectLocationContainers.Count; }
        }

        public IEnumerable<ObjectLocationContainer> Containers
        {
            get
            {
                var storeManager = StoreManagerFactory.GetStoreManager() as IStoreManager2;
                if (storeManager == null)
                {
                    throw new Exception("Invalid store manager instance returned by store manager factory");
                }
                return _containerOffsets.Select(containerOffset => storeManager.ReadObject<ObjectLocationContainer>(_storeFileName, containerOffset));
            }
        }

        private ObjectLocationContainer GetContainerForObjectId(ulong objectId)
        {
            var containerNumber = (int) (objectId/ContainerSize);

            // see if its loaded
            if (_objectLocationContainers.ContainsKey(containerNumber))
            {
                return _objectLocationContainers[containerNumber];
            }

            // see if there is a offset and load the container
            if (_containerOffsets.Count > containerNumber)
            {
                var offset = _containerOffsets[(int) containerNumber];
                var storeManager = StoreManagerFactory.GetStoreManager() as IStoreManager2;
                if (storeManager == null) throw new Exception("Invalid store manager instance returned by store manager factory");
                var container = storeManager.ReadObject<ObjectLocationContainer>(_storeFileName, offset);
                _objectLocationContainers.Add(containerNumber, container);
                return container;
            }
            else
            {
                // else we create a new one
                var container = new ObjectLocationContainer();
                _objectLocationContainers.Add(containerNumber, container);
                _containerOffsets.Add(0);
                return container;
            }
        }

        public ulong GetObjectOffset(ulong objectId)
        {
            var container = GetContainerForObjectId(objectId);
            if (container != null)
            {
                return container.GetObjectOffset(objectId);                
            }

            throw new BrightstarInternalException("No container for object id " + objectId);
        }

        public void SetObjectOffset(ulong objectId, ulong offset, ulong type, ulong version)
        {
            var container = GetContainerForObjectId(objectId);
            container.SetObjectOffset(objectId, offset, type, version);
        }

        public void DeleteObjectOffset(ulong objectId)
        {
            var container = GetContainerForObjectId(objectId);
            container.DeleteObjectOffset(objectId);
        }
    }
}
