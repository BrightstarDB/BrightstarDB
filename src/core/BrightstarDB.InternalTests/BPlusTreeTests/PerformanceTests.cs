using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    [Ignore]
    public class PerformanceTests
    {
        [TestMethod]
        public void MakeTestData()
        {
            int batchSize = 50000;
            var testList = TestUtils.MakeRandomInsertList(batchSize);
            WriteTestData(testList, "c:\\brightstar\\testdata.dat");
        }

        [TestMethod]
        public void TestBatchInsert()
        {
            const int target = 40000000;
            const int batchSize = 50000;
            const int batchCount = target/batchSize;
            // Tests insert of 40 million unique keys in batches of 50,000
            // Reports time for each batch to console.

#if SILVERLIGHT
            var persistenceManager = new IsolatedStoragePersistanceManager();
#else
            var persistenceManager = new FilePersistenceManager();
#endif
            // Create a test batch
            if (!File.Exists("C:\\brightstar\\testdata.dat"))
            {
                MakeTestData();
            }
            var testList = ReadTestData("c:\\brightstar\\testdata.dat");

            // Create empty store
            if (File.Exists("40m_batch.data")) File.Delete("40m_batch.data");
            var pageStore = new AppendOnlyFilePageStore(persistenceManager, "40m_batch.data", 4096, false, false);
            var tree = new BPlusTree(0, pageStore);
            ulong lastRoot = tree.RootId;
            tree.Save(0, null);
            pageStore.Commit(0ul, null);

            byte[] testBuffer = Encoding.UTF8.GetBytes("Test Buffer");
            var batchTimer = Stopwatch.StartNew();
            int insertedCount = 0;
            var txnId = 1ul;
            for (int i = 0; i < batchCount; i++)
            {
                tree = new BPlusTree(pageStore, lastRoot);
                foreach (var item in testList)
                {
                    var insertKey = (ulong) ((item*batchCount) + i);
                    try
                    {
                        tree.Insert(txnId, insertKey, testBuffer);
                        insertedCount++;
                    }
                    catch (Exception e)
                    {
                        Assert.Fail("Failed to add key {0}. Cause: {1}", insertKey, e);
                    }
                }
                long beforeSave = batchTimer.ElapsedMilliseconds;
                tree.Save(txnId, null);
                lastRoot = tree.RootId;
                pageStore.Commit(txnId, null);
                Console.WriteLine("{0},{1},{2}", insertedCount, beforeSave, batchTimer.ElapsedMilliseconds);
                txnId++;
            }
        }

        private static void WriteTestData(IEnumerable<int> data, string path)
        {
            using(var textStream = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write)))
            {
                foreach(var i in data)
                {
                    textStream.WriteLine(i);
                }
                textStream.Flush();
                textStream.Close();
            }
        }

        private static IEnumerable<int> ReadTestData(string path)
        {
            var ret = new List<int>();
            using (var textStream = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read)))
            {
                while (!textStream.EndOfStream)
                {
                    var line = textStream.ReadLine();
                    var value = Int32.Parse(line);
                    ret.Add(value);
                }
            }
            return ret;
        }
    }
}
