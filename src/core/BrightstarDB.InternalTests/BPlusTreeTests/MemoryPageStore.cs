using System;
using System.Collections.Generic;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    class MemoryPageStore : IPageStore
    {
        private int _pageSize;
        private List<MemoryPage> _pages;

        public MemoryPageStore(int pageSize)
        {
            _pages = new List<MemoryPage>();
            _pageSize = pageSize;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public IPage Retrieve(ulong pageId, BrightstarProfiler profiler)
        {
            return _pages[(int) pageId];
        }

        public IPage Create(ulong commitId)
        {
            var newPage = new MemoryPage((ulong)_pages.Count, _pageSize);
            _pages.Add(newPage);
            return newPage;
        }

        public void Commit(ulong commitId, BrightstarProfiler profiler)
        {
            throw new NotImplementedException();
        }

        public void Write(ulong commitId, ulong pageId, byte[] buff, int srcOffset = 0, int pageOffset = 0, int len = -1,
                          BrightstarProfiler profiler = null)
        {
            throw new NotImplementedException();
        }

        public bool IsWriteable(IPage pageId)
        {
            return true;
        }

        public IPage GetWriteablePage(ulong txnId, IPage page)
        {
            return page;
        }

        public int PageSize { get; private set; }
        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }
        public void Close()
        {
            throw new NotImplementedException();
        }

        public void MarkDirty(ulong commitId, ulong pageId)
        {
            // No-op
        }
    }
}