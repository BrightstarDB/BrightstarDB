using System;
using System.Threading;
using BrightstarDB.PerformanceTests.Model;

namespace BrightstarDB.PerformanceTests
{
    public class StorageStressTestBase
    {
        protected static void InsertInBatches(int numBatches, int numPerBatch, string storeName)
        {
            var connectionString = String.Format("type=embedded;storesDirectory={0};storeName={1}",
                                                 TestConfiguration.StoreLocation, storeName);
            int docNum = 0;
            for (int batchNum = 0; batchNum < numBatches; batchNum++)
            {
                using (var context = new MyEntityContext(connectionString))
                {
                    var start = DateTime.Now;
                    Console.WriteLine("Building batch #{0}", batchNum);
                    for (int i = 0; i < numPerBatch; i++, docNum++)
                    {
                        var doc = context.Articles.Create();
                        doc.Title = "Document #" + docNum;
                        doc.PublishDate = new DateTime(2000, 01, 01).AddDays(docNum%5000);
                    }
                    var end = DateTime.Now;
                    Console.WriteLine("Building batch took {0}ms", end.Subtract(start).TotalMilliseconds);
                    Console.WriteLine("Saving batch {0}...", batchNum);
                    start = DateTime.Now;
                    context.SaveChanges();
                    end = DateTime.Now;
                    Console.WriteLine("Save completed in {0}ms", end.Subtract(start).TotalMilliseconds);
                }
            }
        }
    }
}