using System.Collections.Generic;
using System.IO;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class Bucket : IStorable
    {
        public List<Resource> Resources;

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            dataStream.Write(Resources.Count);
            var dataWritten = 0;
            foreach (var resource in Resources)
            {
                dataWritten += resource.Save(dataStream);
            }
            return 4 + dataWritten;
        }

        public void Read(BinaryReader dataStream)
        {
            var count = dataStream.ReadInt32();
            Resources = new List<Resource>();
            for (int i=0;i<count;i++)
            {
                var obj = new Resource();
                obj.Read(dataStream);
                Resources.Add(obj);
            }
        }

    }
}
