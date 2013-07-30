using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class LruPageCacheTests
    {
        [Test]
        public void TestLookup()
        {
            var cache = new LruPageCache(10);
            cache.InsertOrUpdate("test", new TestCacheItem(1ul));
            cache.InsertOrUpdate("test", new TestCacheItem(2ul));

            var retrieved = cache.Lookup("test", 1ul);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(1ul, retrieved.Id);
            Assert.AreEqual(0, (retrieved as TestCacheItem).Version);

            var notRetrieved = cache.Lookup("test", 3ul);
            Assert.IsNull(notRetrieved);

            var wrongPartition = cache.Lookup("invalid", 1ul);
            Assert.IsNull(wrongPartition);
        }

        [Test]
        public void TestEvictionOnInsert()
        {
            var cache = new LruPageCache(10);
            for (int i = 1; i < 10; i++)
            {
                cache.InsertOrUpdate("test", new TestCacheItem((ulong) i, 1));
            }
            for (int i = 1; i < 10; i++)
            {
                Assert.IsNotNull(cache.Lookup("test", (ulong)i));
            }
            cache.InsertOrUpdate("test", new TestCacheItem(10, 1));
            Assert.IsNull(cache.Lookup("test", 1ul));
            Assert.IsNull(cache.Lookup("test", 2ul));
            Assert.IsNotNull(cache.Lookup("test", 3ul));
            Assert.IsNotNull(cache.Lookup("test", 4ul));
        }

        [Test]
        public void TestOverwriteOnInsert()
        {
            var cache = new LruPageCache(10);
            for (int i = 1; i < 10; i++)
            {
                cache.InsertOrUpdate("test", new TestCacheItem((ulong)i, 1));
            }
            for (int i = 1; i < 10; i++)
            {
                Assert.IsNotNull(cache.Lookup("test", (ulong)i));
            }
            cache.InsertOrUpdate("test", new TestCacheItem(3, 2));
            // Insert should not have caused eviction of these items
            Assert.IsNotNull(cache.Lookup("test", 1ul));
            Assert.IsNotNull(cache.Lookup("test", 2ul));

            // Item with key 3 should have been udpated
            var retreived = cache.Lookup("test", 3ul) as TestCacheItem;
            Assert.IsNotNull(retreived);
            Assert.AreEqual(2, retreived.Version);
        }

        [Test]
        public void TestCancelEviction()
        {
            var cache = new LruPageCache(10);
            cache.BeforeEvict += (sender, args) => { args.CancelEviction = args.PageId < 3; };

            for (int i = 1; i < 10; i++)
            {
                cache.InsertOrUpdate("test", new TestCacheItem((ulong)i, 1));
            }
            for (int i = 1; i < 10; i++)
            {
                Assert.IsNotNull(cache.Lookup("test", (ulong)i));
            }
            cache.InsertOrUpdate("test", new TestCacheItem(10, 1));
            // Insert should not have caused eviction of these items
            Assert.IsNotNull(cache.Lookup("test", 1ul));
            Assert.IsNotNull(cache.Lookup("test", 2ul));
            // Insert should have evicted these items instead
            Assert.IsNull(cache.Lookup("test", 3ul));
            Assert.IsNull(cache.Lookup("test", 4ul));
            // And this item should remain untouched
            Assert.IsNotNull(cache.Lookup("test", 5ul));

        }
    }

    public class TestCacheItem : IPageCacheItem
    {
        public TestCacheItem(ulong id, int version = 0)
        {
            Id = id;
            Version = version;
        }

        public ulong Id { get; private set; }
        public int Version { get; private set; }
    }
}
