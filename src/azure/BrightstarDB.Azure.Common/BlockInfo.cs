using System;
using System.IO;
using System.Runtime.Serialization;
using BrightstarDB.Caching;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Azure.Common
{
    /// <summary>
    /// Common data structure for file blocks
    /// </summary>
    [DataContract]
    [Serializable]
    public class BlockInfo : IBinarySerializable, IBlockInfo
    {
        /// <summary>
        /// The name of the file that the block is a part of
        /// </summary>
        [DataMember]
        public string StoreName { get; set; }

        /// <summary>
        /// The file offest (in bytes) of the start of this block 
        /// </summary>
        [DataMember]
        public long Offset { get; set; }

        /// <summary>
        /// The amount of valid data (in bytes) contained in this block
        /// </summary>
        [DataMember]
        public int Length { get; set; }

        /// <summary>
        /// The block data
        /// </summary>
        [DataMember]
        public byte[] Data { get; set; }

        /// <summary>
        /// Timestamp in ticks when this block was last accessed
        /// </summary>
        /// <remarks>This property is used for local caching and is not serialized over the wire</remarks>
        public long LastAccess { get; set; }

        public override string ToString()
        {
            return String.Format("<BlockInfo: {0}@{1} with length {2}>", StoreName, Offset, Length);
        }

        /// <summary>
        /// Provides a readonly stream to access the content of this block
        /// </summary>
        /// <returns></returns>
        public Stream OpenStream()
        {
            return new MemoryStream(Data, 0, Length, false);
        }

        #region Implementation of IBinarySerializable

        /// <summary>
        /// Writes the binary serialization of the object to <paramref name="dataStream"/>
        /// </summary>
        /// <param name="dataStream"></param>
        /// <returns>The number of bytes written</returns>
        public int Save(Stream dataStream)
        {
            var offset = dataStream.Position;
            var w = new BinaryWriter(dataStream);
            w.Write(StoreName);
            w.Write(Offset);
            w.Write(Length);
            w.Write(Data, 0, Length);
            w.Flush();
            return (int)(dataStream.Position - offset);
        }

        /// <summary>
        /// Reads the binary serialization of the object from <paramref name="dataStream"/>
        /// </summary>
        /// <param name="dataStream"></param>
        public void Read(Stream dataStream)
        {
            var r= new BinaryReader(dataStream);
            StoreName = r.ReadString();
            Offset = r.ReadInt64();
            Length = r.ReadInt32();
            Data = new byte[4194304]; // 4MB buffer
            r.Read(Data, 0, Length);
        }

        #endregion

        public BlockInfo Copy()
        {
            var copyData = new byte[this.Data.Length];
            this.Data.CopyTo(copyData, 0);
            return new BlockInfo
                       {
                           StoreName = this.StoreName,
                           Offset = this.Offset,
                           Length = this.Length,
                           LastAccess = this.LastAccess,
                           Data = copyData
                       };
        }
    }
}
