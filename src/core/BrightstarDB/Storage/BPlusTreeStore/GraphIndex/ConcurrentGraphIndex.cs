using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if !WINDOWS_PHONE
using System.Threading;
#endif
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore.GraphIndex
{
    internal class ConcurrentGraphIndex : IPageStoreGraphIndex
    {
        private readonly IPageStore _pageStore;
        private readonly Dictionary<string, int> _graphUriIndex;
        private readonly List<GraphIndexEntry> _allEntries;
#if WINDOWS_PHONE
        private readonly object _lock = new object();
#else
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
#endif

        public ConcurrentGraphIndex(IPageStore pageStore)
        {
            _pageStore = pageStore;
            _graphUriIndex = new Dictionary<string, int>();
            _allEntries= new List<GraphIndexEntry>();
        }

        public ConcurrentGraphIndex(IPageStore pageStore, ulong rootPage, BrightstarProfiler profiler)
        {
            using (profiler.Step("Load ConcurrentGraphIndex"))
            {
                _pageStore = pageStore;
                _graphUriIndex = new Dictionary<string, int>();
                _allEntries = new List<GraphIndexEntry>();
                Read(rootPage, profiler);
            }
        }

        #region Implementation of IGraphIndex

        /// <summary>
        /// Returns an enumeration over all graphs in the index
        /// </summary>
        /// <returns></returns>
        public IEnumerable<GraphIndexEntry> EnumerateEntries()
        {
#if WINDOWS_PHONE
            lock(_lock)
            {
                return _allEntries.Where(x => !x.IsDeleted).ToList();
            }
#else
            _lock.EnterReadLock();
            try
            {
                return _allEntries.Where(x => !x.IsDeleted);
            }
            finally
            {
                _lock.ExitReadLock();
            }
#endif
        }

        /// <summary>
        /// Return the URI for the graph with the specified ID
        /// </summary>
        /// <param name="graphId">The ID of the graph to lookup</param>
        /// <returns></returns>
        /// <remarks>Returns null if no graph exists with the specified URI or if the graph is marked as deleted</remarks>
        public string GetGraphUri(int graphId)
        {
#if WINDOWS_PHONE
            lock(_lock)
            {
                if (graphId < _allEntries.Count && !_allEntries[graphId].IsDeleted)
                {
                    return _allEntries[graphId].Uri;
                }
                return null;                
            }
#else
            _lock.EnterReadLock();
            try
            {
                if (graphId < _allEntries.Count && !_allEntries[graphId].IsDeleted)
                {
                    return _allEntries[graphId].Uri;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
#endif
        }

        /// <summary>
        /// Finds or creates a new ID for the graph with the specified graph URI
        /// </summary>
        /// <param name="graphUri">The graph URI to lookup</param>
        /// <param name="profiler"></param>
        /// <returns>The ID assigned to the graph</returns>
        public int AssertGraphId(string graphUri, BrightstarProfiler profiler = null)
        {
            if (String.IsNullOrEmpty(graphUri))
            {
                throw new ArgumentException("Graph URI must not be null or an empty string", "graphUri");
            }
            if (graphUri.Length > short.MaxValue)
            {
                throw new ArgumentException(
                    String.Format("Graph URI string exceeds maximum allowed length of {0} bytes", short.MaxValue), "graphUri");
            }
#if WINDOWS_PHONE
            lock(_lock)
            {
                int entryId;
                if (_graphUriIndex.TryGetValue(graphUri, out entryId) && !_allEntries[entryId].IsDeleted)
                {
                    return entryId;
                }
                var newId = _allEntries.Count;
                var entry = new GraphIndexEntry(newId, graphUri, false);
                _allEntries.Add(entry);
                _graphUriIndex.Add(graphUri, newId);
                return newId;                
            }
#else
            using (profiler.Step("Assert Graph Id"))
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    int entryId;
                    if (_graphUriIndex.TryGetValue(graphUri, out entryId) && !_allEntries[entryId].IsDeleted)
                    {
                        return entryId;
                    }
                    _lock.EnterWriteLock();
                    try
                    {
                        var newId = _allEntries.Count;
                        var entry = new GraphIndexEntry(newId, graphUri, false);
                        _allEntries.Add(entry);
                        _graphUriIndex.Add(graphUri, newId);
                        return newId;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            }
#endif
        }

        /// <summary>
        /// Finds the ID assigned to the graph with the specified graph URI
        /// </summary>
        /// <param name="graphUri">The graph URI to lookup</param>
        /// <param name="graphId">Receives the ID of the graph</param>
        /// <returns>True if an ID was found, false otherwise</returns>
        public bool TryFindGraphId(string graphUri, out int graphId)
        {
#if WINDOWS_PHONE
            lock (_lock)
            {
                int entryId;
                if (_graphUriIndex.TryGetValue(graphUri, out entryId) && !_allEntries[entryId].IsDeleted)
                {
                    graphId = entryId;
                    return true;
                }
                graphId = -1;
                return false;                
            }
#else
            _lock.EnterReadLock();
            try
            {
                int entryId;
                if (_graphUriIndex.TryGetValue(graphUri, out entryId) && !_allEntries[entryId].IsDeleted)
                {
                    graphId = entryId;
                    return true;
                }
                graphId = -1;
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
#endif
        }

        public void DeleteGraph(int graphId)
        {
#if WINDOWS_PHONE
            lock(_lock)
            {
                if (graphId < _allEntries.Count)
                {
                    var toDelete = _allEntries[graphId];
                    if (!toDelete.IsDeleted)
                    {
                        _allEntries[graphId] = new GraphIndexEntry(toDelete.Id, toDelete.Uri, true);
                        _graphUriIndex.Remove(toDelete.Uri);
                        IsDirty = true;
                    }
                }                
            }
#else
            _lock.EnterWriteLock();
            try
            {
                if (graphId < _allEntries.Count)
                {
                    var toDelete = _allEntries[graphId];
                    if (!toDelete.IsDeleted)
                    {
                        _allEntries[graphId] = new GraphIndexEntry(toDelete.Id, toDelete.Uri, true);
                        _graphUriIndex.Remove(toDelete.Uri);
                        IsDirty = true;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
#endif
        }


        /// <summary>
        /// Get boolean flag indicating if the index contains changes that need to be saved
        /// </summary>
        public bool IsDirty { get; private set; }

        #endregion

       public ulong Save(ulong transactionId, BrightstarProfiler profiler)
       {
           return Write(_pageStore, transactionId, profiler);
       }

       public ulong Write(IPageStore pageStore, ulong transactionId, BrightstarProfiler profiler)
       {
           ulong rootPage = pageStore.Create();
           ulong currentPage = rootPage;
           var buff = new byte[pageStore.PageSize];
           int offset = 0;
           foreach (var graphIndexEntry in _allEntries)
           {
               int entrySize = String.IsNullOrEmpty(graphIndexEntry.Uri)
                                   ? 1
                                   : 3 + Encoding.UTF8.GetByteCount(graphIndexEntry.Uri);
               if (offset + entrySize > pageStore.PageSize - 9)
               {
                   ulong nextPage = pageStore.Create();
                   buff[offset] = 0xff;
                   BitConverter.GetBytes(nextPage).CopyTo(buff, pageStore.PageSize - 8);
                   pageStore.Write(transactionId, currentPage, buff, profiler: profiler);
                   currentPage = nextPage;
                   offset = 0;
               }
               else
               {
                   if (String.IsNullOrEmpty(graphIndexEntry.Uri))
                   {
                       // Record an empty entry
                       buff[offset++] = 2;
                   }
                   else
                   {
                       if (graphIndexEntry.IsDeleted)
                       {
                           buff[offset++] = 1;
                       }
                       else
                       {
                           buff[offset++] = 0;
                       }
                       var uriBytes = Encoding.UTF8.GetBytes(graphIndexEntry.Uri);
                       BitConverter.GetBytes(uriBytes.Length).CopyTo(buff, offset);
                       offset += 4;
                       uriBytes.CopyTo(buff, offset);
                       offset += uriBytes.Length;
                   }
               }
           }
           buff[offset] = 0xff;
           BitConverter.GetBytes(0ul).CopyTo(buff, pageStore.PageSize - 8);
           pageStore.Write(transactionId, currentPage, buff, profiler: profiler);
           return rootPage;
       }

        void Read(ulong rootPageId, BrightstarProfiler profiler)
        {
            byte[] currentPage = _pageStore.Retrieve(rootPageId, profiler);
            int offset = 0;
            int entryIndex = 0;
            while(true)
            {
                var marker = currentPage[offset++];
                if (marker == 0xff)
                {
                    ulong nextPageId = BitConverter.ToUInt64(currentPage, _pageStore.PageSize - 8);
                    if (nextPageId == 0) return;
                    currentPage = _pageStore.Retrieve(nextPageId, profiler);
                    offset = 0;
                }
                else if (marker == 2)
                {
                    _allEntries.Add(new GraphIndexEntry(entryIndex++, null, true));
                }
                else
                {
                    int uriByteLength = BitConverter.ToInt32(currentPage, offset);
                    offset += 4;
                    var uri = Encoding.UTF8.GetString(currentPage, offset, uriByteLength);
                    offset += uriByteLength;
                    var newEntry = new GraphIndexEntry(entryIndex++, uri, marker == 1);
                    _allEntries.Add(newEntry);
                    if (!newEntry.IsDeleted) _graphUriIndex[newEntry.Uri] = newEntry.Id;
                }
            }
        }
    }
}
