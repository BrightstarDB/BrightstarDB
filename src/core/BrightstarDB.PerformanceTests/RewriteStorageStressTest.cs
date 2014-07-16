using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.PerformanceTests.Model;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.PerformanceTests
{
    [TestFixture]
    public class RewriteStorageStressTest
    {
        // Test insert of half a million records in different batch sizes
        [TestCase(500, 1000)]
        [TestCase(50, 10000)]
        [TestCase(5, 100000)]
        public void TestEntityFrameworkInserts(int numBatches, int numPerBatch)
        {
            var defaultPersistenceType = Configuration.PersistenceType;
            Configuration.PersistenceType = PersistenceType.Rewrite;
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
            Configuration.PersistenceType = PersistenceType.Rewrite;
            Configuration.PageCacheSize = 2;
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


        private static void InsertInBatches(int numBatches, int numPerBatch, string storeName)
        {
            var connectionString = String.Format("type=embedded;storesDirectory={0};storeName={1}",
                                                 TestConfiguration.StoreLocation, storeName);
            int docNum = 0;
            for (int batchNum = 0; batchNum < numBatches; batchNum++)
            {
                using (var context = new MyEntityContext(connectionString))
                {
                    for (int i = 0; i < numPerBatch; i++, docNum++)
                    {
                        var doc = context.Articles.Create();
                        doc.Title = "Document #" + docNum;
                        doc.PublishDate = new DateTime(2000, 01, 01).AddDays(docNum%5000);
                    }
                    Console.WriteLine("Saving batch {0}...", batchNum);
                    var start = DateTime.Now;
                    context.SaveChanges();
                    var end = DateTime.Now;
                    Console.WriteLine("Save completed in {0}ms", end.Subtract(start).TotalMilliseconds);
                }
            }
        }

    }
}
