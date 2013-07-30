using System;
using System.Collections.Generic;
using System.IO;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.Persistence
{
    internal class AppendOnlyFilePageStore : IPageStore
    {
        private readonly string _path;
        private readonly Stream _stream;
        private readonly int _pageSize;
        private readonly int _bitShift;
        private ulong _nextPageId;
        private readonly bool _readonly;
        private bool _disposed;
        private ulong _newPageOffset;
        private readonly List<WeakReference> _newPages;
        private readonly IPersistenceManager _peristenceManager;
        private BackgroundPageWriter _backgroundPageWriter;

        public AppendOnlyFilePageStore(IPersistenceManager persistenceManager, string filePath, int pageSize, bool readOnly, bool disableBackgroundWrites)
        {
            _peristenceManager = persistenceManager;
            _path = filePath;

            if ((_pageSize % 4096) != 0)
            {
                throw new ArgumentException("Page size must be a multiple of 4096 bytes");
            }
            _pageSize = pageSize;
            _bitShift = (int)Math.Log(_pageSize, 2.0);

            if (!_peristenceManager.FileExists(filePath) && !readOnly)
            {
                // Create an empty file that we can write to later
                _peristenceManager.CreateFile(filePath);
            }
            _stream = _peristenceManager.GetInputStream(_path);
            _nextPageId = ((ulong)_stream.Length >> _bitShift) + 1;
            if (!readOnly)
            {
                _newPages = new List<WeakReference>(512);
                _newPageOffset = _nextPageId;
            }
            _pageSize = pageSize;
            _readonly = readOnly;

            if (!readOnly && !disableBackgroundWrites)
            {
                _backgroundPageWriter =
                    new BackgroundPageWriter(persistenceManager.GetOutputStream(filePath, FileMode.Open));
            }

            if (!readOnly)
            {
                PageCache.Instance.BeforeEvict += BeforePageCacheEvict;
            }
        }

        /// <summary>
        /// Handles notification of page eviction from the page cache
        /// </summary>
        /// <param name="sender">The page cache performing the eviction</param>
        /// <param name="args">The evication event arguments</param>
        /// <remarks>When the eviction event is for a writeable page, this handler 
        /// ensures that the page is queued with the background page writer. If there
        /// is no background page writer because the page store was created with the 
        /// disableBackgroundWriter option, then this method cancels an eviction
        /// for a writeable page.</remarks>
        private void BeforePageCacheEvict(object sender, EvictionEventArgs args)
        {
            if (args.Partition.Equals(_path))
            {
                // Evicting a page from this store
                if (args.PageId > _newPageOffset)
                {
                    // Evicting a writeable page - add the page to the background write queue to ensure it gets written out.
                    if (_backgroundPageWriter == null)
                    {
                        // Do not evict this page
                        args.CancelEviction = true;
                    }
                    else
                    {
                        // Queue the page with the background page writer
#if DEBUG_PAGESTORE
                        Logging.LogDebug( "Evict {0}", args.PageId );
#endif
                        var pageToEvict = _newPages[(int) (args.PageId - _newPageOffset)];
                        if (pageToEvict.IsAlive)
                        {
                            // Passing 0 for the transaction id is OK because it is not used for writing append-only pages
                            _backgroundPageWriter.QueueWrite(pageToEvict.Target as IPage, 0ul);
                        }
                        // Once the page write is queued, the cache entry can be evicted.
                        // The background page writer will hold on to the page data object until it is written
                        args.CancelEviction = false;
                    }
                }
            }
        }

        #region Implementation of IPageStore

        public IPage Retrieve(ulong pageId, BrightstarProfiler profiler)
        {
            using (profiler.Step("PageStore.Retrieve"))
            {
                if (!_readonly && pageId >= _newPageOffset)
                {
                    var newPageRef = _newPages[(int) (pageId - _newPageOffset)];
                    if (newPageRef.IsAlive)
                    {
                        var newPage = newPageRef.Target as IPage;
                        if (newPage != null) return newPage;
                    }
                }
                var page = PageCache.Instance.Lookup(_path, pageId) as FilePage;
                if (page != null)
                {
                    profiler.Incr("PageCache Hit");
                    return page;
                }
                using (profiler.Step("Load Page"))
                {
                    profiler.Incr("PageCache Miss");
                    using (profiler.Step("Create FilePage"))
                    {
                        // Lock on stream to prevent attempts to concurrently load a page
                        lock (_stream)
                        {
                            page = new FilePage(_stream, pageId, _pageSize);
                            if (_backgroundPageWriter != null)
                            {
                                _backgroundPageWriter.ResetTimestamp(pageId);
                            }
#if DEBUG_PAGESTORE
                            Logging.LogDebug("Load {0} {1}", pageId, BitConverter.ToInt32(page.Data, 0));
#endif
                        }
                    }
                    using (profiler.Step("Add FilePage To Cache"))
                    {
                        PageCache.Instance.InsertOrUpdate(_path, page);
                    }
                    return page;
                }
            }
        }

        public IPage Create(ulong commitId)
        {
            if (_readonly) throw new InvalidOperationException("Cannot create new pages in readonly page store");
            var dataPage = new FilePage(_nextPageId, _pageSize);
            _newPages.Add(new WeakReference(dataPage));
            _nextPageId++;
            PageCache.Instance.InsertOrUpdate(_path, dataPage);
            return dataPage;
        }

        private IPage Create(ulong txnId, byte[] pageData, int srcOffset = 0, int pageOffset = 0, int len = -1)
        {
            var page = Create(txnId);
            page.SetData(pageData, srcOffset, pageOffset, len);
            return page;
        }

        public void Commit(ulong commitId, BrightstarProfiler profiler)
        {
            using (profiler.Step("PageStore.Commit"))
            {
                var livePages = new List<IPage>();
                if (_backgroundPageWriter != null)
                {
                    foreach (var p in _newPages)
                    {
                        if (p.IsAlive)
                        {
                            _backgroundPageWriter.QueueWrite(p.Target as IPage, commitId);
                            livePages.Add(p.Target as IPage);
                        }
                    }
                    _backgroundPageWriter.Flush();
                    RestartBackgroundWriter();
                    PageCache.Instance.Clear(_path);
                }
                else
                {
                    using (var outputStream = _peristenceManager.GetOutputStream(_path, FileMode.Open))
                    {
                        foreach (var p in _newPages)
                        {
                            if (p.IsAlive)
                            {
                                (p.Target as IPage).Write(outputStream, commitId);
                            }
                        }
                    }
                }
                _newPages.Clear();
                _newPageOffset = _nextPageId;
            }
            
        }

        public void Write(ulong commitId, ulong pageId, byte[] data, int srcOffset = 0, int pageOffset = 0, int len = -1, BrightstarProfiler profiler = null)
        {
            if (pageId < _newPageOffset)
            {
                throw new InvalidOperationException("Attempt to write to a fixed page");
            }
            var pageIx = (int) (pageId - _newPageOffset);
            if (pageIx >= _newPages.Count)
            {
                throw new InvalidOperationException("Attempt to write to an unreserved page");
            }
            using (profiler.Step("Write Page"))
            {
                var page = Retrieve(pageId, profiler);
                page.SetData(data, srcOffset, pageOffset, len);
                if (_backgroundPageWriter != null)
                {
#if DEBUG_PAGESTORE
                    Logging.LogDebug("Mark {0}", page.Id);
#endif
                    _backgroundPageWriter.QueueWrite(page, commitId);
                }
            }
        }

        /// <summary>
        /// Returns a boolean flag indicating if the page with the specified page ID is writeable
        /// </summary>
        /// <param name="page">The page to test</param>
        /// <returns>True if the page is writeable, false otherwise</returns>
        /// <remarks>In an append-only store, only pages created since the last commit are writeable. In a binary-page store, all pages are always writeable. 
        /// Client code should use this method to determine if an update to a page can be done by a call to Write() or if a new page needs to be created using Create()</remarks>
        public bool IsWriteable(IPage page)
        {
            return page.Id >= _newPageOffset;
        }

        public IPage GetWriteablePage(ulong txnId, IPage page)
        {
            if (IsWriteable(page)) return page;
            return Create(txnId, page.Data);
        }

        /// <summary>
        /// Get the size (in bytes) of each data page
        /// </summary>
        public int PageSize
        {
            get { return _pageSize; }
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
            get { return !_readonly; }
        }

        /// <summary>
        /// Close the store, releasing any resources (such as file handles) it may be using
        /// </summary>
        public void Close()
        {
            lock (this)
            {
                if (_stream != null)
                {
                    _stream.Close();
                }
                if (_backgroundPageWriter != null)
                {
                    _backgroundPageWriter.Shutdown();
                    _backgroundPageWriter.Dispose();
                    _backgroundPageWriter = null;
                }
            }
        }

        public void MarkDirty(ulong commitId, ulong pageId)
        {
#if DEBUG_PAGESTORE
            Logging.LogDebug("Mark {0}", pageId);
#endif
            var dirtyPage = Retrieve(pageId, null);
            if (dirtyPage != null)
            {
                _backgroundPageWriter.QueueWrite(dirtyPage, commitId);
            }
        }

        #endregion

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

        #endregion

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

        ~AppendOnlyFilePageStore()
        {
            Dispose(false);
        }

        private void RestartBackgroundWriter()
        {
            lock (this)
            {
                _backgroundPageWriter.Shutdown();
                _backgroundPageWriter.Dispose();
                _backgroundPageWriter =
                    new BackgroundPageWriter(_peristenceManager.GetOutputStream(_path, FileMode.Open));
            }
        }
    }
}
