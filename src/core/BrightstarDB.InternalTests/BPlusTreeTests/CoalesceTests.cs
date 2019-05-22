using System;
using System.Linq;
using BrightstarDB.Storage.BPlusTreeStore;
using NUnit.Framework;

namespace BrightstarDB.InternalTests.BPlusTreeTests
{
    [TestFixture]
    public class CoalesceTests
    {
        [Test]
        public void TestCoalesceEmptyTree()
        {
            const string storeName = "Coalesce.EmptyTree.data";
            ulong srcRootId, coalescedRootId;
            using(var store = TestUtils.CreateEmptyPageStore(storeName))
            {
                var tree = new BPlusTree(0, store);
                srcRootId = tree.Save(0, null);
                store.Commit(0, null);
            }

            using(var store = TestUtils.OpenPageStore(storeName, false))
            {
                var sourceTree = new BPlusTree(store, srcRootId);
                Assert.AreEqual(0, sourceTree.Scan(null).Count());
                var builder = new BPlusTreeBuilder(store, sourceTree.Configuration);
                coalescedRootId = builder.Build(1, sourceTree.Scan(null));
                var coalescedTree = new BPlusTree(store, coalescedRootId);
                Assert.AreEqual(0, coalescedTree.Scan(null).Count());
                store.Commit(1ul, null);
            }

            using (var store = TestUtils.OpenPageStore(storeName, true))
            {
                var coalescedTree = new BPlusTree(store, coalescedRootId);
                Assert.AreEqual(0, coalescedTree.Scan(null).Count());                
            }
        }

        [Test]
        public void TestCoalesceRootLeafNode()
        {
            const string storeName = "Coalesce.RootLeafNode.data";
            ulong srcRootId, targetRootId;
            var txnId = 0ul;
            using(var store = TestUtils.CreateEmptyPageStore(storeName))
            {
                var sourceTree = new BPlusTree(0, store);
                var config = sourceTree.Configuration;
                for (int i = 0; i < config.LeafLoadFactor; i++)
                {
                    sourceTree.Insert(txnId, (ulong)i, BitConverter.GetBytes((ulong)i));
                }
                srcRootId = sourceTree.Save(txnId, null);
                store.Commit(txnId, null);
            }

            using(var store = TestUtils.OpenPageStore(storeName, false))
            {
                var sourceTree = new BPlusTree(store, srcRootId);
                var config = sourceTree.Configuration;
                var builder = new BPlusTreeBuilder(store, config);
                targetRootId = builder.Build(1, sourceTree.Scan(null));
                
                var targetTree = new BPlusTree(store, targetRootId);
                byte[] valueBuff = new byte[64];
                for(int i =0 ; i < config.LeafLoadFactor; i++)
                {
                    Assert.IsTrue(targetTree.Search((ulong)i, valueBuff, null));
                    Assert.AreEqual((ulong) i, BitConverter.ToUInt64(valueBuff, 0));
                }
                store.Commit(1, null);
            }

            using(var store = TestUtils.OpenPageStore(storeName, true))
            {
                var targetTree = new BPlusTree(store, targetRootId);
                var config = targetTree.Configuration;
                byte[] valueBuff = new byte[64];
                for (int i = 0; i < config.LeafLoadFactor; i++)
                {
                    Assert.IsTrue(targetTree.Search((ulong)i, valueBuff, null));
                    Assert.AreEqual((ulong)i, BitConverter.ToUInt64(valueBuff, 0));
                }
            }
        }

        [Test]
        public void TestCoalesceLeafLoadPlusOne()
        {
            const string storeName = "Coalesce.LeafLoadPlusOne.data";
            ulong srcRootId, targetRootId;
            var txnId = 0ul;
            using (var store = TestUtils.CreateEmptyPageStore(storeName))
            {
                var sourceTree = new BPlusTree(0, store);
                var config = sourceTree.Configuration;
                for (int i = 0; i < config.LeafLoadFactor + 1; i++)
                {
                    sourceTree.Insert(txnId, (ulong)i, BitConverter.GetBytes((ulong)i));
                }
                srcRootId = sourceTree.Save(txnId, null);
                store.Commit(txnId, null);
            }

            using (var store = TestUtils.OpenPageStore(storeName, false))
            {
                var sourceTree = new BPlusTree(store, srcRootId);
                var config = sourceTree.Configuration;
                var builder = new BPlusTreeBuilder(store, config);
                targetRootId = builder.Build(1, sourceTree.Scan(null));
                var targetTree = new BPlusTree(store, targetRootId);
                byte[] valueBuff = new byte[64];
                for (int i = 0; i < config.LeafLoadFactor + 1; i++)
                {
                    Assert.IsTrue(targetTree.Search((ulong)i, valueBuff, null));
                    Assert.AreEqual((ulong)i, BitConverter.ToUInt64(valueBuff, 0));
                }
                store.Commit(1, null);
            }

            using (var store = TestUtils.OpenPageStore(storeName, true))
            {
                var targetTree = new BPlusTree(store, targetRootId);
                var config = targetTree.Configuration;

                byte[] valueBuff = new byte[64];
                for (int i = 0; i < config.LeafLoadFactor + 1; i++)
                {
                    Assert.IsTrue(targetTree.Search((ulong)i, valueBuff, null));
                    Assert.AreEqual((ulong)i, BitConverter.ToUInt64(valueBuff, 0));
                }

                var root = targetTree.GetNode(targetTree.RootId, null) as IInternalNode;
                Assert.IsNotNull(root);
                Assert.AreEqual(1, root.KeyCount);
                var leftChild = targetTree.GetNode(root.GetChildPointer(0), null) as ILeafNode;
                var rightChild = targetTree.GetNode(root.GetChildPointer(1), null) as ILeafNode;
                Assert.IsNotNull(leftChild);
                Assert.IsNotNull(rightChild);

                Assert.AreEqual(config.LeafLoadFactor+1, leftChild.KeyCount + rightChild.KeyCount);
                // Key count in each node should be >= split index
                Assert.IsTrue(leftChild.KeyCount > config.LeafSplitIndex, "Left child has too few keys");
                Assert.IsTrue(rightChild.KeyCount > config.LeafSplitIndex, "Right child has too few keys");
                // And neither
            }
            
        }
    }
}
