using System.IO;
using System.Threading;
using BrightstarDB.Client;
using BrightstarDB.Config;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class PreloadTests
    {
        [TestFixtureSetUp]
        public void SetUp()
        {
            if (Directory.Exists("preload_test"))
            {
                Directory.Delete("preload_test", true);
            }
            Directory.CreateDirectory("preload_test");
        }

        [Test]
        public void TestPreloadStore()
        {
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=" + Path.GetFullPath("preload_test"));
            var dataPartition = Path.GetFullPath(Path.Combine("preload_test", "storeA", "data.bs"));
            client.CreateStore("storeA", PersistenceType.AppendOnly);
            Assert.That(PageCache.Instance.Lookup(dataPartition, 1ul), Is.Not.Null);
            BrightstarService.Shutdown();
            PageCache.Instance.Clear();
            Assert.That(PageCache.Instance.Lookup(dataPartition, 1ul), Is.Null);
            client = BrightstarService.GetClient("type=embedded;storesDirectory=" + Path.GetFullPath("preload_test"),
                new EmbeddedServiceConfiguration(new PageCachePreloadConfiguration
                {
                    Enabled = true,
                    DefaultCacheRatio = 1.0m
                }));
            // Preload runs in the background so give it a bit of time
            Thread.Sleep(1000);
            Assert.That(PageCache.Instance.Lookup(dataPartition, 1ul), Is.Not.Null);
        }

        [Test]
        public void TestPreloadMultipleStores()
        {
            var client = BrightstarService.GetClient("type=embedded;storesDirectory=" + Path.GetFullPath("preload_test"));
            var dataPartitionB = Path.GetFullPath(Path.Combine("preload_test", "storeB", "data.bs"));
            var dataPartitionC = Path.GetFullPath(Path.Combine("preload_test", "storeC", "data.bs"));
            client.CreateStore("storeB", PersistenceType.AppendOnly);
            client.CreateStore("storeC", PersistenceType.AppendOnly);
            Assert.That(PageCache.Instance.Lookup(dataPartitionB, 1ul), Is.Not.Null);
            Assert.That(PageCache.Instance.Lookup(dataPartitionC, 1ul), Is.Not.Null);
            BrightstarService.Shutdown();
            PageCache.Instance.Clear();
            Assert.That(PageCache.Instance.Lookup(dataPartitionB, 1ul), Is.Null);
            Assert.That(PageCache.Instance.Lookup(dataPartitionC, 1ul), Is.Null);
            client = BrightstarService.GetClient("type=embedded;storesDirectory=" + Path.GetFullPath("preload_test"),
                new EmbeddedServiceConfiguration(
                    new PageCachePreloadConfiguration {Enabled = true, DefaultCacheRatio = 1.0m}
                    ));
            // Preload runs in the background so give it a bit of time
            Thread.Sleep(1000);
            Assert.That(PageCache.Instance.Lookup(dataPartitionB, 1ul), Is.Not.Null);
            Assert.That(PageCache.Instance.Lookup(dataPartitionC, 1ul), Is.Not.Null);
            
        }
    }
}
