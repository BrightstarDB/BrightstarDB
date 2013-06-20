using System;
using System.IO;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    class MemoryPage : IPage
    {
        public ulong Id { get; private set; }
        public byte[] Data { get; private set; }
        public bool IsDirty { get; private set; }
        public long Modified { get; private set; }
        public bool Deleted { get; private set; }

        public MemoryPage(ulong id, int pageSize)
        {
            Data = new byte[pageSize];
            Id = id;
        }

        public void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1)
        {
            Array.ConstrainedCopy(data, srcOffset, Data, pageOffset, len < 0 ? data.Length : len);
        }

        public long Write(Stream outputStream, ulong transactionId)
        {
            throw new NotImplementedException();
        }

        public long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId)
        {
            throw new NotImplementedException();
        }
    }
}