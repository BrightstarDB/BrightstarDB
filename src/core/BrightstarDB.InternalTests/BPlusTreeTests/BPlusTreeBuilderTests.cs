using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class BPlusTreeBuilderTests
    {
        private readonly List<KeyValuePair<byte[], byte[]>> _testOrderedValues = new List<KeyValuePair<byte[], byte[]>>(20000);

        public BPlusTreeBuilderTests()
        {
            for(int i = 0; i < 20000; i++)
            {
                var valueBuffer = new byte[64];
                BitConverter.GetBytes(i).CopyTo(valueBuffer, 0);
                _testOrderedValues.Add(new KeyValuePair<byte[], byte[]>(BitConverter.GetBytes((ulong)i), valueBuffer));
            }
        }

        [TestMethod]
        public void TestBuildSingleLeafNode()
        {
            RunTest(TestBuildSingleLeafNode, "TestBuildSingleLeafNode");
        }

        [TestMethod]
        public void TestBuildSingleInternalNode()
        {
            RunTest((pageStore, pageStoreName, persistenceType)=>BuildAndScan(pageStore, pageStoreName, persistenceType, 200), "TestBuildSingleInternalNode");
        }

        [TestMethod]
        public void TestBuildFullTree()
        {
            RunTest((pageStore, pageStoreName, persistenceType)=>BuildAndScan(pageStore, pageStoreName,persistenceType, 20000), "TestBuildFullTree");
        }

        private static void RunTest(Action<IPageStore, string, PersistenceType > testAction, string baseFileName)
        {
            testAction(TestUtils.CreateEmptyPageStore(baseFileName + "_append"), baseFileName + "_append", PersistenceType.AppendOnly);
            testAction(TestUtils.CreateEmptyPageStore(baseFileName + "_rewrite", PersistenceType.Rewrite), baseFileName + "_rewrite", PersistenceType.Rewrite);
        }

        private void TestBuildSingleLeafNode(IPageStore pageStore, string pageStoreName, PersistenceType persistenceType)
        {
            BuildAndScan(pageStore, pageStoreName, persistenceType, 20);
        }

        private void BuildAndScan(IPageStore pageStore, string pageStoreName, PersistenceType persistenceType, int keyCount)
        {
            var config = new BPlusTreeConfiguration(8, 64, pageStore.PageSize);
            var builder = new BPlusTreeBuilder(pageStore, config);
            var treeRoot = builder.Build(1, _testOrderedValues.Take(keyCount));
            var treeBeforeSave = new BPlusTree(pageStore, treeRoot);
            treeBeforeSave.Save(1, null);
            //ValidateScan(treeBeforeSave.Scan(null), keyCount, pageStoreName + " before save");
            pageStore.Commit(1, null);
            pageStore.Close();
            
           
            using(var ps = TestUtils.OpenPageStore(pageStoreName, true, persistenceType, 1))
            {
                var tree = new BPlusTree(ps, treeRoot);
                tree.DumpStructure();
                ValidateScan(tree.Scan(null), keyCount, pageStoreName + " after save");
                ValidateSearch(tree, keyCount, pageStoreName + "after save");
            }
        }

        private static void ValidateScan(IEnumerable<KeyValuePair<byte [], byte []>> scan, int keyCount, string pageStoreName)
        {
            int ix = 0;
            foreach (var keyValuePair in scan)
            {
                Assert.AreEqual((ulong) ix, BitConverter.ToUInt64(keyValuePair.Key, 0), "Unexpected key at index {0} in test store {1}", ix, pageStoreName);
                Assert.AreEqual(ix, BitConverter.ToInt32(keyValuePair.Value, 0), "Unexpected value at index {0} in test store {1}", ix, pageStoreName);
                ix++;
                Assert.IsTrue(ix <= keyCount, "Scan returned more entries than expected in test store {0}", pageStoreName);
            }
            Assert.AreEqual(keyCount, ix, "Scan returned unexpected number of entries in test store {0}",pageStoreName);
        }

        private static void ValidateSearch(BPlusTree tree, int keyCount, string pageStoreName)
        {
            var value = new byte[64];
            for (int key = 0; key < keyCount; key++)
            {
                Assert.IsTrue(tree.Search((ulong) key, value, null), "Could not find entry for key {0} in store {1}", key, pageStoreName);
                Assert.AreEqual(key, BitConverter.ToInt32(value, 0), "Incorrect value found for key {0} in store {1}", key, pageStoreName);
            }
        }
    }
}
