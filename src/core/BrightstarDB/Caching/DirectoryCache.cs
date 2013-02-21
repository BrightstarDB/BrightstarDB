using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrightstarDB.Caching
{
    /// <summary>
    /// A cache implementation that stores cache entries as files
    /// </summary>
    public class DirectoryCache : AbstractCache
    {
        private readonly DirectoryInfo _baseDir;
        private readonly DirectoryInfo _normalDir;
        private readonly DirectoryInfo _highDir;

        /// <summary>
        /// Creates a new directory cache
        /// </summary>
        /// <param name="directoryPath">The path to the directory to contain the cache. The directory will be created if it does not already exist</param>
        /// <param name="cacheMaxSize">The maximum size of the cache in bytes</param>
        /// <param name="cacheEvictionPolicy">The policy to use to maintain cache size</param>
        /// <param name="highwaterMark">The cache size (in bytes) above which the eviction policy will be applied</param>
        /// <param name="lowwaterMark">The cache size (in bytes) that the eviction policy will attempt to reduce the cache to when it runs</param>
        public DirectoryCache(string directoryPath, long cacheMaxSize, ICacheEvictionPolicy cacheEvictionPolicy, long highwaterMark = 0, long lowwaterMark = 0) : base(cacheMaxSize, cacheEvictionPolicy, highwaterMark, lowwaterMark)
        {
            _baseDir = new DirectoryInfo(directoryPath);
            if (!_baseDir.Exists) _baseDir.Create();
            _normalDir = new DirectoryInfo(Path.Combine(_baseDir.FullName, "normal"));
            _highDir = new DirectoryInfo(Path.Combine(_baseDir.FullName, "high"));
            if (!_normalDir.Exists) _normalDir.Create();
            if (!_highDir.Exists) _highDir.Create();
            CacheSize = _normalDir.GetFiles().Sum(f => f.Length);
            CacheSize += _highDir.GetFiles().Sum(f => f.Length);
        }

        #region Overrides of AbstractCache

        /// <summary>
        /// Provides an enumeration over the entries in the cache.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<AbstractCacheEntry> ListEntries()
        {
#if SILVERLIGHT
            return _highDir.GetFiles().Select(f => new DirectoryCacheEntry(f, CachePriority.High)).Union(
                _normalDir.GetFiles().Select(f => new DirectoryCacheEntry(f, CachePriority.Normal))).Cast<AbstractCacheEntry>();
#else
            return _highDir.EnumerateFiles().Select(f => new DirectoryCacheEntry(f, CachePriority.High)).Union(
                _normalDir.EnumerateFiles().Select(f => new DirectoryCacheEntry(f, CachePriority.Normal)));
#endif
        }

        /// <summary>
        /// Cache entry for file-based cache
        /// </summary>
        private class DirectoryCacheEntry : AbstractCacheEntry
        {
            private readonly FileInfo _fileInfo;

            public DirectoryCacheEntry(FileInfo fileInfo, CachePriority priority)
            {
                _fileInfo = fileInfo;
                Key = fileInfo.Name;
                Priority = priority;
                Size = fileInfo.Length;
            }

            #region Overrides of AbstractCacheEntry

            public override byte[] GetBytes()
            {
#if SILVERLIGHT
                var buff = new byte[_fileInfo.Length];
                using (var s = _fileInfo.OpenRead())
                {
                    s.Read(buff, 0, (int)_fileInfo.Length);
                    s.Close();
                }
                return buff;
#else
                return File.ReadAllBytes(_fileInfo.FullName);
#endif
            }

            #endregion
        }

        private string KeyToFileName(string key)
        {
            return key.Replace(Path.DirectorySeparatorChar, '_');
        }
        /// <summary>
        /// Implemented in derived classes to add a new entry to the cache
        /// </summary>
        /// <param name="key">The key for the new entry</param>
        /// <param name="data">The data for the new entry</param>
        /// <param name="cachePriority">The entry priority</param>
        /// <returns>The newly created cache entry</returns>
        protected override AbstractCacheEntry AddEntry(string key, byte[] data, CachePriority cachePriority)
        {
            try
            {
                var fileName = KeyToFileName(key);
                var dir = cachePriority == CachePriority.High ? _highDir : _normalDir;
                var cacheFilePath = Path.Combine(dir.FullName, fileName);
#if SILVERLIGHT
                using(var w = File.OpenWrite(cacheFilePath))
                {
                    w.Write(data, 0, data.Length);
                    w.Flush(true);
                    w.Close();
                }
#else
                File.WriteAllBytes(cacheFilePath, data);
#endif
                return new DirectoryCacheEntry(new FileInfo(cacheFilePath), cachePriority);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Implemented in dervied classes to retrieve an entry from the cache
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>The cache entry found or null if there was no match on <paramref name="key"/></returns>
        protected override AbstractCacheEntry GetEntry(string key)
        {
            var fileName = KeyToFileName(key);
            var normalFile = new FileInfo(Path.Combine(_normalDir.FullName, fileName));
            if(normalFile.Exists)return new DirectoryCacheEntry(normalFile, CachePriority.Normal);
            var highFile = new FileInfo(Path.Combine(_highDir.FullName, fileName));
            if (highFile.Exists) return new DirectoryCacheEntry(highFile, CachePriority.High);
            return null;
        }

        /// <summary>
        /// Removes the entry with the specified key from the cache
        /// </summary>
        /// <param name="key">The key of the entry to be removed</param>
        /// <returns>The number of bytes of data evicted from the cache as a result of this operation. May be 0 if the key was not found in the cache.</returns>
        protected override long RemoveEntry(string key)
        {
            var fileName = KeyToFileName(key);
            var normalFile = new FileInfo(Path.Combine(_normalDir.FullName, fileName));
            if (normalFile.Exists)
            {
                long bytesDeleted = normalFile.Length;
                normalFile.Delete();
                return bytesDeleted;
            }
            var highFile = new FileInfo(Path.Combine(_highDir.FullName, fileName));
            if (highFile.Exists)
            {
                long bytesDeleted = highFile.Length;
                highFile.Delete();
                return bytesDeleted;
            }
            return 0;
        }

        #endregion


    }
}
