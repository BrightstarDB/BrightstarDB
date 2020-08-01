using System;
using System.Collections.Generic;
using System.Text;
#if !WINDOWS_PHONE
using System.Threading;
#endif
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    /// <summary>
    /// Implementation of IPrefixManager that stores its data in a page store
    /// </summary>
    internal class PrefixManager : IPageStorePrefixManager
    {
        // maps prefix to short value
        private readonly Dictionary<string, string> _prefixMappings = new Dictionary<string, string>();

        // maps short value to prefix
        private readonly Dictionary<string, string> _shortValueMappings = new Dictionary<string, string>();

#if WINDOWS_PHONE || PORTABLE
        private readonly object _lock =new object();
#else
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
#endif
        readonly IPageStore _pageStore;

        /// <summary>
        /// Creates a new prefix manager that will write its content to the specified page store
        /// </summary>
        /// <param name="pageStore"></param>
        public PrefixManager(IPageStore pageStore)
        {
            _pageStore = pageStore;
        }

        public PrefixManager(IPageStore pageStore, ulong startPageId, BrightstarProfiler profiler)
        {
            _pageStore = pageStore;
            var startPage = _pageStore.Retrieve(startPageId, profiler);
            Load(startPage, profiler);
        }

        /// <summary>
        /// Get the flag that indicates if the prefix collection has been modified
        /// </summary>
        public bool IsDirty { get; private set; }
        
        #region Implementation of IPrefixManager

        public string MakePrefixedUri(string uri)
        {
#if WINDOWS_PHONE || PORTABLE
            lock (_lock)
            {
                var pos = uri.LastIndexOf("/");
                if (pos < 0) pos = uri.LastIndexOf('#');

                // no match then no prefix
                if (pos < 0 || pos == uri.Length - 1) return uri;
                var start = uri.Substring(0, pos + 1);
                var rest = uri.Substring(pos + 1);

                string match;
                if (_prefixMappings.TryGetValue(start, out match))
                {
                    return match + ":" + rest;
                }
                var prefix = "bs" + _prefixMappings.Count;
                _prefixMappings.Add(start, prefix);
                _shortValueMappings.Add(prefix, start);
                IsDirty = true;
                return prefix + ":" + rest;
            }
#else
            _lock.EnterUpgradeableReadLock();
            try
            {
                var pos = uri.LastIndexOf("/");
                if (pos < 0) pos = uri.LastIndexOf('#');

                // no match then no prefix
                if (pos < 0 || pos == uri.Length - 1) return uri;
                var start = uri.Substring(0, pos + 1);
                var rest = uri.Substring(pos + 1);

                string match;
                if (_prefixMappings.TryGetValue(start, out match))
                {
                    return match + ":" + rest;
                }
                else
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        var prefix = "bs" + _prefixMappings.Count;
                        _prefixMappings.Add(start, prefix);
                        _shortValueMappings.Add(prefix, start);
                        IsDirty = true;
                        return prefix + ":" + rest;
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _lock.ExitUpgradeableReadLock();
            }
#endif
        }

        public string ResolvePrefixedUri(string uri)
        {
            if (!uri.StartsWith("bs")) return uri;
            var pos = uri.IndexOf(':');
            if (pos < 0) throw new BrightstarInternalException("Invalid shortened uri " + uri);

            var shortValue = uri.Substring(0, pos);
            var rest = uri.Substring(pos + 1);

#if WINDOWS_PHONE || PORTABLE
            lock(_lock)
            {
                string prefix;
                if (_shortValueMappings.TryGetValue(shortValue, out prefix))
                {
                    return prefix + rest;
                }
                throw new BrightstarInternalException("No match for short prefix");
            }
#else
            _lock.EnterReadLock();
            try
            {
                string prefix;
                if (_shortValueMappings.TryGetValue(shortValue, out prefix))
                {
                    return prefix + rest;
                }
                else
                {
                    throw new BrightstarInternalException("No match for short prefix");
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
#endif
        }

        #endregion

        /// <summary>
        /// Loads the prefix collection reading from the specified start page in the page store
        /// </summary>
        /// <param name="page"></param>
        /// <param name="profiler"></param>
        private void Load(IPage page, BrightstarProfiler profiler)
        {
#if WINDOWS_PHONE || PORTABLE
            lock (_lock)
            {
                InterlockedLoad(page, profiler);
            }
#else
            _lock.EnterWriteLock();
            try
            {
                InterlockedLoad(page, profiler);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
#endif
        }

        /// <summary>
        /// Performs the actual load of prefixes from a page.
        /// </summary>
        /// <param name="page"></param>
        /// <param name="profiler"></param>
        /// <remarks>Calls to this method should be made inside a critical section of code protected with a mutex or reader/writer lock</remarks>
        private void InterlockedLoad(IPage page, BrightstarProfiler profiler)
        {
            using (profiler.Step("PrefixManager.InterlockedLoad"))
            {
                int offset = 0;
                while (offset < _pageStore.PageSize)
                {
                    ushort prefixLength = BitConverter.ToUInt16(page.Data, offset);
                    offset += 2;
                    if (prefixLength == ushort.MaxValue)
                    {
                        ulong nextPageId = BitConverter.ToUInt64(page.Data, offset);
                        if (nextPageId == 0)
                        {
                            // End of data
                            return;
                        }
                        page = _pageStore.Retrieve(nextPageId, profiler);
                        offset = 0;
                    }
                    else
                    {
                        var prefix = Encoding.UTF8.GetString(page.Data, offset, prefixLength);
                        offset += prefixLength;
                        var uriLen = BitConverter.ToUInt16(page.Data, offset);
                        offset += 2;
                        var uri = Encoding.UTF8.GetString(page.Data, offset, uriLen);
                        offset += uriLen;
                        _prefixMappings[uri] = prefix;
                        _shortValueMappings[prefix] = uri;
                    }
                }
            }
        }
        /// <summary>
        /// Saves the prefix collection to one or more pages in the page store
        /// </summary>
        /// <returns>The ID of the start page of the collection</returns>
        public ulong Save(ulong transactionId, BrightstarProfiler profiler)
        {
            return Write(_pageStore, transactionId, profiler);
        }


        public ulong Write(IPageStore pageStore, ulong transactionId, BrightstarProfiler profiler)
        {
            IPage startPage = pageStore.Create(transactionId);
            IPage currentPage = startPage;
            byte[] buff = new byte[pageStore.PageSize];
            int offset = 0;
            foreach (var entry in _shortValueMappings)
            {
                byte[] encodedPrefix = Encoding.UTF8.GetBytes(entry.Key);
                byte[] encodedUri = Encoding.UTF8.GetBytes(entry.Value);
                int totalLength = encodedUri.Length + encodedPrefix.Length + 4;
                if (offset + totalLength > (pageStore.PageSize - 10))
                {
                    // Not enough room for the entry and the next page pointer
                    // So create a new page for this entry and write a pointer to it
                    // onto the current page
                    IPage nextPage = pageStore.Create(transactionId);
                    BitConverter.GetBytes(ushort.MaxValue).CopyTo(buff, offset);
                    offset += 2;
                    BitConverter.GetBytes(nextPage.Id).CopyTo(buff, offset);
                    currentPage.SetData(buff);
                    currentPage = nextPage;
                    buff = new byte[pageStore.PageSize];
                    offset = 0;
                }
                BitConverter.GetBytes((ushort)encodedPrefix.Length).CopyTo(buff, offset);
                offset += 2;
                encodedPrefix.CopyTo(buff, offset);
                offset += encodedPrefix.Length;
                BitConverter.GetBytes((ushort)encodedUri.Length).CopyTo(buff, offset);
                offset += 2;
                encodedUri.CopyTo(buff, offset);
                offset += encodedUri.Length;
            }
            // Write the end marker
            BitConverter.GetBytes(ushort.MaxValue).CopyTo(buff, offset);
            offset += 2;
            BitConverter.GetBytes(0ul).CopyTo(buff, offset);
            currentPage.SetData(buff);
            return startPage.Id;
        }
    }
}
