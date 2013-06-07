using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if WINDOWS_PHONE
using Remotion.Linq;
using Microsoft.Silverlight.Testing;
#endif

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class BasicTreeTests
    {
        private readonly BPlusTreeConfiguration _config = new BPlusTreeConfiguration(8, 64, 4096);

        [TestMethod]
        public void TestInsertNoSplit()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestInsertNoSplit.data"))
            {
                var tree = new BPlusTree(pageStore);
                tree.Insert(0, 5ul, TestUtils.StringToByteArray("five"));
                tree.Insert(0, 3ul, TestUtils.StringToByteArray("three"));
                var valueBuffer = new byte[_config.ValueSize];
                Assert.IsTrue(tree.Search(5ul, valueBuffer, null), "Expected search for key 5 to return true");
                TestUtils.AssertBuffersEqual(TestUtils.StringToByteArray("five"), valueBuffer);
                Assert.IsTrue(tree.Search(3ul, valueBuffer, null), "Expected search for key 3 to return true");
                TestUtils.AssertBuffersEqual(TestUtils.StringToByteArray("three"), valueBuffer);
                Assert.IsFalse(tree.Search(2ul, valueBuffer, null), "Expected search for key 2 to return false");
                tree.Save(0, null);
                var root = tree.RootId;
                pageStore.Commit((ulong) DateTime.Now.Ticks, null);

                tree = new BPlusTree(pageStore, root);
                Assert.IsTrue(tree.Search(5ul, valueBuffer, null), "Expected search for key 5 to return true");
                TestUtils.AssertBuffersEqual(TestUtils.StringToByteArray("five"), valueBuffer);
                Assert.IsTrue(tree.Search(3ul, valueBuffer, null), "Expected search for key 3 to return true");
                TestUtils.AssertBuffersEqual(TestUtils.StringToByteArray("three"), valueBuffer);
                Assert.IsFalse(tree.Search(2ul, valueBuffer, null), "Expected search for key 2 to return false");
            }
        }

        [TestMethod]
        public void TestInsertSingleRootSplit()
        {
            ulong rootId;
            var buff = new byte[_config.ValueSize];
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestInsertSingleRootSplit.data"))
            {
                var tree = new BPlusTree(pageStore);
                var txnId = 0ul;
                for (int i = 0; i < _config.LeafLoadFactor; i++)
                {
                    tree.Insert(txnId, (ulong) i, BitConverter.GetBytes((ulong) i));
                }

                tree.Insert(txnId, (ulong) _config.LeafLoadFactor, BitConverter.GetBytes((ulong) _config.LeafLoadFactor));
                Assert.IsTrue(tree.Search(14, buff, null));

                // Check we can find all the values inserted so far
                for (int i = 0; i <= _config.LeafLoadFactor; i++)
                {
                    Assert.IsTrue(tree.Search((ulong) i, buff, null));
                }
                tree.Save(txnId, null);
                pageStore.Commit(txnId, null);
                rootId = tree.RootId;
            }

            using (var pageStore = TestUtils.OpenPageStore("TestInsertSingleRootSplit.data", true))
            {
                var tree = new BPlusTree(pageStore, rootId);
                for (int i = 0; i <= _config.LeafLoadFactor; i++)
                {
                    Assert.IsTrue(tree.Search((ulong) i, buff, null), "Could not find entry for key {0}", i);
                    var value = BitConverter.ToUInt64(buff, 0);
                    Assert.AreEqual((ulong) i, value);
                }
            }
        }

        [TestMethod]
        public void TestSplitRootNode()
        {
            ulong treeRootId;
            var buff = new byte[_config.ValueSize];
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestSplitRootNode.data"))
            {
                var tree = new BPlusTree(pageStore);
                var txnId = 0ul;
                for (int i = 0; i < _config.InternalBranchFactor; i++)
                {
                    for (int j = 0; j < _config.LeafLoadFactor; j++)
                    {
                        var nodeKey = (ulong) ((i*_config.LeafLoadFactor) + j);
                        tree.Insert(txnId, nodeKey, BitConverter.GetBytes(nodeKey));
                    }
                }
                treeRootId = tree.Save(0ul, null);
                Assert.IsTrue(tree.Search(14ul, buff, null));
                pageStore.Commit(0ul, null);
                pageStore.Close();
            }
            using (var pageStore = TestUtils.OpenPageStore("TestSplitRootNode.data",true))
            {
                var tree = new BPlusTree(pageStore, treeRootId);
                for (int i = 0; i < _config.InternalBranchFactor; i++)
                {
                    for (int j = 0; j < _config.LeafLoadFactor; j++)
                    {
                        var nodeKey = (ulong) ((i*_config.LeafLoadFactor) + j);
                        Assert.IsTrue(tree.Search(nodeKey, buff, null), "Could not find entry for key {0}", nodeKey);
                        var value = BitConverter.ToUInt64(buff, 0);
                        Assert.AreEqual(nodeKey, value);
                    }
                }
            }

        }

        [TestMethod]
        public void TestInsertInReverseOrder()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestInsertInReverseOrder.data"))
            {
                var tree = new BPlusTree(pageStore);
                var txnId = 0ul;
                for (int i = 100000; i > 0; i--)
                {
                    tree.Insert(txnId, (ulong) i, BitConverter.GetBytes(i));
                }

                var buff = new byte[_config.ValueSize];
                for (int i = 100000; i > 0; i--)
                {
                    Assert.IsTrue(tree.Search((ulong) i, buff, null));
                    TestUtils.AssertBuffersEqual(BitConverter.GetBytes(i), buff);
                }
            }
        }

        [TestMethod]
        public void TestInsertInRandomOrder()
        {
            ulong treeRootId;
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestInsertInRandomOrder.data"))
            {
                var tree = new BPlusTree(pageStore);
                var txnId = 0ul;
                foreach (var insertValue in TestUtils.MakeRandomInsertList(100000))
                {
                    tree.Insert(txnId, (ulong) insertValue, BitConverter.GetBytes(insertValue));
                }
                treeRootId = tree.Save(txnId, null);
                pageStore.Commit(txnId, null);
            }
            using (var pageStore = TestUtils.OpenPageStore("TestInsertInRandomOrder.data", true))
            {
                var tree = new BPlusTree(pageStore, treeRootId);
                var buff = new byte[_config.ValueSize];
                for (int i = 0; i < 100000; i++)
                {
                    Assert.IsTrue(tree.Search((ulong) i, buff, null), "Could not find key {0} in tree", i);
                    TestUtils.AssertBuffersEqual(BitConverter.GetBytes(i), buff);
                }
            }
        }



        [TestMethod]
        public void TestDeleteFromLeafRoot()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestDeleteFromLeafRoot.data"))
            {
                var tree = new BPlusTree(pageStore);
                var txnId = 0ul;
                var buff = new byte[_config.ValueSize];
                tree.Insert(txnId, 1ul, TestUtils.StringToByteArray("one"));
                tree.Insert(txnId, 2ul, TestUtils.StringToByteArray("two"));
                tree.Insert(txnId, 3ul, TestUtils.StringToByteArray("three"));

                tree.Delete(txnId, 2ul, null);
                Assert.IsTrue(tree.Search(1ul, buff, null));
                TestUtils.AssertBuffersEqual(TestUtils.StringToByteArray("one"), buff);
                Assert.IsTrue(tree.Search(3ul, buff, null));
                TestUtils.AssertBuffersEqual(TestUtils.StringToByteArray("three"), buff);
                Assert.IsFalse(tree.Search(2ul, buff, null));
            }
        }

        [TestMethod]
        public void TestBorrowLeft()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestBorrowLeft.data"))
            {
                var tree = new BPlusTree(pageStore);
                var buff = new byte[_config.ValueSize];
                var txnId = 0ul;
                for (int j = _config.LeafLoadFactor; j >= 0; j--)
                {
                    tree.Insert(txnId, (ulong) j, BitConverter.GetBytes((ulong) j));
                }

                tree.Delete(txnId, (ulong) (_config.LeafLoadFactor - 1), null);
                tree.Delete(txnId, (ulong) (_config.LeafLoadFactor - 2), null);
                    // This should force a borrow from the left node

                for (int i = 0; i <= _config.LeafLoadFactor; i++)
                {
                    Assert.IsTrue(
                        i == (_config.LeafLoadFactor - 1) ^ i == (_config.LeafLoadFactor - 2) ^
                        tree.Search((ulong) i, buff, null),
                        "Could not find entry for key {0}", i);
                }
            }
        }
    
        [TestMethod]
        public void TestBorrowRight()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestBorrowRight.data"))
            {
                var tree = new BPlusTree(pageStore);
                var buff = new byte[_config.ValueSize];
                var txnId = 0ul;
                for (int i = 0; i <= _config.LeafLoadFactor; i++)
                {
                    tree.Insert(txnId, (ulong) i, BitConverter.GetBytes((ulong) i));
                }
                Console.WriteLine("Before deletes:");
                tree.DumpStructure();
                tree.Delete(txnId, 13ul, null);
                tree.Delete(txnId, 12ul, null); // Should force a borrow frrom the right node
                Console.WriteLine("After Deletes");
                tree.DumpStructure();
                for (int i = 0; i <= _config.LeafLoadFactor; i++)
                {
                    Assert.IsTrue(i == 12 ^ i == 13 ^ tree.Search((ulong) i, buff, null),
                        "Could not find entry for key {0}", i);
                }
            }
        }

        [TestMethod]
        public void TestMergeLeft()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestMergeLeft.data"))
            {
                var tree = new BPlusTree(pageStore);
                var testBytes = new byte[] {1, 2, 3, 4};
                var buff = new byte[_config.ValueSize];
                var txnId = 0ul;
                for (int i = 0; i < _config.LeafLoadFactor*_config.InternalBranchFactor; i++)
                {
                    tree.Insert(txnId, (ulong) i, testBytes);
                }

                ulong delFrom = (ulong) (_config.LeafLoadFactor*_config.InternalBranchFactor) - 1;
                ulong delRange = ((ulong) _config.LeafLoadFactor/2) + 2;
                for (ulong i = 0; i < delRange; i++)
                {
                    tree.Delete(txnId, delFrom - i, null); // Should be enough to force a left merge
                }

                for (ulong i = 0; i < delFrom; i++)
                {
                    Assert.IsTrue((delFrom - i < delRange) ^ tree.Search(i, buff, null),
                                  "Failed to find key {0} after deletes", i);
                }
            }
        }

        [TestMethod]
        public void TestMergeRight()
        {
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestMergeRight.data"))
            {
                var tree = new BPlusTree(pageStore);
                var testBytes = new byte[] {1, 2, 3, 4};
                var buff = new byte[_config.ValueSize];
                var txnId = 0ul;
                for (int i = 0; i < _config.LeafLoadFactor*_config.InternalBranchFactor; i++)
                {
                    try
                    {
                        tree.Insert(txnId, (ulong) i, testBytes);
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail("Insert failed for key {0} with exception {1}", i, ex);
                    }

                    
                }

                var rootNode = tree.GetNode(tree.RootId, null);
                var childId = (rootNode as IInternalNode).GetChildNodeId(rootNode.RightmostKey);
                var child = tree.GetNode(childId, null) as IInternalNode;
                var childLeftmostKey = BitConverter.ToUInt64(child.LeftmostKey, 0);
                var grandchild =
                    tree.GetNode(child.GetChildNodeId(BitConverter.GetBytes(childLeftmostKey - 1)), null) as ILeafNode;
                var deleteFrom = BitConverter.ToUInt64(grandchild.LeftmostKey, 0);

                var findChildId = (rootNode as IInternalNode).GetChildNodeId(BitConverter.GetBytes(deleteFrom));
                Assert.AreEqual(child.PageId, findChildId, "Incorrect node id returned for key {0}", deleteFrom);

                tree.DumpStructure();

                for (ulong i = 0; i < 4; i++)
                {
                    var deleteKey = deleteFrom + i;
                    tree.Delete(txnId, deleteKey, null); // Should be enough to force a right merge
                    Console.WriteLine("\n\nDeleted {0}\n", deleteKey);
                    tree.DumpStructure();
                    Assert.IsTrue(tree.Search(10395ul, buff, null));
                }

                for (ulong i = 0; i < (ulong) (_config.LeafLoadFactor*_config.InternalBranchFactor); i++)
                {
                    try
                    {
                        Assert.IsTrue(tree.Search(i, buff, null) ^ (i - deleteFrom >= 0 && i - deleteFrom < 4),
                                      "Could not find key {0}. deleteFrom={1}", i, deleteFrom);
                    }
                    catch (AssertFailedException)
                    {
                        Console.WriteLine("\nFailed tree structure:\n");
                        tree.DumpStructure();
                        throw;
                    }
                }

                deleteFrom = (ulong) (_config.LeafLoadFactor*_config.InternalBranchFactor) - 5;
                for (ulong i = 0; i < 4; i++)
                {
                    var deleteKey = deleteFrom + i;
                    tree.Delete(txnId, deleteKey, null);
                }
            }
        }


        [TestMethod]
        public void TestValuelessBTree()
        {
            var insertedValues = new List<Guid>();
            ulong treeRoot;
            var txnId = 0ul;
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestValuelessBTree.data"))
            {
                var tree = new BPlusTree(pageStore, 16, 0);
                for(int i = 0; i < 1000; i++)
                {
                    var g = Guid.NewGuid();
                    tree.Insert(txnId, g.ToByteArray(), null);
                    insertedValues.Add(g);
                }
                tree.Save(0, null);
                treeRoot = tree.RootId;
                pageStore.Commit(0ul, null);
                tree.DumpStructure();
            }

            using (var pageStore = TestUtils.OpenPageStore("TestValuelessBTree.data", false))
            {
                var tree = new BPlusTree(pageStore, treeRoot, 16, 0);
                tree.DumpStructure();
                var buff = new byte[0];
                foreach(var g in insertedValues)
                {
                    Assert.IsTrue(tree.Search(g.ToByteArray(), buff, null));
                }
            }
        }

        [TestMethod]
        public void TestBatchedInserts()
        {
            var rng = new Random();
            var keyBuff = new byte[8];
            var value = new byte[64];
            ulong key = 0;
            var txnId = 0ul;
            var inserted = new HashSet<ulong>();
            inserted.Add(0); // Not using enumerable initializer because its not supported in the mobile build
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestBatchedInserts.data"))
            {
                var tree = new BPlusTree(pageStore);
                for (int i = 1; i <= 10000; i++)
                {
                    while (inserted.Contains(key))
                    {
                        rng.NextBytes(keyBuff);
                        key = BitConverter.ToUInt64(keyBuff, 0);
                    }
                    inserted.Add(key);
                    tree.Insert(txnId, key, value);
                    if (i%250 == 0)
                    {
                        tree.Save((ulong)i / 250, null);
                        pageStore.Commit((ulong)i / 250, null);
                        Assert.AreEqual(i, tree.Scan(0, ulong.MaxValue, null).Count());
                        Console.WriteLine("Dump tree @ commit after {0}", i);
                        tree.DumpStructure();
                    }
                }
            }
        }

        [TestMethod]
        public void TestInsertAndDeleteAllEntries()
        {
            var value = new byte[64];
            ulong rootPageId;
            const int keyCount = 20000;
            using(var pageStore = TestUtils.CreateEmptyPageStore("TestInsertAndDeleteAllEntries.data"))
            {
                var tree = new BPlusTree(pageStore);
                for(int i = 0; i < keyCount; i++)
                {
                    tree.Insert(0, (ulong)i, value);
                }
                rootPageId = tree.Save(0, null);
                pageStore.Commit(0, null);
            }

            using(var pageStore = TestUtils.OpenPageStore("TestInsertAndDeleteAllEntries.data", true))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                for(int i = 0; i < keyCount; i++)
                {
                    Assert.IsTrue(tree.Search((ulong)i, value, null), "Could not find key {0} after insert and save.");
                }
            }

            using (var pageStore = TestUtils.OpenPageStore("TestInsertAndDeleteAllEntries.data", false))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                Assert.IsTrue(tree.Search(3457ul, value, null), "Could not find entry 3457 before deletes");
                for (int i = 0; i < keyCount; i += 2)
                {
                    tree.Delete(1ul, (ulong)i, null);
                    Assert.IsFalse(tree.Search((ulong)i, value, null), "Still found entry for key {0} after delete", i);
                    Assert.IsTrue(tree.Search(3457ul, value, null), "Could not find entry 3457 after delete of {0}",i);
                }
                rootPageId = tree.Save(1, null);
                pageStore.Commit(1, null);
            }

            using (var pageStore = TestUtils.OpenPageStore("TestInsertAndDeleteAllEntries.data", true))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                for (int i = 1; i < keyCount; i += 2)
                {
                    Assert.IsTrue(tree.Search((ulong)i, value, null), "Could not find key {0} after deletion of even numbered keys.", i);
                    Assert.IsFalse(tree.Search((ulong)i-1, value, null), "Found key {0} after deletion of even numbered keys", i-1);
                }
            }

            using (var pageStore = TestUtils.OpenPageStore("TestInsertAndDeleteAllEntries.data", false))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                for (int i = 1; i < keyCount; i += 2)
                {
                    tree.Delete(2ul, (ulong)i, null);
                    Assert.IsFalse(tree.Search((ulong)i, value, null), "Key {0} was still found by a search after it was deleted", i);
                }
                rootPageId = tree.Save(2, null);
                pageStore.Commit(2, null);
            }

            using (var pageStore = TestUtils.OpenPageStore("TestInsertAndDeleteAllEntries.data", true))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                Console.WriteLine("After load\r\n");
                tree.DumpStructure();
                Assert.AreEqual(0, tree.Scan(null).Count(), "Expected an empty tree after deleting all odd-numbered entries");
            }
        }

        [TestMethod]
        public void TestInsertAndDeleteInReverseOrder()
        {
            const string pageStoreName = "TestInsertAndDeleteInReverseOrder.data";
            var value = new byte[64];
            ulong rootPageId;
            const int keyCount = 20000;
            using (var pageStore = TestUtils.CreateEmptyPageStore(pageStoreName))
            {
                var tree = new BPlusTree(pageStore);
                for (int i = 0; i < keyCount; i++)
                {
                    tree.Insert(0, (ulong) i, value);
                }
                rootPageId = tree.Save(0, null);
                pageStore.Commit(0, null);
            }

            using (var pageStore = TestUtils.OpenPageStore(pageStoreName, false))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                for (int i = keyCount - 1; i >= 0; i--)
                {
                    try
                    {
                        tree.Delete(1, (ulong) i, null);
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Delete threw exception on key {0}", i);
                        throw;
                    }
                }
                rootPageId = tree.Save(1, null);
                pageStore.Commit(0, null);
            }

            using (var pageStore = TestUtils.OpenPageStore(pageStoreName, false))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                Assert.AreEqual(0, tree.Scan(null).Count(), "Expected and empty tree after all deletes");
            }
        }

        [TestMethod]
        public void TestDeleteInRandomOrder()
        {
            const string pageStoreName = "DeleteInRandomOrder.data";
            var value = new byte[64];
            ulong rootPageId;
            const int keyCount = 20000;
            using (var pageStore = TestUtils.CreateEmptyPageStore(pageStoreName))
            {
                var tree = new BPlusTree(pageStore);
                for (int i = 0; i < keyCount; i++)
                {
                    tree.Insert(0, (ulong)i, value);
                }
                rootPageId = tree.Save(0, null);
                pageStore.Commit(0, null);
            }

            
            var deleteList = TestUtils.MakeRandomInsertList(20000).ToList();
            using (var pageStore = TestUtils.OpenPageStore(pageStoreName, false))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                for (int i = 0; i < deleteList.Count; i++)
                {
                    Assert.IsTrue(tree.Search((ulong)deleteList[i], value, null), "Could not find key {0} before deletion", deleteList[i]);
                    tree.Delete(1, (ulong)deleteList[i], null);
                    Assert.IsFalse(tree.Search((ulong)deleteList[i],value, null), "Search returned key {0} after it was supposed to be deleted", deleteList[i]);
                }
                rootPageId = tree.Save(1, null);
                pageStore.Commit(1, null);
            }
            using (var pageStore = TestUtils.OpenPageStore(pageStoreName, false))
            {
                var tree = new BPlusTree(pageStore, rootPageId);
                Assert.AreEqual(0, tree.Scan(null).Count(), "Expected and empty tree after all deletes");
            }
        }
    }
}
