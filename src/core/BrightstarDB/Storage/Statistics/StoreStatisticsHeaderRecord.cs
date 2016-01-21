using System;
using System.IO;

namespace BrightstarDB.Storage.Statistics
{
    internal class StoreStatisticsHeaderRecord
    {
        public int Version { get; set; }
        public ulong CommitNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public long StartOffset { get; set; }
        public long Reserved1 { get; set; }
        public long Reserved2 { get; set; }
        public long Reserved3 { get; set; }
        public static int RecordSize {get { return 52; }}

        public StoreStatisticsHeaderRecord(ulong commitNumber, DateTime timestamp, long startOffset)
        {
            CommitNumber = commitNumber;
            Timestamp = timestamp;
            StartOffset = startOffset;
        }

        private StoreStatisticsHeaderRecord() {  }
        public int Save(BinaryWriter dataStream)
        {
            dataStream.Write(2);
            dataStream.Write(CommitNumber);
            dataStream.Write(Timestamp.Ticks);
            dataStream.Write(StartOffset);
            dataStream.Write(0L);
            dataStream.Write(0L);
            dataStream.Write(0L);
            return RecordSize;
        }

        public static StoreStatisticsHeaderRecord Load(BinaryReader dataStream)
        {
            var ret = new StoreStatisticsHeaderRecord();
            ret.Read(dataStream);
            return ret;
        }

        private void Read(BinaryReader dataStream)
        {
            Version = dataStream.ReadInt32();
            if (Version == 1 || Version == 2)
            {
                CommitNumber = dataStream.ReadUInt64();
                Timestamp = new DateTime(dataStream.ReadInt64());
                StartOffset = dataStream.ReadInt64();
                // Skip reserved space
                dataStream.ReadInt64();
                dataStream.ReadInt64();
                dataStream.ReadInt64();
            }
            else
            {
                throw new BrightstarInternalException("Invalid StoreStatisticsHeaderRecord structure version: " + Version);
            }
        }


    }
}