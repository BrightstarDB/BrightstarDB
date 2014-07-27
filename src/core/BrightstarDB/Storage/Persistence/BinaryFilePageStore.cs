using System;
using System.Collections.Concurrent;
using System.IO;
using BrightstarDB.Profiling;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
using Array = BrightstarDB.Portable.Compatibility.Array;
#endif

namespace BrightstarDB.Storage.Persistence
{
    internal class BinaryFilePageStore : IPageStore
    {
        private BackgroundPageWriter _backgroundPageWriter;

        /// <summary>
        /// The on-disk size of the store pages. The actual
        /// data page size (as reported by the PageSize property)
        /// is this value - 8 bytes for the header.
        /// </summary>
        private readonly int _nominalPageSize;

        /// <summary>
        /// The manager for access to the underlying file
        /// </summary>
        private readonly IPersistenceManager _persistenceManager;

        /// <summary>
        /// The path to the file that this store manages access to. This
        /// is used as a partition ID in the page cache.
        /// </summary>
        private readonly string _filePath;

        private readonly string _partitionId;

        /// <summary>
        /// The ID of the current read transaction for this store. The
        /// write transaction will be this value + 1
        /// </summary>
        private readonly ulong _currentReadTxnId;

        /// <summary>
        /// The stream to use when reading from disk
        /// </summary>
        private Stream _inputStream;

        /// <summary>
        /// The sequential ulong identifier for the next page to be created for this store
        /// </summary>
        private ulong _nextPageId;

        /// <summary>
        /// Tracks the IDs of pages we have modified on disk during the present transaction
        /// </summary>
        private readonly ConcurrentDictionary<ulong, bool> _modifiedPages;

        /// <summary>
        /// Boolean flag that is set to true when this object is disposed
        /// </summary>
        private bool _disposed;

        
        private readonly object _restartLock = new object();

        public BinaryFilePageStore(IPersistenceManager persistenceManager, string filePath, int pageSize, bool readOnly,
            ulong currentTransactionId)
        {
            _persistenceManager = persistenceManager;
            _nominalPageSize = pageSize;
            _filePath = filePath;
            _currentReadTxnId = currentTransactionId;
            PageSize = _nominalPageSize - 8;
            CanWrite = !readOnly;
            OpenInputStream();
            _nextPageId = (ulong) _inputStream.Length/((uint) _nominalPageSize*2) + 1;
            if (CanWrite)
            {
                _backgroundPageWriter =
                    new BackgroundPageWriter(_persistenceManager.GetOutputStream(_filePath, FileMode.Open));
                PageCache.Instance.BeforeEvict += BeforePageCacheEvict;
            }
            _modifiedPages = new ConcurrentDictionary<ulong, bool>();
            _partitionId = filePath + "." + (readOnly ? currentTransactionId : currentTransactionId + 1);
        }

        /// <summary>
        /// Get the number of bytes available for data storage in a single page
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// Get a boolean flag indicating if the store is readable
        /// </summary>
        public bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// Get a boolean flag indicating if the store is writeable
        /// </summary>
        public bool CanWrite { get; private set; }

        #region Implementation of IPageStore

        public IPage Retrieve(ulong pageId, BrightstarProfiler profiler)
        {
            using (profiler.Step("BinaryFilePageStore.GetPage"))
            {
                // Look in the page cache
                IPage page = PageCache.Instance.Lookup(_partitionId, pageId) as BinaryFilePage;
                if (page != null)
                {
                    profiler.Incr("PageCache Hit");
                    return page;
                }
               
                // See if the page is queued for writing
                if (_backgroundPageWriter != null && _backgroundPageWriter.TryGetPage(pageId, out page))
                {
                    profiler.Incr("BackgroundWriter Queue Hit");
                    return page;
                }

                // Not found in memory, so go to the disk
                profiler.Incr("PageCache Miss");
                using (profiler.Step("Load Page"))
                {
                    page = _modifiedPages.ContainsKey(pageId)
                        ? new BinaryFilePage(_inputStream, pageId, _nominalPageSize, _currentReadTxnId + 1, true)
                        : new BinaryFilePage(_inputStream, pageId, _nominalPageSize, _currentReadTxnId, false);
                    PageCache.Instance.InsertOrUpdate(_partitionId, page);
                    return page;
                }
            }
        }

        public IPage Create(ulong commitId)
        {
            if (!CanWrite)
            {
                throw new InvalidOperationException("Cannot create new pages in a read-only store.");
            }
            var page = new BinaryFilePage(_nextPageId++, _nominalPageSize, _currentReadTxnId + 1);
            _modifiedPages.AddOrUpdate(page.Id, true, (k, v) => true);
            PageCache.Instance.InsertOrUpdate(_partitionId, page);
            return page;
        }

        public void Commit(ulong commitId, BrightstarProfiler profiler)
        {
            if (CanWrite)
            {
                foreach (var pageId in _modifiedPages.Keys)
                {
                    var page = PageCache.Instance.Lookup(_partitionId, pageId) as BinaryFilePage;
                    if (page != null && page.IsDirty)
                    {
                        _backgroundPageWriter.QueueWrite(page, _currentReadTxnId + 1);
                    }
                }
                _backgroundPageWriter.Flush();
                lock (_restartLock)
                {
                    _backgroundPageWriter.Shutdown();
                    _backgroundPageWriter.Dispose();
                    PageCache.Instance.Clear(_partitionId);
                    _backgroundPageWriter =
                        new BackgroundPageWriter(_persistenceManager.GetOutputStream(_filePath, FileMode.Open));
                }
            }
            else
            {
                throw new InvalidOperationException("Attempt to Commit on a read-only store instance");
            }
        }

        // TODO: Modify the interface so that we don't have to implement this stub
        public void Write(ulong commitId, ulong pageId, byte[] buff, int srcOffset = 0, int pageOffset = 0, int len = -1,
            BrightstarProfiler profiler = null)
        {
            // This method is only used by the ResourceTable, which is always an append-only store.
            throw new NotImplementedException();
        }

        public bool IsWriteable(IPage page)
        {
            if (page is BinaryFilePage)
            {
                return (page as BinaryFilePage).IsWriteable;
            }
            return false;
        }

        public IPage GetWriteablePage(ulong commitId, IPage page)
        {
            if (!CanWrite) throw new InvalidOperationException("Attempt to retrieve a writeable page from a read-only store");
            var p = page as BinaryFilePage;
            if (p == null)
            {
                throw new ArgumentException("Expected a BinaryFilePage instance. Received a " + page.GetType().FullName);
            }
            if (p.IsWriteable)
            {
                return p;
            }
            p.MakeWriteable(_currentReadTxnId + 1);
            return p;
        }

        public void Close()
        {
            if (_inputStream != null)
            {
                _inputStream.Close();
                _inputStream.Dispose();
                _inputStream = null;
            }

            if (_backgroundPageWriter != null)
            {
                _backgroundPageWriter.Shutdown();
                _backgroundPageWriter.Dispose();
                _backgroundPageWriter = null;
            }

        }

        internal void OnPageModified(BinaryFilePage page)
        {
            _modifiedPages.AddOrUpdate(page.Id, true, (k, v) => true);
            _backgroundPageWriter.QueueWrite(page, _currentReadTxnId+1);
        }

        public void MarkDirty(ulong commitId, ulong pageId)
        {
            var page = Retrieve(pageId, null) as BinaryFilePage;
            page.IsDirty = true;
            if (page != null)
            {
                OnPageModified(page);
            }
        }

        public int Preload(int numPages, BrightstarProfiler profiler)
        {
            // TODO: This is a bit unsatisfactory, it would be better to scan through the tree loading the internal nodes in a breadth-first manner
            var maxPage = Math.Min((ulong)numPages / 2, _nextPageId - 1);
            int loadCount = 0;
            for (ulong pageId = 0; pageId < maxPage; pageId++)
            {
                Retrieve(pageId, profiler);
                loadCount++;
            }
            return loadCount;
        }

        #endregion

        #region Implementation of IDisposable

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
                    Close();
                    if (_backgroundPageWriter != null)
                    {
                        _backgroundPageWriter.Dispose();
                        _backgroundPageWriter = null;
                    }
                }
                _disposed = true;
            }
        }

        ~BinaryFilePageStore()
        {
            Dispose(false);
        }

        #endregion

        #region PageCache event handler

        private void BeforePageCacheEvict(object sender, EvictionEventArgs args)
        {
            args.CancelEviction = _modifiedPages.ContainsKey(args.PageId);
            return;
            /*
            lock (_restartLock) // Ensure we don't try to process page evictions while restarting the background writer
            {
                if (_modifiedPages.ContainsKey(args.PageId))
                {
                    var page = PageCache.Instance.Lookup(_filePath, args.PageId) as BinaryFilePage;
                    if (page != null)
                    {
                        _backgroundPageWriter.QueueWrite(page, _currentReadTxnId + 1);
                    }
                }
                // Unmodified pages can just be evicted
            }
             */
        }

        #endregion

        #region Private methods

        private void OpenInputStream()
        {
            if (!_persistenceManager.FileExists(_filePath))
            {
                if (CanWrite)
                {
                    // Attempt to create the file
                    _persistenceManager.CreateFile(_filePath);
                }
                else
                {
                    // File not found
#if PORTABLE
                    throw new FileNotFoundException(String.Format("Could not find file at {0}", filePath));
#else
                    throw new FileNotFoundException("Could not find requested file", _filePath);
#endif
                }
            }
            _inputStream = _persistenceManager.GetInputStream(_filePath);
        }

        #endregion
    }
}


//    internal class __BinaryFilePageStore : IPageStore
//    {
//        private readonly string _filePath;

//        /// <summary>
//        /// This records the requested data size for the page. Because a page starts with an 8 byte transaction ID, the
//        /// actual page size is this value - 8, and the disk space taken up by each page is 2 * this value.
//        /// </summary>
//        private readonly int _nominalPageSize;

//        private readonly bool _isReadOnly;
//        private bool _disposed;
//        private readonly IPersistenceManager _persistenceManager;
//        private Stream _inputStream;
//        private Stream _outputStream;
//        private readonly Dictionary<ulong, Tuple<BinaryFilePage, ulong>> _modifiedPages;
//        private readonly object _pageCacheLock = new object();
//        private readonly object _streamLock = new object();
//        private ulong _nextPageId;

//        /// <summary>
//        /// Get or set the identifier for the current transaction being read
//        /// </summary>
//        private ulong CurrentTransactionId { get; set; }

//        public BinaryFilePageStore(IPersistenceManager persistenceManager, string filePath, int pageSize, bool readOnly, ulong currentTransactionId)
//        {
//            _persistenceManager = persistenceManager;
//            _filePath = filePath;
//            _nominalPageSize = pageSize;
//            _isReadOnly = readOnly;
//            _modifiedPages = new Dictionary<ulong, Tuple<BinaryFilePage, ulong>>();
//            CurrentTransactionId = currentTransactionId;
//            if(!_persistenceManager.FileExists(filePath))
//            {
//                if (readOnly)
//                {
//#if SILVERLIGHT || PORTABLE
//                    throw new FileNotFoundException(String.Format("Could not find file at {0}", filePath));
//#else
//                    throw new FileNotFoundException(String.Format("Could not find file at {0}", filePath), filePath);
//#endif
//                }
//                _persistenceManager.CreateFile(filePath);
//            }
//            _inputStream = _persistenceManager.GetInputStream(filePath);
//            _nextPageId = (ulong)_inputStream.Length/((uint)pageSize*2) + 1;
//            PageCache.Instance.BeforeEvict += BeforePageCacheEvict;
//        }

//        private void BeforePageCacheEvict(object sender, EvictionEventArgs args)
//        {
//            if (args.Partition.Equals(_filePath))
//            {
//                Tuple<BinaryFilePage, ulong> bfpTuple;
//                if (_modifiedPages.TryGetValue(args.PageId, out bfpTuple))
//                {
//                    EnsureOutputStream();
//                    bfpTuple.Item1.Write(_outputStream, bfpTuple.Item2);
//                }
//                _modifiedPages.Remove(args.PageId);
//            }
//        }

//        #region Implementation of IDisposable

//        /// <summary>
//        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
//        /// </summary>
//        /// <filterpriority>2</filterpriority>
//        public void Dispose()
//        {
//            Dispose(true);
//            GC.SuppressFinalize(this);
//        }

//        private void Dispose(bool disposing)
//        {
//            if (!_disposed)
//            {
//                if (disposing)
//                {
//                    lock (_streamLock)
//                    {
//                        if (_inputStream != null)
//                        {
//                            _inputStream.Close();
//                        }
//                        if (_outputStream != null)
//                        {
//                            if (_outputStream.CanWrite)
//                            {
//                                try
//                                {
//                                    _outputStream.Close();
//                                }
//                                catch (Exception)
//                                {
//                                    // Ignore unhandled exceptions
//                                }
//                            }
//                            _outputStream.Dispose();
//                            _outputStream = null;
//                        }
//                    }
//                }
//                // Clean up any unmanaged stuff here
//                _disposed = true;
//            }
//        }

//        ~BinaryFilePageStore()
//        {
//            Dispose(false);
//        }

//        #endregion

//        #region Implementation of IPageStore

//        /// <summary>
//        /// Retrieves the data for the specified page
//        /// </summary>
//        /// <param name="pageId">The ID of the page</param>
//        /// <param name="profiler"></param>
//        /// <returns>The data buffer for the page</returns>
//        public IPage Retrieve(ulong pageId, BrightstarProfiler profiler)
//        {
//            Tuple<BinaryFilePage, ulong> modifiedPage;
//            if (_modifiedPages.TryGetValue(pageId, out modifiedPage))
//            {
//                return new BinaryPageAdapter(this, modifiedPage.Item1, modifiedPage.Item2, true);
//            }
//            var page = GetPage(pageId, profiler);
//            return page == null ? null : new BinaryPageAdapter(this, page, CurrentTransactionId, false);
//        }

//        /// <summary>
//        /// Creates a new empty page in the page store
//        /// </summary>
//        /// <param name="commitId"></param>
//        /// <returns>The new page</returns>
//        public IPage Create(ulong commitId)
//        {
//            if (_isReadOnly)
//            {
//                throw new InvalidOperationException("Cannot create new pages in a read-only store.");
//            }
//            lock (_pageCacheLock)
//            {
//                var page = new BinaryFilePage(_nextPageId++, _nominalPageSize, CurrentTransactionId);
//                    _modifiedPages.Add(page.Id, new Tuple<BinaryFilePage, ulong>(page, commitId));
//                    return new BinaryPageAdapter(this, page, commitId, true);
//            }
//        }

//        /// <summary>
//        /// Commits all changed and new pages to the page store
//        /// </summary>
//        /// <param name="commitId">The transaction identifier for the commit</param>
//        /// <param name="profiler"></param>
//        public void Commit(ulong commitId, BrightstarProfiler profiler)
//        {
//            EnsureOutputStream();
//            try
//            {
//                using (profiler.Step("PageStore.Commit"))
//                {
//                    try
//                    {
//                        foreach (var entry in _modifiedPages.OrderBy(e => e.Key))
//                        {
//                            // TODO: Ensure we are writing the correct commit
//                            entry.Value.Item1.Write(_outputStream, commitId);
//                            lock (_pageCacheLock)
//                            {
//                                PageCache.Instance.InsertOrUpdate(_filePath, entry.Value.Item1);
//                            }
//                        }
//                        _modifiedPages.Clear();
//                    }
//                    catch (Exception)
//                    {
//                        _modifiedPages.Clear();
//                        throw;
//                    }
//                    CurrentTransactionId = commitId;
//                }
//            }
//            finally
//            {
//                lock (_streamLock)
//                {
//                    _outputStream.Flush();
//                    _outputStream.Close();
//                    _outputStream = null;
//                }
//            }
//        }

//        /// <summary>
//        /// Writes data to the specified page
//        /// </summary>
//        /// <param name="commitId">The transaction id for the update</param>
//        /// <param name="pageId">The ID of the page to write to</param>
//        /// <param name="buff">The data to be written</param>
//        /// <param name="srcOffset">The offset into <paramref name="buff"/> from which to start copying bytes. Defaults to 0</param>
//        /// <param name="pageOffset">The offset into the page data buffer to start writing to. Defaults to 0</param>
//        /// <param name="len">The number of bytes to write. Defaults to all bytes in <paramref name="buff"/> from the specified <paramref name="srcOffset"/></param>
//        /// <param name="profiler"></param>
//        public void Write(ulong commitId, ulong pageId, byte[] buff, int srcOffset = 0, int pageOffset = 0, int len = -1, BrightstarProfiler profiler = null)
//        {
//            using (profiler.Step("Write Page"))
//            {
//                var page = GetPage(pageId, profiler);
//                var writeBuff = page.GetWriteBuffer(commitId);
//                Array.Copy(buff, srcOffset, writeBuff, pageOffset, len < 0 ? buff.Length : len);
//                _modifiedPages[pageId] = new Tuple<BinaryFilePage, ulong>(page, commitId);
//            }
//        }

//        internal void EnsureWriteable(ulong pageId)
//        {
//            if (!_modifiedPages.ContainsKey(pageId))
//            {
//                throw new InvalidOperationException("Attempt to write to a read-only page.");
//            }
//        }

//        /// <summary>
//        /// Returns a boolean flag indicating if the page with the specified page ID is writeable
//        /// </summary>
//        /// <param name="page">The page to test</param>
//        /// <returns>True if the page is writeable, false otherwise</returns>
//        /// <remarks>In an append-only store, only pages created since the last commit are writeable. In a binary-page store, all pages can be made writeable, by a call to
//        /// GetWriteablePage()
//        /// Client code should use this method to determine if an update to a page can be done by a call to SetData() on the page or if
//        /// a new page should be retrieved by calling GetWriteablePage()</remarks>
//        public bool IsWriteable(IPage page)
//        {
//            var binaryPage = page as BinaryPageAdapter;
//            return binaryPage != null && binaryPage.IsDirty;
//        }

//        /// <summary>
//        /// Returns a writeable copy of the specified page
//        /// </summary>
//        /// <param name="commitId">The transaction id for the write operation</param>
//        /// <param name="page">The page to return a writeable version of</param>
//        /// <returns></returns>
//        public IPage GetWriteablePage(ulong commitId, IPage page)
//        {
//            Tuple<BinaryFilePage, ulong> bfpTuple;
//            if (!_modifiedPages.TryGetValue(page.Id, out bfpTuple))
//            {
//#if DEBUG_BTREE
//                Logging.LogDebug("Initialized Write Buffer for page@{0} in commit {1}", page.Id, commitId);
//#endif
//                var bfp = GetPage(page.Id, null);
//                Array.ConstrainedCopy(bfp.GetReadBuffer(CurrentTransactionId), 0,
//                                      bfp.GetWriteBuffer(commitId), 0,
//                                      PageSize);
//                bfpTuple = new Tuple<BinaryFilePage, ulong>(bfp, commitId);
//                _modifiedPages[bfp.Id] = bfpTuple;
//            }
//            return new BinaryPageAdapter(this, bfpTuple.Item1, commitId, true);
//        }

//        /// <summary>
//        /// Get the size (in bytes) of each data page
//        /// </summary>
//        public int PageSize
//        {
//            get { return _nominalPageSize - 8; }
//        }

//        /// <summary>
//        /// Get the flag that indicates if the store can be read from
//        /// </summary>
//        public bool CanRead
//        {
//            get { return true; }
//        }

//        /// <summary>
//        /// Get the flag that indicates if the store can be written to
//        /// </summary>
//        public bool CanWrite
//        {
//            get { return !_isReadOnly; }
//        }

//        /// <summary>
//        /// Close the store, releasing any resources (such as file handles) it may be using
//        /// </summary>
//        public void Close()
//        {
//            lock (_streamLock)
//            {
//                if (_inputStream != null)
//                {
//                    _inputStream.Close();
//                    _inputStream = null;
//                }
//                if (_outputStream != null)
//                {
//                    _outputStream.Flush();
//                    _outputStream.Close();
//                    _outputStream = null;
//                }
//            }
//        }

//        public void MarkDirty(ulong commitId, ulong pageId)
//        {
//            Tuple<BinaryFilePage, ulong> bfp;
//            if (_modifiedPages.TryGetValue(pageId, out bfp))
//            {
//                if (bfp.Item1.Id == pageId && bfp.Item2 == commitId)
//                {
//                    return;
//                }
//            }
//            bfp = new Tuple<BinaryFilePage, ulong>(GetPage(pageId, null), commitId);
//            _modifiedPages[pageId] = bfp;
//        }

//        public int Preload(int numPages, BrightstarProfiler profiler)
//        {
//            var maxPage = Math.Min((ulong)numPages/2, _nextPageId - 1);
//            int loadCount = 0;
//            for (ulong pageId = 0; pageId < maxPage; pageId++)
//            {
//                Retrieve(pageId, profiler);
//                loadCount++;
//            }
//            return loadCount;
//        }

//        #endregion

//        private BinaryFilePage GetPage(ulong pageId, BrightstarProfiler profiler)
//        {
//            using (profiler.Step("PageStore.GetPage"))
//            {
//                lock (_pageCacheLock)
//                {
//                    BinaryFilePage page;
//                    Tuple<BinaryFilePage, ulong> modifiedPage;
//                    if (_modifiedPages.TryGetValue(pageId, out modifiedPage))
//                    {
//                        profiler.Incr("PageCache Hit");
//                        return modifiedPage.Item1;
//                    }
//                    page = PageCache.Instance.Lookup(_filePath, pageId) as BinaryFilePage;
//                    if (page != null)
//                    {
//                        profiler.Incr("PageCache Hit");
//                        return page;
//                    }
//                    using (profiler.Step("Load Page"))
//                    {
//                        profiler.Incr("PageCache Miss");
//                        page = new BinaryFilePage(_inputStream, pageId, _nominalPageSize);
//                        PageCache.Instance.InsertOrUpdate(_filePath, page);
//                    }
//                    return page;
//                }
//            }
//        }

//        private void EnsureOutputStream()
//        {
//            lock (_streamLock)
//            {
//                if (_outputStream == null)
//                {
//                    _outputStream = _persistenceManager.GetOutputStream(_filePath, FileMode.Open);
//                }
//            }
//        }
//    }
//}
