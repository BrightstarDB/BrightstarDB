using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.Persistence
{

    internal class BinaryFilePageStore : IPageStore
    {
        private readonly string _filePath;

        /// <summary>
        /// This records the requested data size for the page. Because a page starts with an 8 byte transaction ID, the
        /// actual page size is this value - 8, and the disk space taken up by each page is 2 * this value.
        /// </summary>
        private readonly int _nominalPageSize;

        private readonly bool _isReadOnly;
        private bool _disposed;
        private readonly IPersistenceManager _persistenceManager;
        private Stream _inputStream;
        private readonly Dictionary<ulong, BinaryFilePage> _modifiedPages;
        private readonly object _pageCacheLock = new object();
        private ulong _nextPageId;

        /// <summary>
        /// Get or set the identifier for the current transaction being read
        /// </summary>
        public ulong CurrentTransactionId { get; set; }

        public BinaryFilePageStore(IPersistenceManager persistenceManager, string filePath, int pageSize, bool readOnly, ulong currentTransactionId)
        {
            _persistenceManager = persistenceManager;
            _filePath = filePath;
            _nominalPageSize = pageSize;
            _isReadOnly = readOnly;
            _modifiedPages = new Dictionary<ulong, BinaryFilePage>();
            CurrentTransactionId = currentTransactionId;
            if(!_persistenceManager.FileExists(filePath))
            {
                if (readOnly)
                {
#if SILVERLIGHT
                    throw new FileNotFoundException(String.Format("Could not find file at {0}", filePath));
#else
                    throw new FileNotFoundException(String.Format("Could not find file at {0}", filePath), filePath);
#endif
                }
                _persistenceManager.CreateFile(filePath);
            }
            _inputStream = _persistenceManager.GetInputStream(filePath);
            _nextPageId = (ulong)_inputStream.Length/((uint)pageSize*2) + 1;
        }

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_inputStream != null)
                    {
                        _inputStream.Close();
                    }
                }
                // Clean up any unmanaged stuff here
                _disposed = true;
            }
        }

        ~BinaryFilePageStore()
        {
            Dispose(false);
        }

        #endregion

        #region Implementation of IPageStore

        /// <summary>
        /// Retrieves the data for the specified page
        /// </summary>
        /// <param name="pageId">The ID of the page</param>
        /// <param name="profiler"></param>
        /// <returns>The data buffer for the page</returns>
        public byte[] Retrieve(ulong pageId, BrightstarProfiler profiler)
        {
            var page = GetPage(pageId, profiler);
            return page == null ? null : page.GetReadBuffer(CurrentTransactionId);
        }

        /// <summary>
        /// Creates a new empty page in the page store
        /// </summary>
        /// <returns>The ID of the new page</returns>
        public ulong Create()
        {
            if (_isReadOnly)
            {
                throw new InvalidOperationException("Cannot create new pages in a read-only store.");
            }
            lock (_pageCacheLock)
            {
                var page = new BinaryFilePage(_nextPageId++, _nominalPageSize, CurrentTransactionId);
                _modifiedPages.Add(page.Id, page);
                return page.Id;
            }
        }

        /// <summary>
        /// Commits all changed and new pages to the page store
        /// </summary>
        /// <param name="commitId">The transaction identifier for the commit</param>
        /// <param name="profiler"></param>
        public void Commit(ulong commitId, BrightstarProfiler profiler)
        {
            using (profiler.Step("PageStore.Commit"))
            {
                using (var outputStream = _persistenceManager.GetOutputStream(_filePath, FileMode.Open))
                {
                    try
                    {
                        foreach (var entry in _modifiedPages.OrderBy(e => e.Key))
                        {
                            entry.Value.Write(outputStream, commitId);
                            lock (_pageCacheLock)
                            {
                                PageCache.Instance.InsertOrUpdate(_filePath, entry.Value);
                            }
                        }
                        _modifiedPages.Clear();
                    }
                    catch (Exception)
                    {
                        _modifiedPages.Clear();
                        throw;
                    }
                }
                CurrentTransactionId = commitId;
            }
        }

        /// <summary>
        /// Writes data to the specified page
        /// </summary>
        /// <param name="commitId">The transaction id for the update</param>
        /// <param name="pageId">The ID of the page to write to</param>
        /// <param name="buff">The data to be written</param>
        /// <param name="srcOffset">The offset into <paramref name="buff"/> from which to start copying bytes. Defaults to 0</param>
        /// <param name="pageOffset">The offset into the page data buffer to start writing to. Defaults to 0</param>
        /// <param name="len">The number of bytes to write. Defaults to all bytes in <paramref name="buff"/> from the specified <paramref name="srcOffset"/></param>
        /// <param name="profiler"></param>
        public void Write(ulong commitId, ulong pageId, byte[] buff, int srcOffset = 0, int pageOffset = 0, int len = -1, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("Write Page"))
            {
                var page = GetPage(pageId, profiler);
                var writeBuff = page.GetWriteBuffer(commitId);
                Array.Copy(buff, srcOffset, writeBuff, pageOffset, len < 0 ? buff.Length : len);
                _modifiedPages[pageId] = page;
            }
        }

        /// <summary>
        /// Returns a boolean flag indicating if the page with the specified page ID is writeable
        /// </summary>
        /// <param name="pageId">The ID of the page to test</param>
        /// <returns>True if the page is writeable, false otherwise</returns>
        /// <remarks>In an append-only store, only pages created since the last commit are writeable. In a binary-page store, all pages are always writeable. 
        /// Client code should use this method to determine if an update to a page can be done by a call to Write() or if a new page needs to be created using Create()</remarks>
        public bool IsWriteable(ulong pageId)
        {
            return true;
        }

        /// <summary>
        /// Get the size (in bytes) of each data page
        /// </summary>
        public int PageSize
        {
            get { return _nominalPageSize - 8; }
        }

        /// <summary>
        /// Get the flag that indicates if the store can be read from
        /// </summary>
        public bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Get the flag that indicates if the store can be written to
        /// </summary>
        public bool CanWrite
        {
            get { return !_isReadOnly; }
        }

        /// <summary>
        /// Close the store, releasing any resources (such as file handles) it may be using
        /// </summary>
        public void Close()
        {
            _inputStream.Close();
            _inputStream = null;
        }

        #endregion

        private BinaryFilePage GetPage(ulong pageId, BrightstarProfiler profiler)
        {
            using (profiler.Step("PageStore.GetPage"))
            {
                lock (_pageCacheLock)
                {
                    BinaryFilePage page;
                    if (_modifiedPages.TryGetValue(pageId, out page))
                    {
                        profiler.Incr("PageCache Hit");
                        return page;
                    }
                    page = PageCache.Instance.Lookup(_filePath, pageId) as BinaryFilePage;
                    if (page != null)
                    {
                        profiler.Incr("PageCache Hit");
                        return page;
                    }
                    using (profiler.Step("Load Page"))
                    {
                        profiler.Incr("PageCache Miss");
                        page = new BinaryFilePage(_inputStream, pageId, _nominalPageSize);
                        PageCache.Instance.InsertOrUpdate(_filePath, page);
                    }
                    return page;
                }
            }
        }
    }
}
