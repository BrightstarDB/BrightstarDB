using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BrightstarDB.Azure.Common;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace BrightstarDB.Azure.StoreWorker
{
    /// <summary>
    /// An in-memory implementation of the IBlockStoreCache interface that
    /// uses a concurrent dictionary for thread-safe caching and a 
    /// background thread for cache eviction
    /// </summary>
    /// <remarks>
    /// Cache eviction kicks in when the number of cached blocks
    /// exceeeds a threshold determined as (Heap to use) / 4097Kb. The eviction
    /// thread will remove 25% of entries  based on a simple LRU algorithm.
    /// </remarks>
    public class BlockStoreCache : IBlockStoreCache
    {
        private readonly ConcurrentDictionary<string, BlockInfo> _cache;
        private readonly long _maxBlockCount;
        private const long BlockInfoSize = (4096*1024) + 1024; // 4MB for data, 1kb for metadata
        private volatile bool _evictionStarted;
        private readonly LocalResource _localStorage;
        private readonly IBlockStoreDiskCache _diskCache;

        public BlockStoreCache(long maxHeap, string localStorageKey)
        {
            _maxBlockCount = maxHeap/BlockInfoSize;
            _cache = new ConcurrentDictionary<string, BlockInfo>();
            _localStorage = RoleEnvironment.GetLocalResource(localStorageKey);
            int capacity = (_localStorage.MaximumSizeInMegabytes/4) - 10; // Just a bit of a safety buffer
            _diskCache = new BlockStoreDiskCache(_localStorage.RootPath + Path.DirectorySeparatorChar + "bscache",
                                                 capacity, AzureBlockStore.AzureBlockSize);
        }

        public void Insert(BlockInfo block)
        {
            block.LastAccess = DateTime.UtcNow.Ticks;
            _cache[MakeKey(block)] = block;
            //Trace.WriteLine(String.Format("BlockStoreCache: Insert {0} with key {1}", block, MakeKey(block)));
            if (_cache.Count > _maxBlockCount && !_evictionStarted)
            {
                Task.Factory.StartNew(EvictOldBlocks);
            }
            if (block.Length == AzureBlockStore.AzureBlockSize)
            {
                WriteToDiskCache(block);
            }
        }

        private void EvictOldBlocks()
        {
            try
            {
                _evictionStarted = true;
                Trace.TraceInformation("BlockStoreCache: Eviction started with {0} blocks in the memory cache.",
                                       _cache.Count);
                foreach (var toEvict in _cache.Values.OrderBy(v => v.LastAccess).Take(_cache.Count/4))
                {
                    BlockInfo removedBlock;
                    _cache.TryRemove(MakeKey(toEvict), out removedBlock);
                }
                Trace.TraceInformation("BlockStoreCache: Eviction compelted with {0} blocks in the memory cache.",
                                       _cache.Count);
                _evictionStarted = false;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error in BlockStoreCache.EvictOldBlocks: {0}", ex);
            }
        }

        public BlockInfo Lookup(string path, long offset)
        {
            var key = MakeKey(path, offset);
            BlockInfo block;
            if (_cache.TryGetValue(key, out block))
            {
                block.LastAccess = DateTime.UtcNow.Ticks;
                return block;
            }
            return LookupInDiskCache(key, path, offset);
        }

        private static string MakeKey(BlockInfo block)
        {
            return MakeKey(block.StoreName, block.Offset);
        }

        private static string MakeKey(string storeName, long offset)
        {
            return storeName + "_" + offset;
        }

        public void InvalidateBlocks(string path)
        {
            foreach(var key in _cache.Keys.Where(k=>k.StartsWith(path)))
            {
                BlockInfo removedBlock;
                _cache.TryRemove(key, out removedBlock);
            }
        }

        private static string MakeCacheFileName(BlockInfo block)
        {
            return MakeCacheFileName(MakeKey(block));
        }

        private static string MakeCacheFileName(string cacheKey)
        {
            return cacheKey.Replace(Path.DirectorySeparatorChar, '_');
        }

        private void WriteToDiskCache(BlockInfo block)
        {
            // Start a background thread to write the block to the disk
            Task.Factory.StartNew(
                () =>
                    {
                        try
                        {
                            _diskCache.PutData(MakeKey(block), block.Data);
                        }
                        catch (Exception e)
                        {
                            Trace.TraceWarning("Error writing block to disk cache: {0}", e);
                        }
                    });
        }

        private BlockInfo LookupInDiskCache(string key, string blockPath, long blockOffset)
        {
            byte[] buffer = new byte[AzureBlockStore.AzureBlockSize];
            if (_diskCache.TryGetData(key, buffer))
            {
                return new BlockInfo
                           {
                               Data = buffer,
                               LastAccess = DateTime.UtcNow.Ticks,
                               Length = buffer.Length,
                               Offset = blockOffset,
                               StoreName = blockPath
                           };
            }
            return null;
        }

    }
}
