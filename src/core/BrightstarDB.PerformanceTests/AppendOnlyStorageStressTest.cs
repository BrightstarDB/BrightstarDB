using System;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.PerformanceTests
{
    [TestFixture]
    public class AppendOnlyStorageStressTest : StorageStressTestBase
    {
        // Test insert of half a million records in different batch sizes
        [TestCase(500, 1000)]
        [TestCase(50, 10000)]
        [TestCase(5, 100000)]
        public void TestEntityFrameworkInserts(int numBatches, int numPerBatch)
        {
            var defaultPersistenceType = Configuration.PersistenceType;
            Configuration.PersistenceType = PersistenceType.AppendOnly;
            try
            {
                var storeName = String.Format("RewriteStorageStressTest.TestEntityFrameworkInserts_{0}_{1}_{2}",
                                              numBatches, numPerBatch, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                InsertInBatches(numBatches, numPerBatch, storeName);
            }
            finally
            {
                Configuration.PersistenceType = defaultPersistenceType;
            }
        }

        [TestCase(500, 1000)]
        [TestCase(50, 10000)]
        [TestCase(5, 100000)]
        public void TestSmallPageCacheEntityFrameworkInserts(int numBatches, int numPerBatch)
        {
            var defaultPersistenceType = Configuration.PersistenceType;
            Configuration.PersistenceType = PersistenceType.AppendOnly;
            Configuration.PageCacheSize = 1;
            StoreConfiguration.DefaultStoreConfiguration.DisableBackgroundWrites = true;
            
            try
            {
                var storeName = String.Format("RewriteStorageStressTest.TestSmallPageCacheEntityFrameworkInserts{0}_{1}_{2}",
                                              numBatches, numPerBatch, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                InsertInBatches(numBatches, numPerBatch, storeName);
            }
            finally
            {
                Configuration.PersistenceType = defaultPersistenceType;
            }
        }

    }
}
