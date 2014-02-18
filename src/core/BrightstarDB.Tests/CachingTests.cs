using System;
using System.IO;
using System.Threading;
using BrightstarDB.Caching;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class CachingTests
    {
        [Test]
        public void TestMemoryCacheBasicOperations()
        {
            ICache cache = new MemoryCache(2000, new LruCacheEvictionPolicy());
            RunCacheTests(cache);
        }

        private static void RunCacheTests(ICache cache)
        {
            cache.Insert("ByteArray", new byte[] {0, 1, 2, 3}, CachePriority.Normal);
            cache.Insert("String", "Hello World", CachePriority.Normal);
            cache.Insert("Object", new TestObject("Test Object", 1234), CachePriority.Normal);

            var byteArray = cache.Lookup("ByteArray");
            Assert.IsNotNull(byteArray);
            Assert.AreEqual(4, byteArray.Length);
            for (int i = 0; i < 4; i++) Assert.AreEqual(i, byteArray[i]);

            var cachedString = cache.Lookup<string>("String");
            Assert.IsNotNull(cachedString);
            Assert.AreEqual("Hello World", cachedString);

            var cachedObject = cache.Lookup<TestObject>("Object");
            Assert.IsNotNull(cachedObject);
            Assert.AreEqual("Test Object", cachedObject.StringValue);
            Assert.AreEqual(1234, cachedObject.LongValue);

            cache.Remove("Object");
            Assert.IsNull(cache.Lookup("Object"));
            Assert.IsFalse(cache.ContainsKey("Object"));
            Assert.IsTrue(cache.ContainsKey("String"));
        }

#if !SILVERLIGHT && !PORTABLE
        [Test]
        public void TestDirectoryCacheBasicOperations()
        {
            string cacheDirPath = Path.Combine(Path.GetTempPath(), "cachetest_" + DateTime.Now.Ticks);
            ICache cache = new DirectoryCache(cacheDirPath, 2000, new LruCacheEvictionPolicy());
            RunCacheTests(cache);
            ICache reopenedCache = new DirectoryCache(cacheDirPath, 2000, new LruCacheEvictionPolicy());
            var byteArray = cache.Lookup("ByteArray");
            Assert.IsNotNull(byteArray);
            Assert.AreEqual(4, byteArray.Length);
            for (int i = 0; i < 4; i++) Assert.AreEqual(i, byteArray[i]);

            var cachedString = cache.Lookup<string>("String");
            Assert.IsNotNull(cachedString);
            Assert.AreEqual("Hello World", cachedString);
            Assert.IsFalse(reopenedCache.ContainsKey("Object"));           
        }
#endif

#if !PORTABLE
        [Test]
        public void TestLruPolicy()
        {
            var hundredBytes = new byte[100];
            ICache cache = new MemoryCache(1000, new LruCacheEvictionPolicy(), 900, 700);
            for(int i = 0; i < 9;i++)
            {
                cache.Insert("Entry " + i, hundredBytes, CachePriority.Normal);
                Thread.Sleep(20);
            }
            // Cache size is now 900 bytes

            cache.Insert("Entry 10", hundredBytes, CachePriority.Normal);
            Assert.IsFalse(cache.ContainsKey("Entry 0"));
            Assert.IsFalse(cache.ContainsKey("Entry 1"));
            Assert.IsTrue(cache.ContainsKey("Entry 2"));
            Assert.IsTrue(cache.ContainsKey("Entry 10"));
            
            // Cache size should now be 800 bytes
            cache.Lookup("Entry 2");
            cache.Insert("Entry 11", hundredBytes, CachePriority.Normal);
            cache.Insert("Entry 12", hundredBytes, CachePriority.Normal); // This insert should force eviction
            Assert.IsFalse(cache.ContainsKey("Entry 0")); // was previously evicted
            Assert.IsFalse(cache.ContainsKey("Entry 1")); // was previously evicted
            Assert.IsTrue(cache.ContainsKey("Entry 2"), "Expected Entry2 to remain in cache after second eviction"); // Won't be evicted due to recent access
            Assert.IsFalse(cache.ContainsKey("Entry 3")); // newly evicted
            Assert.IsFalse(cache.ContainsKey("Entry 4")); // newly evicted
            Assert.IsTrue(cache.ContainsKey("Entry 5"), "Expected Entry 5 to remain in cache after second eviction"); // shouldn't be evicted
            Assert.IsTrue(cache.ContainsKey("Entry 11"), "Expected Entry 11 to remain in cache after second eviction"); // should have been added before eviction
            Assert.IsTrue(cache.ContainsKey("Entry 12"), "Expected Entry 12 to be added to cache after second eviction"); // should have been added after eviction

        }

        [Test]
        public void TestLruPriority()
        {
            var hundredBytes = new byte[100];
            ICache cache = new MemoryCache(1000, new LruCacheEvictionPolicy(), 900, 700);
            for (int i = 0; i < 9; i++)
            {
                cache.Insert("Entry " + i, hundredBytes, i%2 == 0 ? CachePriority.High : CachePriority.Normal);
                Thread.Sleep(20);
            }
            // Cache size is now 900 bytes

            cache.Insert("Entry 10", hundredBytes, CachePriority.High);
            Assert.IsTrue(cache.ContainsKey("Entry 0"), "Expected Entry 0 to remain after first eviction");
            Assert.IsFalse(cache.ContainsKey("Entry 1"), "Expected Entry 1 to be removed after first eviction");
            Assert.IsTrue(cache.ContainsKey("Entry 2"), "Expected Entry 2 to remain after first eviction");
            Assert.IsFalse(cache.ContainsKey("Entry 3"), "Expected Entry 3 to be removed after first eviction");
            Assert.IsTrue(cache.ContainsKey("Entry 4"));
            Assert.IsTrue(cache.ContainsKey("Entry 5"));
            Assert.IsTrue(cache.ContainsKey("Entry 10"));

            cache.Insert("Entry 11", hundredBytes, CachePriority.High);
            cache.Insert("Entry 12", hundredBytes, CachePriority.High);
            Assert.IsFalse(cache.ContainsKey("Entry 5"), "Expected Entry 5 to be removed after second eviction");
            Assert.IsFalse(cache.ContainsKey("Entry 7"), "Expected Entry 7 to be removed after second eviction");

            Assert.IsNotNull(cache.Lookup("Entry 2"));
            cache.Insert("Entry 13", hundredBytes, CachePriority.High);
            cache.Insert("Entry 14", hundredBytes, CachePriority.High);
            Assert.IsFalse(cache.ContainsKey("Entry 0")); // Should now start evicting high priority items
            Assert.IsTrue(cache.ContainsKey("Entry 2"), "Expected Entry 2 to remain after third eviction due to recent access");
            Assert.IsFalse(cache.ContainsKey("Entry 4"), "Expected Entry 4 to be removed after third eviction"); 
        }

        [Test]
        public void TestTwoLevelCache()
        {
            var hundredBytes = new byte[100];
            ICache primary = new MemoryCache(500, new LruCacheEvictionPolicy(), 400, 300);
            ICache secondary = new MemoryCache(1000, new LruCacheEvictionPolicy(), 900, 700);
            ICache cache = new TwoLevelCache(primary, secondary);

            for (int i = 0; i < 9; i++)
            {
                cache.Insert("Entry " + i, hundredBytes, CachePriority.Normal);
                Thread.Sleep(20);
            }

            for(int i = 0; i < 9; i++)
            {
                Assert.IsTrue(secondary.ContainsKey("Entry " + i), "Expected secondary cache to contain Entry {0} after initialization");
                if (i < 5) Assert.IsFalse(primary.ContainsKey("Entry " + i), "Expected primary cache to NOT contain Entry {0} after initialization", i);
                else Assert.IsTrue(primary.ContainsKey("Entry " + i), "Expected primary cache to contain Entry {0} after initialization", i);
            }

            cache.Insert("Entry 10", hundredBytes, CachePriority.Normal);
            Assert.IsFalse(cache.ContainsKey("Entry 0"));
            Assert.IsFalse(cache.ContainsKey("Entry 1"));
            Assert.IsTrue(cache.ContainsKey("Entry 2"));
            Assert.IsTrue(cache.ContainsKey("Entry 10"));
            Assert.IsTrue(primary.ContainsKey("Entry 10"));
            Assert.IsTrue(secondary.ContainsKey("Entry 10"));
        }
#endif
    }

#if !SILVERLIGHT && !NETFX_CORE && !PORTABLE
    [Serializable]
#endif
    public class TestObject
    {
        public string StringValue { get; set; }
        public long LongValue { get; set; }
        public TestObject() {}
        public TestObject(string stringValue, long longValue)
        {
            StringValue = stringValue;
            LongValue = longValue;
        }
    }
}
