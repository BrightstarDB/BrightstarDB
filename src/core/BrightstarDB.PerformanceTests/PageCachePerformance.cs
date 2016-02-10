using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;
using NUnit.Framework;

namespace BrightstarDB.PerformanceTests
{
    [TestFixture]
    public class PageCachePerformance
    {
        private List<ulong> _pageIdList = new List<ulong>();
        private Random _random = new Random();

        [TestFixtureSetUp]
        public void SetUp()
        {
            // Read the random page id list from samples.bin in the data directory
            // These are stored on disk as 4-byte Int32 values and need to be cast
            // to long for use in the page cache.
            //var buff = new byte[4];
            //using (var samples = File.OpenRead("data\\samples.bin"))
            //{
            //    while (samples.Read(buff, 0, 4) > 0)
            //    {
            //        _pageIdList.Add((ulong)BitConverter.ToInt32(buff, 0));
            //    }
            //}
            //Console.WriteLine("Read {0} data points from samples.bin file", _pageIdList.Count);
            //Console.WriteLine(String.Join(", ",_pageIdList.Take(20)));


            _pageIdList = new List<ulong>(GenerateAccesses(1000000, 4, 42));
            Console.WriteLine(String.Join(", ", _pageIdList.Take(20)));
        }

        private IEnumerable<ulong> GenerateAccesses(int n, int h, int b)
        {
            for (int i = 0; i < n; i++)
            {
                yield return 0ul;
                ulong lowerBound = 1;
                for (int d = 1; d <= h; d++)
                {
                    ulong entryCount = (ulong)Math.Pow(b, d);
                    yield return lowerBound + (ulong)(_random.NextDouble() * entryCount);
                    lowerBound += entryCount;
                }
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void TestLruPageCache(int threadCount)
        {
            var cache = new LruPageCache(2<<18); // 2GB cache with 4k pages - default watermarks
            RunCacheTest(cache, 5, threadCount);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void TestMemoryPageCache(int threadCount)
        {
            var cache = new MemoryPageCache(2048, 17);
            RunCacheTest(cache, 5, threadCount);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void TestLruPageCache2(int threadCount)
        {
            var cache = new LruPageCache2(2<<18, 8);
            RunCacheTest(cache, 5, threadCount);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void TestLruPageCache3(int threadCount)
        {
            var cache = new LruPageCache3(2 << 18, 8);
            RunCacheTest(cache, 5, threadCount);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void TestLruPageCache4(int threadCount)
        {
            var cache = new LruPageCache4(2 << 18, 8);
            RunCacheTest(cache, 5, threadCount);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        public void TestLruPageCache5(int threadCount)
        {
            var cache = new LruPageCache5(2 << 18, 8);
            RunCacheTest(cache, 5, threadCount);
        }

        private void RunCacheTest(IPageCache pageCache, int runCount, int threadCount)
        {
            var aggregateResults = new List<CacheTestResults>();
            for (int i = 0; i < runCount + 1; i++) // +1 to allow one warmup run that will be discarded
            {
                var sw = new Stopwatch();
                var runTasks = new Task<CacheTestResults>[threadCount];

                for (int t = 0; t < threadCount; t++)
                {
                    var threadNum = t;
                    runTasks[t] = new Task<CacheTestResults>(() => RunCacheTest(pageCache, (_pageIdList.Count / threadCount) * threadNum));
                }
                foreach (var t in runTasks) t.Start();
                sw.Start();
                Task.WaitAll(runTasks.Cast<Task>().ToArray());
                sw.Stop();
                aggregateResults.Add(new CacheTestResults(sw.Elapsed, runTasks.Select(t => t.Result)));
                Console.WriteLine("Run #{0} completed in {1}ms", i, sw.ElapsedMilliseconds);
            }
            Console.WriteLine(
                "Test completed.\n\tAverage run time: {0}ms\n\tAverage Hit Count: {1}\n\tAverage Hit Rate: {2}%",
                aggregateResults.Skip(1).Average(r => r.TimeTaken.TotalMilliseconds),
                aggregateResults.Skip(1).Average(r => r.HitCount),
                aggregateResults.Skip(1).Sum(r => r.HitCount) * 100.0 / (runCount * threadCount * _pageIdList.Count));

        }

        private CacheTestResults RunCacheTest(IPageCache pageCache, int sampleStart)
        {
            var i = sampleStart;
            var hitCount = 0;
            var sw = new Stopwatch();
            sw.Start();
            do
            {
                var cacheItem = pageCache.Lookup("test", _pageIdList[i]);
                if (cacheItem == null)
                {
                    pageCache.InsertOrUpdate("test", new DummyPage(_pageIdList[i]));
                }
                else
                {
                    hitCount++;
                }
                i++;
                if (i == _pageIdList.Count) i = 0;
            } while (i != sampleStart);
            sw.Stop();
            
            var results = new CacheTestResults(sw.Elapsed, hitCount);
            Console.WriteLine(results);
            return results;
        }

        private class CacheTestResults
        {
            public TimeSpan TimeTaken { get; }
            public int HitCount { get; }
            private List<CacheTestResults> AggregatedResults { get; set; }

            public CacheTestResults(TimeSpan timeTaken, int hitCount)
            {
                TimeTaken = timeTaken;
                HitCount = hitCount;
                AggregatedResults = new List<CacheTestResults>();
            }

            public CacheTestResults(TimeSpan aggregateTime, IEnumerable<CacheTestResults> childResults)
            {
                TimeTaken = aggregateTime;
                AggregatedResults = new List<CacheTestResults>(childResults);
                HitCount = AggregatedResults.Sum(t => t.HitCount);
            }

            public override string ToString()
            {
                return $"{HitCount} cache hits in {TimeTaken.TotalMilliseconds}ms";
            }
        }

        private class DummyPage : IPageCacheItem
        {
            public ulong Id { get; }

            byte[] Data { get; }

            public DummyPage(ulong id)
            {
                Id = id;
                Data = new byte[4096];
            }
        }

        private class MemoryPageCache : IPageCache
        {
            private readonly MemoryCache _cache;
            private readonly CacheItemPolicy _cachePolicy;
            public MemoryPageCache(int capacityMb, int memoryPercentage)
            {
                _cache = new MemoryCache(Guid.NewGuid().ToString(), new NameValueCollection {
                    { "CacheMemoryLimitMegabytes", capacityMb.ToString() },
                    { "PhysicalMemoryLimitPercentage", memoryPercentage.ToString() },
                    //{"PollingInterval", "00:00:02"}
                });
                _cachePolicy = new CacheItemPolicy {SlidingExpiration = TimeSpan.FromSeconds(10)};
            }

            public event PreEvictionDelegate BeforeEvict;
            public event PostEvictionDelegate AfterEvict;
            public event EvictionCompletedDelegate EvictionCompleted;
            public void InsertOrUpdate(string partition, IPageCacheItem page)
            {
                _cache.Set(MakeCacheKey(partition, page.Id), page, _cachePolicy);
            }

            public IPageCacheItem Lookup(string partition, ulong pageId)
            {
                return _cache.Get(MakeCacheKey(partition, pageId)) as IPageCacheItem;
            }

            private string MakeCacheKey(string partition, ulong pageId)
            {
                return partition + ":" + pageId;
            }

            public void Clear(string partition)
            {
                // Cannot be implemented as there is no way to iterate all entries with a given key prefix.
                // This might not be a massive deal breaker as sliding expiration will force unused partiion
                // pages out quite quickly anyway
            }

            public int FreePages => (int) (_cache.CacheMemoryLimit/4096);
            public void Clear()
            {
                _cache.Trim(100);
            }
        }
    }

    internal class LruPageCache2 : IPageCache
    {
        private readonly Dictionary<string, LinkedListNode<SegmentEntry>> _map;
        private int _currentSegment;
        private readonly LinkedList<SegmentEntry>[] _cacheSegments;
        private readonly ReaderWriterLockSlim _lock;
        private readonly int _segmentCount;
        private readonly int _segmentCapacity;
        private readonly int _thresholdCapacity;

        public event PreEvictionDelegate BeforeEvict;
        public event PostEvictionDelegate AfterEvict;
        public event EvictionCompletedDelegate EvictionCompleted;

        public LruPageCache2(int totalCapacity, int segmentCount = 5)
        {
            _map = new Dictionary<string, LinkedListNode<SegmentEntry>>();
            _segmentCount = segmentCount;
            _currentSegment = 0;
            _cacheSegments = new LinkedList<SegmentEntry>[segmentCount];
            for (var i = 0; i < segmentCount; i++)
            {
                _cacheSegments[i] = new LinkedList<SegmentEntry>();
            }
            _segmentCapacity = (totalCapacity/segmentCount);
            _thresholdCapacity = _segmentCapacity*(segmentCount - 1);
            _lock = new ReaderWriterLockSlim();
        }

        private string MakeCacheKey(string partition, ulong pageId)
        {
            return partition + ":" + pageId;
        }

        public void InsertOrUpdate(string partition, IPageCacheItem page)
        {
            var cacheKey = MakeCacheKey(partition, page.Id);
            var entry = new SegmentEntry(page, _currentSegment);
            _lock.EnterWriteLock();
            try
            {
                LinkedListNode<SegmentEntry> existingEntry;
                if (_map.TryGetValue(cacheKey, out existingEntry))
                {
                    existingEntry.Value = entry;
                }
                else
                {
                    var llNode = new LinkedListNode<SegmentEntry>(entry);
                    _map.Add(cacheKey, llNode);
                    _cacheSegments[_currentSegment].AddLast(llNode);
                    if (_map.Count%_segmentCapacity == 0)
                    {
                        _currentSegment += 1;
                        if (_currentSegment == _segmentCount) _currentSegment = 0;
                    }
                    if (_map.Count > _thresholdCapacity)
                    {
                        Purge();
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IPageCacheItem Lookup(string partition, ulong pageId)
        {
            _lock.EnterReadLock();
            try
            {
                var cacheKey = MakeCacheKey(partition, pageId);
                LinkedListNode<SegmentEntry> existingEntry;
                if (_map.TryGetValue(cacheKey, out existingEntry))
                {
                    existingEntry.Value.SegmentId = _currentSegment;
                    return existingEntry.Value.CacheItem;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear(string partition)
        {
            _lock.EnterReadLock();
            try
            {
                var partitionPrefix = partition + ":";
                var keysToRemove = _map.Keys.Where(k => k.StartsWith(partitionPrefix)).ToList();
                foreach (var k in keysToRemove)
                {
                    RemoveEntry(k);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void RemoveEntry(string k)
        {
            LinkedListNode<SegmentEntry> entryToUnlink;
            if (_map.TryGetValue(k, out entryToUnlink))
            {
                entryToUnlink.List.Remove(entryToUnlink);
            }
            _map.Remove(k);
        }

        public int FreePages { get; }

        public void Clear()
        {
            _map.Clear();
            foreach (var segment in _cacheSegments)
            {
                segment.Clear();
            }
        }

        private void Purge()
        {
            for (int i = 1; i < _segmentCount; i++)
            {
                PurgeSegment((_currentSegment + i)%_segmentCount);
                if (_map.Count < _thresholdCapacity) return;
            }
        }

        private void PurgeSegment(int segmentId)
        {
            var toPurge = new List<string>(_segmentCapacity);
            toPurge.AddRange(_map.Where(x=>x.Value.Value.SegmentId == segmentId).Select(x=>x.Key));
            foreach (var p in toPurge)
            {
                RemoveEntry(p);
            }
        }

        private class SegmentEntry
        {
            public IPageCacheItem CacheItem { get; }
            public int SegmentId { get; set; }

            public SegmentEntry(IPageCacheItem cacheItem, int segmentId)
            {
                CacheItem = cacheItem;
                SegmentId = segmentId;
            }
        }
    }

    internal class LruPageCache3 : IPageCache
    {
        private readonly Dictionary<SegmentKey, SegmentEntry> _map;
        private int _currentSegment;
        private readonly ReaderWriterLockSlim _lock;
        private readonly int _segmentCount;
        private readonly int _segmentCapacity;
        private readonly int _thresholdCapacity;

        private class SegmentKey
        {
            public string Partition { get; private set; }
            public ulong PageId { get; private set; }

            private readonly int _hashCode;
            public SegmentKey(string partition, ulong pageId)
            {
                Partition = partition;
                PageId = pageId;
                _hashCode = PageId.GetHashCode() ^ Partition.GetHashCode();
            }
            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                var other = obj as SegmentKey;
                if (other == null) return false;
                return Partition.Equals(other.Partition) && PageId.Equals(other.PageId);
            }
        }
        public event PreEvictionDelegate BeforeEvict;
        public event PostEvictionDelegate AfterEvict;
        public event EvictionCompletedDelegate EvictionCompleted;

        public LruPageCache3(int totalCapacity, int segmentCount = 5)
        {
            _map = new Dictionary<SegmentKey, SegmentEntry>();
            _segmentCount = segmentCount;
            _currentSegment = 0;
            _segmentCapacity = (totalCapacity / segmentCount);
            _thresholdCapacity = (_segmentCapacity * segmentCount)-1;
            _lock = new ReaderWriterLockSlim();
        }

        public void InsertOrUpdate(string partition, IPageCacheItem page)
        {
            var cacheKey = new SegmentKey(partition, page.Id);
            _lock.EnterWriteLock();
            try
            {
                _map[cacheKey] = new SegmentEntry(page, _currentSegment);
                if (_map.Count % _segmentCapacity == 0)
                {
                    _currentSegment += 1;
                    if (_currentSegment == _segmentCount) _currentSegment = 0;
                }
                if (_map.Count > _thresholdCapacity)
                {
                    Purge();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IPageCacheItem Lookup(string partition, ulong pageId)
        {
            _lock.EnterReadLock();
            try
            {
                var cacheKey = new SegmentKey(partition, pageId);
                SegmentEntry existingEntry;
                if (_map.TryGetValue(cacheKey, out existingEntry))
                {
                    Interlocked.Exchange(ref existingEntry.SegmentId, _currentSegment);
                    existingEntry.SegmentId = _currentSegment;
                    return existingEntry.CacheItem;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear(string partition)
        {
            _lock.EnterReadLock();
            try
            {
                var partitionPrefix = partition + ":";
                var keysToRemove = _map.Keys.Where(k => k.Partition.Equals(partition)).ToList();
                foreach (var k in keysToRemove)
                {
                    RemoveEntry(k);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            EvictionCompleted?.Invoke(this, new EventArgs());
        }

        private void RemoveEntry(SegmentKey k)
        {
            EvictionEventArgs args = null;
            if (BeforeEvict != null || AfterEvict != null)
            {
                args=new EvictionEventArgs(k.Partition, k.PageId);
            }
            BeforeEvict?.Invoke(this, args);
            if (args != null && args.CancelEviction) return;
            _map.Remove(k);
            AfterEvict?.Invoke(this, args);
        }

        public int FreePages => _thresholdCapacity - _map.Count;

        public void Clear()
        {
            _map.Clear();
        }

        private void Purge()
        {
            for (int i = 1; i < _segmentCount; i++)
            {
                PurgeSegment((_currentSegment + i) % _segmentCount);
                if (_map.Count < _thresholdCapacity) return;
            }
        }

        private void PurgeSegment(int segmentId)
        {
            var toPurge = new List<SegmentKey>(_segmentCapacity);
            toPurge.AddRange(_map.Where(x => x.Value.SegmentId == segmentId).Select(x => x.Key));
            foreach (var p in toPurge)
            {
                RemoveEntry(p);
            }
        }

        private struct SegmentEntry
        {
            public IPageCacheItem CacheItem { get; }
            public int SegmentId;

            public SegmentEntry(IPageCacheItem cacheItem, int segmentId)
            {
                CacheItem = cacheItem;
                SegmentId = segmentId;
            }
        }
    }

    internal class LruPageCache4 : IPageCache
    {
        private readonly ConcurrentDictionary<string, SegmentEntry> _map;
        private int _currentSegment;
        //private ReaderWriterLockSlim _lock;
        private readonly int _segmentCount;
        private readonly int _segmentCapacity;
        private readonly int _thresholdCapacity;

        public event PreEvictionDelegate BeforeEvict;
        public event PostEvictionDelegate AfterEvict;
        public event EvictionCompletedDelegate EvictionCompleted;

        public LruPageCache4(int totalCapacity, int segmentCount = 5)
        {
            _map = new ConcurrentDictionary<string, SegmentEntry>();
            _segmentCount = segmentCount;
            _currentSegment = 0;
            _segmentCapacity = (totalCapacity / segmentCount);
            _thresholdCapacity = _segmentCapacity * (segmentCount - 1);
            //_lock = new ReaderWriterLockSlim();
        }

        private string MakeCacheKey(string partition, ulong pageId)
        {
            return partition + ":" + pageId;
        }

        public void InsertOrUpdate(string partition, IPageCacheItem page)
        {
            var cacheKey = MakeCacheKey(partition, page.Id);
            _map[cacheKey] = new SegmentEntry(page, _currentSegment);
            if (_map.Count % _segmentCapacity == 0)
            {
                _currentSegment += 1;
                if (_currentSegment == _segmentCount) _currentSegment = 0;
            }
            if (_map.Count > _thresholdCapacity)
            {
                Purge();
            }
        }

        public IPageCacheItem Lookup(string partition, ulong pageId)
        {
            var cacheKey = MakeCacheKey(partition, pageId);
            SegmentEntry existingEntry;
            if (_map.TryGetValue(cacheKey, out existingEntry))
            {
                Interlocked.Exchange(ref existingEntry.SegmentId, _currentSegment);
                existingEntry.SegmentId = _currentSegment;
                return existingEntry.CacheItem;
            }
            return null;
        }

        public void Clear(string partition)
        {
            var partitionPrefix = partition + ":";
            var keysToRemove = _map.Keys.Where(k => k.StartsWith(partitionPrefix)).ToList();
            foreach (var k in keysToRemove)
            {
                RemoveEntry(k);
            }
        }

        private void RemoveEntry(string k)
        {
            SegmentEntry removed;
            _map.TryRemove(k, out removed);
        }

        public int FreePages { get; }

        public void Clear()
        {
            _map.Clear();
        }

        private void Purge()
        {
            for (int i = 1; i < _segmentCount; i++)
            {
                PurgeSegment((_currentSegment + i) % _segmentCount);
                if (_map.Count < _thresholdCapacity) return;
            }
        }

        private void PurgeSegment(int segmentId)
        {
            var toPurge = new List<string>(_segmentCapacity);
            toPurge.AddRange(_map.Where(x => x.Value.SegmentId == segmentId).Select(x => x.Key));
            foreach (var p in toPurge)
            {
                RemoveEntry(p);
            }
        }
        private class SegmentEntry
        {
            public IPageCacheItem CacheItem { get; }
            public int SegmentId;

            public SegmentEntry(IPageCacheItem cacheItem, int segmentId)
            {
                CacheItem = cacheItem;
                SegmentId = segmentId;
            }
        }
    }


    // Variant of LruPageCache3 with purge taking place in a background thread
    internal class LruPageCache5 : IPageCache
    {
        
        private readonly Dictionary<SegmentKey, SegmentEntry> _map;
        private int _currentSegment;
        private readonly ReaderWriterLockSlim _lock;
        private readonly int _segmentCount;
        private readonly int _segmentCapacity;
        private readonly int _thresholdCapacity;
        private bool _purgeStarted;

        private class SegmentKey
        {
            public string Partition { get; private set; }
            public ulong PageId { get; private set; }

            private readonly int _hashCode;
            public SegmentKey(string partition, ulong pageId)
            {
                Partition = partition;
                PageId = pageId;
                _hashCode = PageId.GetHashCode() ^ Partition.GetHashCode();
            }
            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                var other = obj as SegmentKey;
                if (other == null) return false;
                return Partition.Equals(other.Partition) && PageId.Equals(other.PageId);
            }
        }
        public event PreEvictionDelegate BeforeEvict;
        public event PostEvictionDelegate AfterEvict;
        public event EvictionCompletedDelegate EvictionCompleted;

        public LruPageCache5(int totalCapacity, int segmentCount = 5)
        {
            _map = new Dictionary<SegmentKey, SegmentEntry>();
            _segmentCount = segmentCount;
            _currentSegment = 0;
            _segmentCapacity = (totalCapacity / segmentCount);
            _thresholdCapacity = ((_segmentCapacity - 1)* segmentCount); // 1 full segment is reserved for additions that come in while the purge is taking place
            _lock = new ReaderWriterLockSlim();
        }

        public void InsertOrUpdate(string partition, IPageCacheItem page)
        {
            var cacheKey = new SegmentKey(partition, page.Id);
            _lock.EnterWriteLock();
            try
            {
                _map[cacheKey] = new SegmentEntry(page, _currentSegment);
                if (_map.Count % _segmentCapacity == 0)
                {
                    _currentSegment += 1;
                    if (_currentSegment == _segmentCount) _currentSegment = 0;
                }
                if (_map.Count > _thresholdCapacity)
                {
                    if (!_purgeStarted)
                    {
                        StartPurge((_currentSegment + 1)%_segmentCount);
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public IPageCacheItem Lookup(string partition, ulong pageId)
        {
            _lock.EnterReadLock();
            try
            {
                var cacheKey = new SegmentKey(partition, pageId);
                SegmentEntry existingEntry;
                if (_map.TryGetValue(cacheKey, out existingEntry))
                {
                    Interlocked.Exchange(ref existingEntry.SegmentId, _currentSegment);
                    existingEntry.SegmentId = _currentSegment;
                    return existingEntry.CacheItem;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Clear(string partition)
        {
            _lock.EnterReadLock();
            try
            {
                var keysToRemove = _map.Keys.Where(k => k.Partition.Equals(partition)).ToList();
                foreach (var k in keysToRemove)
                {
                    RemoveEntry(k);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            EvictionCompleted?.Invoke(this, new EventArgs());
        }

        private void RemoveEntry(SegmentKey k)
        {
            EvictionEventArgs args = null;
            if (BeforeEvict != null || AfterEvict != null)
            {
                args = new EvictionEventArgs(k.Partition, k.PageId);
            }
            BeforeEvict?.Invoke(this, args);
            if (args != null && args.CancelEviction) return;
            _map.Remove(k);
            AfterEvict?.Invoke(this, args);
        }

        public int FreePages => _thresholdCapacity - _map.Count;

        public void Clear()
        {
            _map.Clear();
        }

        private void StartPurge(int fromSegment)
        {
            _purgeStarted = true;
            var purgeTask = new Task(()=>Purge(fromSegment));
            purgeTask.ContinueWith(_ => { _purgeStarted = false; });
            purgeTask.Start();
        }

        private void Purge(int fromSegment)
        {
            for (var i = 0; i < (_segmentCount - 1); i++)
            {
                PurgeSegment((fromSegment + i) % _segmentCount);
                if (_map.Count < _thresholdCapacity) return;
            }
        }

        private void PurgeSegment(int segmentId)
        {
            var toPurge = new List<SegmentKey>(_segmentCapacity);
            _lock.EnterReadLock();
            try
            {
                toPurge.AddRange(_map.Where(x => x.Value.SegmentId == segmentId).Select(x => x.Key));
            }
            finally
            {
                _lock.ExitReadLock();
            }
                foreach (var p in toPurge)
                {
                    _lock.EnterWriteLock();
                try
                {
                    RemoveEntry(p);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        private struct SegmentEntry
        {
            public IPageCacheItem CacheItem { get; }
            public int SegmentId;

            public SegmentEntry(IPageCacheItem cacheItem, int segmentId)
            {
                CacheItem = cacheItem;
                SegmentId = segmentId;
            }
        }
    }
}
