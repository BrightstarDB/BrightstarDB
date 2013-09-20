using BrightstarDB.Utils;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class GenericLruCacheTests
    {
        [Test]
        public void TestLookup()
        {
            var cache = new LruCache<int, string>(10);
            cache.InsertOrUpdate(1, "test");
            cache.InsertOrUpdate(2, "test");

            string retrieved;
            Assert.IsTrue(cache.TryLookup(1, out retrieved));
            Assert.AreEqual("test", retrieved);
            Assert.IsTrue(cache.TryLookup(2, out retrieved));
            Assert.AreEqual("test", retrieved);

            Assert.IsFalse(cache.TryLookup(3, out retrieved));
        }

        [Test]
        public void TestEvictionOnInsert()
        {
            var cache = new LruCache<int, string>(10, 9, 7);
            string retrieved;
            for (int i = 0; i < 8; i++)
            {
                cache.InsertOrUpdate(i, "test:"+i);
            }
            for (int i = 0; i < 8; i++)
            {
                Assert.IsTrue(cache.TryLookup(i, out retrieved),
                    "Could not find entry for key {0}", i);
            }
            cache.InsertOrUpdate(8, "test8");
            Assert.IsFalse(cache.TryLookup(0, out retrieved));
            Assert.IsFalse(cache.TryLookup(1, out retrieved));
            for (int i = 2; i < 9; i++)
            {
                Assert.IsTrue(cache.TryLookup(i, out retrieved),
                    "Could not find entry for key {0} after evictions", i);
            }
        }

        [Test]
        public void TestOverwriteOnInsert()
        {
            var cache = new LruCache<int, string>(10, 9, 7);
            string retrieved;
            for (int i = 0; i < 8; i++)
            {
                cache.InsertOrUpdate(i, "test" + i);
            }
            for (int i = 0; i < 8; i++)
            {
                Assert.IsNotNull(cache.TryLookup(i, out retrieved));
            }
            cache.InsertOrUpdate(3, "updated");
            // Insert should not have caused eviction of any items
            for (int i = 0; i < 8; i++)
            {
                Assert.IsNotNull(cache.TryLookup(i, out retrieved));
            }
            // Item with key 3 should have been udpated
            Assert.IsTrue(cache.TryLookup(3, out retrieved));
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("updated", retrieved);
        }
    }
}
