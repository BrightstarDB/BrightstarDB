using System;
using System.IO;
using BrightstarDB.Client;

namespace BrightstarDB.Storage.Persistence
{
    internal class BinaryFilePage : IPageCacheItem
    {
        public ulong Id { get; private set; }
        public ulong FirstTransactionId { get; private set; }
        public ulong SecondTransactionId { get; private set; }
        public byte[] FirstBuffer { get; private set; }
        public byte[] SecondBuffer { get; private set; }
        private readonly int _nominalPageSize;

        public BinaryFilePage(Stream inputStream, ulong pageId, int nominalPageSize)
        {
            _nominalPageSize = nominalPageSize;
            Id = pageId;
            long startOffset = nominalPageSize*2*((long) pageId - 1);
            long seekOffset = startOffset - inputStream.Position;
            inputStream.Seek(seekOffset, SeekOrigin.Current);
            //inputStream.Seek(nominalPageSize * 2 * ((long)pageId - 1), SeekOrigin.Begin);
            var pages = new byte[nominalPageSize*2];
            inputStream.Read(pages, 0, nominalPageSize*2);
            FirstTransactionId = BitConverter.ToUInt64(pages, 0);
            SecondTransactionId = BitConverter.ToUInt64(pages, nominalPageSize);
            FirstBuffer = new byte[nominalPageSize-8];
            Array.Copy(pages, 8, FirstBuffer, 0, nominalPageSize-8);
            SecondBuffer = new byte[nominalPageSize-8];
            Array.Copy(pages, nominalPageSize + 8, SecondBuffer, 0, nominalPageSize - 8);
        }

        public BinaryFilePage(ulong pageId, int nominalPageSize, ulong currentTransactionId)
        {
            _nominalPageSize = nominalPageSize;
            Id = pageId;
            FirstTransactionId = ulong.MaxValue;// This ensures we should always write to FirstBuffer for a new page
            FirstBuffer = new byte[nominalPageSize-8];
            SecondTransactionId = 0;
            SecondBuffer = new byte[nominalPageSize-8];
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

        public void MakeWriteable(ulong writeTransactionId)
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
            Array.Copy(srcBuffer, destBuffer, _nominalPageSize);
        }

        public byte[] GetReadBuffer(ulong currentTransactionId)
        {
            if (FirstTransactionId > currentTransactionId && SecondTransactionId > currentTransactionId)
            {
                // This is an error condition that would happen if a store is kept open while two successive writes are committed
                throw new ReadWriteStoreModifiedException();
            }
            if (FirstTransactionId > SecondTransactionId)
            {
                // Normally if first transaction id is higher than second, we read from first buffer
                // BUT if first transaction id is higher than the current transaction id, then
                // the first buffer is from a failed transaction, so we read from the second one.
                return FirstTransactionId <= currentTransactionId ? FirstBuffer : SecondBuffer;
            }
            return SecondTransactionId <= currentTransactionId ? SecondBuffer : FirstBuffer;
        }

        public byte[] GetWriteBuffer(ulong currentTransactionId)
        {
            if (FirstTransactionId < SecondTransactionId)
            {
                // Normally if first transaction id is lower, we write to first buffer
                // BUT if second transaction id is equal to or higher than current transaction id, 
                // the second buffer is from this transaction or from a failed transaction, 
                // so it is the one that should be overwritten
                return SecondTransactionId >= currentTransactionId ? SecondBuffer : FirstBuffer;
            }
            return FirstTransactionId > currentTransactionId ? FirstBuffer : SecondBuffer;
        }

        public long Write(Stream outputStream, ulong currentTransactionId)
        {
            if (FirstTransactionId < SecondTransactionId)
            {
                if (SecondTransactionId >= currentTransactionId)
                {
                    WriteSecondBuffer(outputStream, currentTransactionId);
                }
                else
                {
                    WriteFirstBuffer(outputStream, currentTransactionId);
                }
            }
            else
            {
                if (FirstTransactionId >= currentTransactionId)
                {
                    WriteFirstBuffer(outputStream, currentTransactionId);
                }
                else
                {
                    WriteSecondBuffer(outputStream, currentTransactionId);
                }
            }
            return 0L;
        }

        private void WriteFirstBuffer(Stream outputStream, ulong transactionId)
        {
            outputStream.Seek(_nominalPageSize*2*((long) Id - 1), SeekOrigin.Begin);
            outputStream.Write(BitConverter.GetBytes(transactionId), 0, 8);
            outputStream.Write(FirstBuffer, 0, _nominalPageSize-8);
            if (outputStream.Length < _nominalPageSize * 2*(long)Id)
            {
                // Ensure file has reserved space for the second half of the page
                outputStream.Write(BitConverter.GetBytes(0ul), 0, 8);
                outputStream.Write(SecondBuffer, 0, _nominalPageSize-8);
            }
            FirstTransactionId = transactionId;
        }

        private void WriteSecondBuffer(Stream outputStream, ulong transactionId)
        {
            outputStream.Seek((_nominalPageSize*2*((long) Id - 1)) + _nominalPageSize, SeekOrigin.Begin);
            outputStream.Write(BitConverter.GetBytes(transactionId), 0, 8);
            outputStream.Write(SecondBuffer, 0, _nominalPageSize-8);
            SecondTransactionId = transactionId;
        }
    }
}