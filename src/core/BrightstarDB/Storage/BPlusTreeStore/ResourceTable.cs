using System;
using System.Text;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class ResourceTable : IResourceTable
    {
        // The page store that persists the resource table
        private readonly IPageStore _pageStore;
        // The size of a resource table segment in bytes
        private readonly int _segmentSize;
        // Id of the page currently being written to
        private ulong _currentPage;
        // zero-based index of the next segment in _currentPage to write to
        private byte _nextSegment;
        // zero-based index of the final segment of the page - reserved for the next page pointer
        private readonly byte _pointerSegment;
        // lock used around inserts into the table
        private readonly object _writeLock;

        public ResourceTable(IPageStore pageStore)
        {
            _pageStore = pageStore;
            _segmentSize = CalculateSegmentSize(_pageStore.PageSize);
            _pointerSegment = (byte) ((_pageStore.PageSize - 8)/_segmentSize);
            _writeLock = new object();
            _nextSegment = _pointerSegment;
            _currentPage = 0;
        }

        private static int CalculateSegmentSize(int pageSize)
        {
            if (pageSize <= 4096)
            {
                return 16;
            }
            if (pageSize <= 8192)
            {
                return 32;
            }
            if (pageSize < 16384)
            {
                return 64;
            }
            return 128;
        }

        #region Implementation of IResourceTable

        /// <summary>
        /// Retrieve the resource at the specified page and segment offset
        /// </summary>
        /// <param name="pageId">The ID of the page that holds the resource to be retrieved</param>
        /// <param name="segment">The index of the segment within the page that holds the start of the resource</param>
        /// <param name="profiler"></param>
        /// <returns>The resource</returns>
        public string GetResource(ulong pageId, byte segment, BrightstarProfiler profiler)
        {
            using (profiler.Step("ResourceTable.GetResource"))
            {
                var currentPage = _pageStore.Retrieve(pageId, profiler);
                int resourceLength = BitConverter.ToInt32(currentPage.Data, segment*_segmentSize);
                int totalLength = resourceLength + 4;
                int segmentsToLoad = totalLength/_segmentSize;
                if (totalLength%_segmentSize > 0) segmentsToLoad++;
                var buffer = new byte[segmentsToLoad*_segmentSize];
                byte segmentIndex = segment;
                for (int i = 0; i < segmentsToLoad; i++)
                {
                    if (segmentIndex == _pointerSegment)
                    {
                        ulong nextPageId = BitConverter.ToUInt64(currentPage.Data, _pageStore.PageSize - 8);
                        currentPage = _pageStore.Retrieve(nextPageId, profiler);
                        segmentIndex = 0;
                    }
                    Array.Copy(currentPage.Data, segmentIndex*_segmentSize, buffer, i*_segmentSize, _segmentSize);
                    segmentIndex++;
                }
                return Encoding.UTF8.GetString(buffer, 4, resourceLength);
            }
        }

       
        public void Insert(ulong transactionId, string resource, out ulong pageId, out byte segmentId, BrightstarProfiler profiler)
        {
            using (profiler.Step("ResourceTable.Insert"))
            {
                var byteCount = Encoding.UTF8.GetByteCount(resource);
                var resourceBytes = new byte[byteCount + 4];
                BitConverter.GetBytes(byteCount).CopyTo(resourceBytes, 0);
                Encoding.UTF8.GetBytes(resource, 0, resource.Length, resourceBytes, 4);
                lock (_writeLock)
                {
                    if (_nextSegment == _pointerSegment)
                    {
                        StartNewPage(transactionId, profiler);
                    }
                    pageId = _currentPage;
                    segmentId = _nextSegment;
                    for (int i = 0; i < (byteCount + 4); i += _segmentSize)
                    {
                        _pageStore.Write(transactionId, _currentPage, resourceBytes, i, _nextSegment*_segmentSize,
                                         _segmentSize < (byteCount + 4 - i) ? _segmentSize : (byteCount + 4 - i),
                                         profiler);
                        _nextSegment++;
                        if (_nextSegment == _pointerSegment)
                        {
                            StartNewPage(transactionId, profiler);
                        }
                    }
                }
            }
        }

        public void Commit(ulong transactionId, BrightstarProfiler profiler)
        {
            using (profiler.Step("Commit ResourceTable"))
            {
                if (_currentPage > 0)
                {
                    _pageStore.Commit(transactionId, profiler);
                    _nextSegment = _pointerSegment;
                    _currentPage = 0;
                }
            }
        }

        #endregion

        private void StartNewPage(ulong transactionId, BrightstarProfiler profiler)
        {
            IPage nextPage = _pageStore.Create(transactionId);
            if (_currentPage > 0)
            {
                _pageStore.Write(transactionId, _currentPage, BitConverter.GetBytes(nextPage.Id), 0, _pageStore.PageSize-8, 8, profiler);
            }
            _currentPage = nextPage.Id;
            _nextSegment = 0;
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

        private bool _disposed;

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _pageStore.Dispose();
                    _disposed = true;
                }
            }
        }
        #endregion

        ~ResourceTable()
        {
            Dispose(false);
        }

    }
}
