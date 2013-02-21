using System;
using System.IO;

namespace BrightstarDB.Storage.TransactionLog
{
    internal class TransactionInfo : ITransactionInfo
    {
        public const int TransactionInfoRecordSize = 52;

        public int VersionNumber { get; set; }
        public Guid JobId { get; set; }
        public TransactionStatus TransactionStatus { get; set; }
        public TransactionType TransactionType { get; set; }
        public ulong DataStartPosition { get; set; }
        public ulong DataLength { get; set; }
        public DateTime TransactionStartTime { get; set; }
        
        public TransactionInfo(Guid txnJobId, TransactionStatus txnStatus, TransactionType txnType, ulong dataStart, ulong dataLength, DateTime txnStartTime)
        {
            VersionNumber = 1;
            JobId = txnJobId;
            TransactionStatus = txnStatus;
            TransactionType = txnType;
            DataStartPosition = dataStart;
            DataLength = dataLength;
            TransactionStartTime = txnStartTime.ToUniversalTime();
        }

        private TransactionInfo()
        {
            
        }

        public int Save(BinaryWriter dataStream, ulong offset = 0u)
        {
            dataStream.Write(VersionNumber); // 4 bytes
            dataStream.Write(JobId.ToByteArray()); // 16 bytes
            dataStream.Write(TransactionStartTime.Ticks); // 8 bytes
            dataStream.Write((int)TransactionType); // 4 bytes
            dataStream.Write((int)TransactionStatus); // 4 bytes
            dataStream.Write(DataStartPosition); // 8 bytes
            dataStream.Write(DataLength); // 8 bytes
            return TransactionInfoRecordSize;
        }

        /// <summary>
        /// Loads a TransactionInfo data structure from the current position in the specified stream
        /// </summary>
        /// <param name="dataStream">The data stream to read from</param>
        /// <returns>The TransactionInfo data structure read.</returns>
        public static TransactionInfo Load(BinaryReader dataStream)
        {
            var ret = new TransactionInfo();
            ret.Read(dataStream);
            return ret;
        }

        public void Read(BinaryReader dataStream)
        {
            VersionNumber = dataStream.ReadInt32();
            if (VersionNumber == 1)
            {
                var guidBytes = dataStream.ReadBytes(16);
                JobId = new Guid(guidBytes);
                var ticks = dataStream.ReadInt64();
                TransactionStartTime = new DateTime(ticks);
                TransactionType = (TransactionType)dataStream.ReadInt32();
                TransactionStatus = (TransactionStatus)dataStream.ReadInt32();
                DataStartPosition = dataStream.ReadUInt64();
                DataLength = dataStream.ReadUInt64();

            }
            else
            {
                throw new BrightstarInternalException("Invalid TransactionInfo structure version: " + VersionNumber);
            }
        }
    }
}
