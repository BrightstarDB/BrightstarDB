using System;
using System.IO;
using System.Security.Cryptography;
#if PORTABLE
using Array = BrightstarDB.Portable.Compatibility.Array;
#endif

namespace BrightstarDB.Storage.Persistence
{
    internal class FilePage : IPage, IDisposable
    {
        private long _modified;
        private readonly ulong _writeOffset;
        private readonly int _pageSize;
        private readonly byte[] _data;
        private readonly object _writeLock = new object();
#if DEBUG_PAGESTORE
        private readonly MD5 _md5 = MD5.Create();
#endif

        public FilePage(ulong pageId, int pageSize)
        {
            Id = pageId;
            _data = new byte[pageSize];
            IsDirty = true;
            Deleted = false;
            _modified = 1;
            _writeOffset = (pageId - 1) * (ulong)pageSize; // TODO : could be a bit shift instead?
            _pageSize = pageSize;
#if DEBUG_PAGESTORE
            Logging.LogDebug("New Page: PageId={0}", Id);
#endif
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
#if DEBUG_PAGESTORE
            Logging.LogDebug("New Data: PageId={0} Hash={1}", Id, DataHash());
#endif
        }

        public FilePage(Stream stream, ulong pageId, int pageSize)
        {
            _data = new byte[pageSize];
            _writeOffset = (pageId -1) * (ulong)pageSize; // TODO : could be a bit shift instead?
            lock (stream)
            {
                stream.Seek((long) _writeOffset, SeekOrigin.Begin);
                stream.Read(Data, 0, pageSize);
            }
#if DEBUG_PAGESTORE
            Logging.LogDebug("Read: PageId={0} Hash={1}", Id, DataHash());
#endif
            IsDirty = false;
            Deleted = false;
            Id = pageId;
            _modified = 0;
            _pageSize = pageSize;
        }

#if DEBUG_PAGESTORE
        private string DataHash() {
            return BitConverter.ToString(_md5.ComputeHash(_data)).Replace("-", "");
        }
#endif

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
#if DEBUG_PAGESTORE
                Logging.LogDebug("Update: PageId={0} Hash={1}", Id, DataHash());
#endif
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
                if (outputStream.Position != (long)_writeOffset)
                {
                    outputStream.Seek((long) _writeOffset, SeekOrigin.Begin);
                }
                outputStream.Write(_data, 0, _pageSize);
                outputStream.Flush();
#if DEBUG_PAGESTORE
                Logging.LogDebug("Write: PageId={0} Hash={1}", Id, DataHash());
#endif
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
#if DEBUG_PAGESTORE
                    Logging.LogDebug("Write: PageId={0} Hash={1}", Id, DataHash());
#endif
                    outputStream.Flush();
                }
#if DEBUG_PAGESTORE
                else
                {
                    Logging.LogDebug("Skip: {0}", Id);
                }
#endif
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