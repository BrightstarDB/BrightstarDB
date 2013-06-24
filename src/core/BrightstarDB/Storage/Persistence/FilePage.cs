using System;
using System.IO;

namespace BrightstarDB.Storage.Persistence
{
    internal class FilePage : IPage, IDisposable
    {
        private long _modified;
        private readonly ulong _writeOffset;
        private readonly int _pageSize;
        private readonly byte[] _data;
        private readonly object _writeLock = new object();

        public FilePage(ulong pageId, int pageSize)
        {
            Id = pageId;
            _data = new byte[pageSize];
            IsDirty = true;
            Deleted = false;
            _modified = 1;
            _writeOffset = (pageId - 1) * (ulong)pageSize; // TODO : could be a bit shift instead?
            _pageSize = pageSize;
        }

        public FilePage(ulong pageId, int pageSize, byte[] data)
        {
            Id = pageId;
            _data = new byte[pageSize];
            data.CopyTo(Data, 0);
            IsDirty = true;
            Deleted = false;
            _modified = 1;
            _writeOffset = (pageId - 1) * (ulong)pageSize; // TODO : could be a bit shift instead?
            _pageSize = pageSize;
        }

        public FilePage(Stream stream, ulong pageId, int pageSize)
        {
            _writeOffset = (pageId -1) * (ulong)pageSize; // TODO : could be a bit shift instead?
            stream.Seek((long)_writeOffset, SeekOrigin.Begin);
            _data = new byte[pageSize];
            stream.Read(Data, 0, pageSize);
            IsDirty = false;
            Deleted = false;
            Id = pageId;
            _modified = 0;
            _pageSize = pageSize;
        }

        #region Implementation of IPage

        public ulong Id { get; private set; }

        public byte[] Data { get { return _data; } }

        public bool IsDirty { get; private set; }

        /// <summary>
        /// Gets the modification count for this page
        /// </summary>
        public long Modified
        {
            get { return _modified; }
        }

        public bool Deleted { get; set; }


        /// <summary>
        /// Update the page data
        /// </summary>
        /// <param name="data">The data buffer to copy from</param>
        /// <param name="srcOffset">The offset in <paramref name="data"/> to start copying from </param>
        /// <param name="pageOffset">The offset in the page to start copying to</param>
        /// <param name="len">The number of bytes to copy</param>
        public void SetData(byte[] data, int srcOffset, int pageOffset, int len)
        {
            lock (_writeLock)
            {
                if (len < 0) len = data.Length;
                Array.ConstrainedCopy(data, srcOffset, _data, pageOffset, len);
                IsDirty = true;
                _modified++;
            }
        }

        /// <summary>
        /// Begins an asynchronous write operation
        /// </summary>
        /// <param name="outputStream">The stream to write to</param>
        /// <param name="transactionId">The transaction to write</param>
        /// <returns>The timestamp associated with the page when the write started</returns>
        public long Write(Stream outputStream, ulong transactionId)
        {
            lock(_writeLock)
            {
                long ret = _modified;
                outputStream.Seek((long)_writeOffset, SeekOrigin.Begin);
                outputStream.Write(_data, 0, _pageSize);
                // KA: Commented out flush on every page write as this causes massive overhead on the Azure block implementation
                // If it turns out that flush is needed on the normal filesystem, may need to either make it conditional or rework the azure block impl
                //outputStream.Flush();
                return ret;
            }
        }

        public long WriteIfModifiedSince(long timestamp, Stream outputStream, ulong transactionId)
        {
            lock (_writeLock)
            {
                long ret = _modified;
                if (_modified > timestamp)
                {
                    outputStream.Seek((long) _writeOffset, SeekOrigin.Begin);
                    outputStream.Write(_data, 0, _pageSize);
                    // KA: See comment in Write() method above.
                    //outputStream.Flush();
                }
                return ret;
            }
        }
        #endregion

        #region Implementation of IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                Logging.LogDebug("Disposed page @{0}", _writeOffset);
            }
            _disposed = true;
        }
        #endregion
    }
}