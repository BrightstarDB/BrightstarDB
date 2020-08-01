using System;
using System.Collections.Generic;
using System.IO;
using VDS.RDF.Query.Datasets;

namespace BrightstarDB.Storage.Persistence
{
    internal class BinaryFilePage : IPage
    {
        public ulong Id { get; private set; }
        public ulong FirstTransactionId { get; private set; }
        public ulong SecondTransactionId { get; private set; }
        public byte[] FirstBuffer { get; private set; }
        public byte[] SecondBuffer { get; private set; }
        public bool IsWriteable { get; private set; }
        private readonly int _nominalPageSize;

        private static readonly object LoadLock = new object();

        public BinaryFilePage(Stream inputStream, ulong pageId, int nominalPageSize, ulong currentTxnId, bool isWriteable)
        {
            _nominalPageSize = nominalPageSize;
            Id = pageId;
            var pages = new byte[nominalPageSize * 2];
            var startOffset = nominalPageSize*2*((long) pageId - 1);
            lock (LoadLock)
            {
                inputStream.Seek(startOffset, SeekOrigin.Begin);
                inputStream.Read(pages, 0, nominalPageSize*2);
            }
            FirstTransactionId = BitConverter.ToUInt64(pages, 0);
            SecondTransactionId = BitConverter.ToUInt64(pages, nominalPageSize);
            FirstBuffer = new byte[nominalPageSize-8];
            Array.Copy(pages, 8, FirstBuffer, 0, nominalPageSize-8);
            SecondBuffer = new byte[nominalPageSize-8];
            Array.Copy(pages, nominalPageSize + 8, SecondBuffer, 0, nominalPageSize - 8);
            Data = GetCurrentBuffer(currentTxnId);
            Logging.LogDebug("BinaryFilePage: Load {0} [{1}|{2}] @ txn {3}", Id, FirstTransactionId, SecondTransactionId, currentTxnId);
            if (isWriteable)
            {
                MakeWriteable(currentTxnId);
            }
        }

        public BinaryFilePage(ulong pageId, int nominalPageSize, ulong currentTxnId)
        {
            _nominalPageSize = nominalPageSize;
            Id = pageId;
            FirstTransactionId = currentTxnId;
            FirstBuffer = new byte[nominalPageSize-8];
            SecondTransactionId = 0;
            SecondBuffer = new byte[nominalPageSize-8];
            Data = FirstBuffer;
            IsWriteable = true;
            Logging.LogDebug("BinaryFilePage: Create {0} [{1}|{2}] @ txn {3}", Id, FirstTransactionId, SecondTransactionId, currentTxnId);
        }

        private BinaryFilePage(BinaryFilePage readOnlyPage, ulong writeTxnId)
        {
            _nominalPageSize = readOnlyPage._nominalPageSize;
            Id = readOnlyPage.Id;
            FirstTransactionId = readOnlyPage.FirstTransactionId;
            SecondTransactionId = readOnlyPage.SecondTransactionId;
            FirstBuffer = new byte[_nominalPageSize-8];
            SecondBuffer = new byte[_nominalPageSize-8];
            Array.Copy(readOnlyPage.FirstBuffer, FirstBuffer, _nominalPageSize-8);
            Array.Copy(readOnlyPage.SecondBuffer, SecondBuffer, _nominalPageSize-8);
            _MakeWriteable(writeTxnId);
            Logging.LogDebug("BinaryFilePage: Create writeable copy {0} [{1}|{2}] @ {3}", Id, FirstTransactionId, SecondTransactionId, writeTxnId);
        }


        public byte[] GetCurrentBuffer(ulong currentTransactionId)
        {
            if (FirstTransactionId > currentTransactionId && SecondTransactionId > currentTransactionId)
            {
                // This is an error condition that can happen if a store is kept open while two successive writes are committed
                throw new ReadWriteStoreModifiedException();
            }

            // Current buffer is the one with the highest transaction id that is less than or equal to currentTransactionId
            if (FirstTransactionId > SecondTransactionId)
            {
                if (FirstTransactionId <= currentTransactionId)
                {
                    return FirstBuffer;
                }
                return SecondBuffer;
            }
            if (SecondTransactionId <= currentTransactionId)
            {
                return SecondBuffer;
            }
            return FirstBuffer;
        }

        public BinaryFilePage MakeWriteable(ulong writeTransactionId)
        {
            return IsWriteable ? this : new BinaryFilePage(this, writeTransactionId);
        }

        private void _MakeWriteable(ulong writeTransactionId)
        {

            var readTransactionId = writeTransactionId - 1;
            byte[] srcBuffer, destBuffer;
            if (FirstTransactionId > readTransactionId && SecondTransactionId > readTransactionId)
            {
                // This is an error condition that can happen if a store is kept open while two successive writes are committed
                throw new ReadWriteStoreModifiedException();
            }

            // Figure out which buffer we will write to and update its transaction id
            // Normally it will be the one with the lower txn id UNLESS the other
            // buffer's transaction ID is greater than or equal to the write transaction id
            // If that is the case we assume that the other buffer is left from a previous failed
            // transaction and overwrite it.
            if (FirstTransactionId < SecondTransactionId)
            {
                if (SecondTransactionId >= writeTransactionId)
                {
                    SecondTransactionId = writeTransactionId;
                }
                else
                {
                    FirstTransactionId = writeTransactionId;
                }
            }
            else
            {
                if (FirstTransactionId >= writeTransactionId)
                {
                    FirstTransactionId = writeTransactionId;
                }
                else
                {
                    SecondTransactionId = writeTransactionId;
                }
            }

            // Figure out which way round to do the copy
            if (FirstTransactionId == writeTransactionId)
            {
                srcBuffer = SecondBuffer;
                destBuffer = FirstBuffer;
            }
            else
            {
                srcBuffer = FirstBuffer;
                destBuffer = SecondBuffer;
            }
            // Copy the read buffer for the immediately preceding transaction id to the other buffer for use in the write transaction
            lock (this)
            {
                Array.Copy(srcBuffer, destBuffer, _nominalPageSize - 8);
                Data = destBuffer;
                IsWriteable = true;
            }

            Logging.LogDebug("BinaryFilePage: MakeWriteable {0} [{1}|{2}] @ writeTxn {3}", Id, FirstTransactionId,
                SecondTransactionId, writeTransactionId);
        }

        public byte[] Data { get; private set; }
        public bool IsDirty { get; internal set; }
        public long Modified { get; private set; }
        public bool Deleted { get; private set; }

        public void SetData(byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1)
        {
            lock (this)
            {
                if (!IsWriteable) throw new InvalidOperationException("Attempt to write to a read-only page");
                Array.ConstrainedCopy(data, srcOffset, Data, pageOffset, len < 0 ? data.Length : len);
                IsDirty = true;
            }
        }

        public long Write(Stream outputStream, ulong currentTransactionId)
        {
            lock (this)
            {
                var offset = _nominalPageSize*2*((long) Id - 1);
                outputStream.Seek(offset, SeekOrigin.Begin);
                outputStream.Write(BitConverter.GetBytes(FirstTransactionId), 0, 8);
                outputStream.Write(FirstBuffer, 0, _nominalPageSize - 8);
                outputStream.Write(BitConverter.GetBytes(SecondTransactionId), 0, 8);
                outputStream.Write(SecondBuffer, 0, _nominalPageSize - 8);
                IsDirty = false;
                Logging.LogDebug("BinaryFilePage: Write {0} [{1}|{2}] @ txn {3}", Id, FirstTransactionId, SecondTransactionId, currentTransactionId);
                return 0L;
            }
        }

        public long WriteIfModifiedSince(long writeTimestamp, Stream outputStream, ulong transactionId)
        {
            throw new NotImplementedException();
        }

    }
}