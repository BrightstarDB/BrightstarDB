#if BTREESTORE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BTreeStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{

    [TestClass]
    public class BTreeTests
    {
        private readonly IStoreManager _storeManager = StoreManagerFactory.GetStoreManager();

        private class IntData : IStorable
        {
            public int Value { get; set; }

            public int Save(BinaryWriter dataStream, ulong offset)
            {
                return 0;
            }

            public void Read(BinaryReader dataStream)
            {
                
            }
        }

        [TestMethod]
        public void TestFirstInsert()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(2, 3, store);
            btree.Insert(1, null);

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(1ul, btree.Root.Keys[0].Key);
        }

        [TestMethod]
        public void TestSecondInsert()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(2, 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(2, btree.Root.Keys.Count);
            Assert.AreEqual(1ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(2ul, btree.Root.Keys[1].Key);
        }

        [TestMethod]
        public void TestRootSplit()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);

            AssertOrderInList(InOrderTraversal<IntData>(btree));

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);

            Assert.AreEqual(2, btree.Root.ChildNodes.Count);
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);
        }

        [TestMethod]
        [ExpectedException(typeof(BrightstarInternalException), "Key Already Exists")]
        public void TestDuplicateInsertFails()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);

            AssertOrderInList(InOrderTraversal(btree));

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);

            Assert.AreEqual(2, btree.Root.ChildNodes.Count);
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);

            btree.Insert(3, null);
        }


        [TestMethod]
        public void TestInsertAfterRootSplit()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(4, null);

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);

            // root has two child nodes
            Assert.AreEqual(2, btree.Root.ChildNodes.Count);

            // left child has 1 key
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[0]).Keys.Count);

            // right child has 2 keys
            Assert.AreEqual(2, btree.LoadNode(btree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);
            Assert.AreEqual(4ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[1].Key);
        }

        [TestMethod]
        public void TestEntryLookup()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(4, null);

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);

            Entry<IntData> entry = null;
            Node<IntData> node = null;

            btree.LookupEntry(3, out entry, out node);

            Assert.IsNotNull(entry);
            Assert.IsNotNull(node);
        }

        [TestMethod]
        public void TestNonRootSplit()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(4, null);
            btree.Insert(5, null);

            AssertOrderInList(InOrderTraversal(btree));

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(2, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(4ul, btree.Root.Keys[1].Key);

            // root has three child nodes
            Assert.AreEqual(3, btree.Root.ChildNodes.Count);

            // root has 2 keys
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(4ul, btree.Root.Keys[1].Key);

            // left child has 1 key
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[0]).Keys.Count);

            // middle child has 1 keys
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);

            // right child has 1 keys
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[2]).Keys.Count);
            Assert.AreEqual(5ul, btree.LoadNode(btree.Root.ChildNodes[2]).Keys[0].Key);

            // check in order traversal
            var inorderKeys = InOrderTraversal(btree);
            Assert.AreEqual(1ul, inorderKeys.First().Key);
            Assert.AreEqual(5ul, inorderKeys.Last().Key);

        }

        [TestMethod]
        public void TestInsertAfterNonRootSplit()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(4, null);
            btree.Insert(5, null);
            btree.Insert(6, null);

            AssertOrderInList(InOrderTraversal(btree));

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(2, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(4ul, btree.Root.Keys[1].Key);

            // root has three child nodes
            Assert.AreEqual(3, btree.Root.ChildNodes.Count);

            // root has 2 keys
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(4ul, btree.Root.Keys[1].Key);

            // left child has 1 key
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[0]).Keys.Count);

            // middle child has 1 keys
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);

            // right child has 1 keys
            Assert.AreEqual(2, btree.LoadNode(btree.Root.ChildNodes[2]).Keys.Count);
            Assert.AreEqual(5ul, btree.LoadNode(btree.Root.ChildNodes[2]).Keys[0].Key);

        }

        [TestMethod]
        public void TestPropagatingSplit()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(4, null);
            btree.Insert(5, null);
            btree.Insert(6, null);
            btree.Insert(7, null);

            AssertOrderInList(InOrderTraversal(btree));

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(4ul, btree.Root.Keys[0].Key);

            // root has two child nodes
            Assert.AreEqual(2, btree.Root.ChildNodes.Count);

            // 1st root child has 1 key
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[0]).Keys.Count);
            Assert.AreEqual(2ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);

            // 2nd root child has 1 keys
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual(6ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);

        }

        [TestMethod]
        public void TestNonSequentialInsert()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(3, null);
            btree.Insert(10, null);
            btree.Insert(2, null);
            btree.Insert(7, null);
            btree.Insert(5, null);
            btree.Insert(4, null);
            btree.Insert(1, null);

            // check in order traversal
            var inorderKeys = InOrderTraversal(btree);
            Assert.AreEqual(1ul, inorderKeys.First().Key);
            Assert.AreEqual(10ul, inorderKeys.Last().Key);

            AssertOrderInList(inorderKeys);
        }

        [TestMethod]
        public void TestFindExistingKey()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 5, store);
            btree.Insert(3, null);
            btree.Insert(10, null);
            btree.Insert(2, null);
            btree.Insert(7, null);
            btree.Insert(5, null);
            btree.Insert(4, null);
            btree.Insert(1, null);
            btree.Insert(12, null);
            btree.Insert(19, null);
            btree.Insert(11, null);

            Assert.AreEqual(10, InOrderTraversal(btree).Count);

            foreach (var key in InOrderTraversal(btree))
            {
                Node<IntData> keyNode = btree.Lookup(key);
                Assert.IsNotNull(keyNode);
                Assert.IsTrue(keyNode.Keys.Contains(key));                
            }
        }

        [TestMethod]
        public void TestFindNonExistantKey()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(3, null);
            btree.Insert(10, null);
            btree.Insert(2, null);
            btree.Insert(7, null);
            btree.Insert(5, null);
            btree.Insert(4, null);
            btree.Insert(1, null);

            var keyNode = btree.Lookup(100);
            Assert.IsNull(keyNode);

        }

        [TestMethod]
        public void TestDeleteInLeaf()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(3, null);
            btree.Insert(4, null);
            btree.Insert(2, null);
            btree.Insert(5, null);

            btree.Delete(2);

            Assert.IsNull(btree.Lookup(2));
            AssertOrderInList(InOrderTraversal(btree));
        }

        [TestMethod]
        public void TestDeleteInLeafWithRightSiblingBorrow()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(4, null);

            Assert.IsNotNull(btree.Lookup(1));

            btree.Delete(1);

            Assert.IsNull(btree.Lookup(1));
            AssertOrderInList(InOrderTraversal(btree));

            Assert.AreEqual(3ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(2ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(4ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);
        }

        [TestMethod]
        public void TestDeleteInLeafWithLeftSiblingBorrow()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(1, null);
            btree.Insert(3, null);
            btree.Insert(5, null);
            btree.Insert(2, null);

            Assert.AreEqual(0, btree.LoadNode(btree.Root.ChildNodes[0]).ChildNodes.Count);
            Assert.AreEqual(0, btree.LoadNode(btree.Root.ChildNodes[1]).ChildNodes.Count);

            Assert.IsNotNull(btree.Lookup(5));

            btree.Delete(5);

            Assert.IsNull(btree.Lookup(5));
            AssertOrderInList(InOrderTraversal(btree));

            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);
        }

        [TestMethod]
        public void TestOutOfOrderInsert()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 3, store);
            btree.Insert(6, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(1, null);
            btree.Insert(4, null);
            btree.Insert(18, null);
            btree.Insert(5, null);
            btree.Insert(7, null);
            btree.Insert(8, null);
            btree.Insert(9, null);
            btree.Insert(10, null);
            btree.Insert(11, null);
            btree.Insert(14, null);
            btree.Insert(15, null);
            btree.Insert(16, null);
            btree.Insert(17, null);
            btree.Insert(19, null);
            btree.Insert(12, null);
            btree.Insert(13, null);
            btree.Insert(20, null);

            AssertOrderInList(InOrderTraversal(btree));
            Assert.AreEqual(20, InOrderTraversal(btree).Count);
        }

        [TestMethod]
        public void TestBuildTestTree()
        {
            var tree = BuildTree(Guid.NewGuid());
            Assert.AreEqual(20, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));
        }

        [TestMethod]
        public void TestLeafNodeDelete()
        {
            var tree = BuildTree(Guid.NewGuid());
            tree.Delete('h');

            Assert.AreEqual(19, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('k', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[0]).ChildNodes[2]).Keys[0].Key);
            Assert.AreEqual(2, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[0]).ChildNodes[2]).Keys.Count);
        }

        [TestMethod]
        public void TestInternalNodeDelete()
        {
            var tree = BuildTree(Guid.NewGuid());
            tree.Delete('t');

            Assert.AreEqual(19, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('w', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('x', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);
        }

        [TestMethod]
        public void TestDeleteLeafNodeWithBorrowRight()
        {
            var tree = BuildTree(Guid.NewGuid());
            tree.Delete('t');

            Assert.AreEqual(19, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('w', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('x', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);

            // now delete r which causes a borrow from x,y,z
            tree.Delete('r');
            Assert.AreEqual(18, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            // check x has moved up
            Assert.AreEqual('x', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes.Count);

            Assert.AreEqual(2, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('y', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);
            Assert.AreEqual('z', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[1].Key);
        }

        [TestMethod]
        public void TestDeleteInternalNodeWithBorrowLeft()
        {
            var tree = BuildTree3(Guid.NewGuid());

            AssertOrderInList(InOrderTraversal(tree));

            tree.Delete('r');

            Assert.AreEqual(19, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));
        }


        [TestMethod]
        public void TestDeleteLeafNodeWithCollapse()
        {
            var tree = BuildTree(Guid.NewGuid());
            tree.Delete('t');

            Assert.AreEqual(19, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('w', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('x', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);

            // now delete r which causes a borrow from x,y,z
            tree.Delete('r');
            Assert.AreEqual(18, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            // check x has moved up
            Assert.AreEqual('x', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes.Count);

            Assert.AreEqual(2, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('y', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);
            Assert.AreEqual('z', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[1].Key);

            tree.Delete('h');
            AssertOrderInList(InOrderTraversal(tree));

            tree.Delete('e');
            Assert.AreEqual(4, tree.Root.Keys.Count);            

        }

        [TestMethod]
        public void TestLeftMerge()
        {
            var tree = BuildTree(Guid.NewGuid());
            tree.Delete('h');
            tree.Delete('t');
            tree.Delete('r');
            tree.Delete('p');

            var entries = InOrderTraversal(tree);
            foreach (var entry in entries)
            {
                Console.WriteLine(entry.Key);
            }

        }

        private PersistentBTree<ObjectRef> BuildTree(Guid storeId)
        {
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var tree = store.MakeNewTree<ObjectRef>(5);

            var root = tree.MakeNewNode();
            root.Keys.Add(new Entry<ObjectRef>('m', null));

            var rootLeft = tree.MakeNewNode(root.NodeId);
            root.ChildNodes.Add(rootLeft.NodeId);

            var rootRight = tree.MakeNewNode(root.NodeId);
            root.ChildNodes.Add(rootRight.NodeId);

            // do root left
            rootLeft.Keys.Add(new Entry<ObjectRef>('d', null));
            rootLeft.Keys.Add(new Entry<ObjectRef>('g', null));

            var rootLeft1 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft1.Keys.Add(new Entry<ObjectRef>('a', null));
            rootLeft1.Keys.Add(new Entry<ObjectRef>('c', null));

            var rootLeft2 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft2.Keys.Add(new Entry<ObjectRef>('e', null));
            rootLeft2.Keys.Add(new Entry<ObjectRef>('f', null));

            var rootLeft3 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft3.Keys.Add(new Entry<ObjectRef>('h', null));
            rootLeft3.Keys.Add(new Entry<ObjectRef>('k', null));
            rootLeft3.Keys.Add(new Entry<ObjectRef>('l', null));

            rootLeft.ChildNodes.Add(rootLeft1.NodeId);
            rootLeft.ChildNodes.Add(rootLeft2.NodeId);
            rootLeft.ChildNodes.Add(rootLeft3.NodeId);

            // do root right
            rootRight.Keys.Add(new Entry<ObjectRef>('q', null));
            rootRight.Keys.Add(new Entry<ObjectRef>('t', null));

            var rootRight1 = tree.MakeNewNode(rootRight.NodeId);
            rootRight1.Keys.Add(new Entry<ObjectRef>('n', null));
            rootRight1.Keys.Add(new Entry<ObjectRef>('p', null));

            var rootRight2 = tree.MakeNewNode(rootRight.NodeId);
            rootRight2.Keys.Add(new Entry<ObjectRef>('r', null));
            rootRight2.Keys.Add(new Entry<ObjectRef>('s', null));

            var rootRight3 = tree.MakeNewNode(rootRight.NodeId);
            rootRight3.Keys.Add(new Entry<ObjectRef>('w', null));
            rootRight3.Keys.Add(new Entry<ObjectRef>('x', null));
            rootRight3.Keys.Add(new Entry<ObjectRef>('y', null));
            rootRight3.Keys.Add(new Entry<ObjectRef>('z', null));

            rootRight.ChildNodes.Add(rootRight1.NodeId);
            rootRight.ChildNodes.Add(rootRight2.NodeId);
            rootRight.ChildNodes.Add(rootRight3.NodeId);

            tree.Root = root;
            return tree;
        }
        
        [TestMethod]
        public void TestBuildTree2()
        {            
            var tree = BuildTree2(Guid.NewGuid());
            AssertOrderInList(InOrderTraversal(tree));
        }

        [TestMethod]
        public void TestDeleteFromTree2()
        {
            var tree = BuildTree2(Guid.NewGuid());
            tree.Delete('c');
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('m', tree.Root.Keys[0].Key);
            Assert.AreEqual(4, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[0]).ChildNodes[0]).Keys.Count);
            Assert.AreEqual('j', tree.LoadNode(tree.Root.ChildNodes[0]).Keys[1].Key);

            Assert.AreEqual(2, tree.LoadNode(tree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual('u', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
        }

        private PersistentBTree<ObjectRef> BuildTree2(Guid storeId)
        {
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var tree = store.MakeNewTree<ObjectRef>(5);

            // var tree = new PersistentBTree(2, 5, store);

            var root = tree.MakeNewNode();
            root.Keys.Add(new Entry<ObjectRef>('j', null));

            var rootLeft = tree.MakeNewNode(root.NodeId);
            root.ChildNodes.Add(rootLeft.NodeId);

            var rootRight = tree.MakeNewNode(root.NodeId);
            root.ChildNodes.Add(rootRight.NodeId);

            // do root left
            rootLeft.Keys.Add(new Entry<ObjectRef>('c', null));
            rootLeft.Keys.Add(new Entry<ObjectRef>('f', null));

            var rootLeft1 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft1.Keys.Add(new Entry<ObjectRef>('a', null));
            rootLeft1.Keys.Add(new Entry<ObjectRef>('b', null));

            var rootLeft2 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft2.Keys.Add(new Entry<ObjectRef>('d', null));
            rootLeft2.Keys.Add(new Entry<ObjectRef>('e', null));

            var rootLeft3 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft3.Keys.Add(new Entry<ObjectRef>('g', null));
            rootLeft3.Keys.Add(new Entry<ObjectRef>('i', null));

            rootLeft.ChildNodes.Add(rootLeft1.NodeId);
            rootLeft.ChildNodes.Add(rootLeft2.NodeId);
            rootLeft.ChildNodes.Add(rootLeft3.NodeId);

            // do root right
            rootRight.Keys.Add(new Entry<ObjectRef>('m', null));
            rootRight.Keys.Add(new Entry<ObjectRef>('r', null));
            rootRight.Keys.Add(new Entry<ObjectRef>('u', null));

            var rootRight1 = tree.MakeNewNode(rootRight.NodeId);
            rootRight1.Keys.Add(new Entry<ObjectRef>('k', null));
            rootRight1.Keys.Add(new Entry<ObjectRef>('l', null));

            var rootRight2 = tree.MakeNewNode(rootRight.NodeId);
            rootRight2.Keys.Add(new Entry<ObjectRef>('n', null));
            rootRight2.Keys.Add(new Entry<ObjectRef>('p', null));

            var rootRight3 = tree.MakeNewNode(rootRight.NodeId);
            rootRight3.Keys.Add(new Entry<ObjectRef>('s', null));
            rootRight3.Keys.Add(new Entry<ObjectRef>('t', null));

            var rootRight4 = tree.MakeNewNode(rootRight.NodeId);
            rootRight4.Keys.Add(new Entry<ObjectRef>('x', null));
            rootRight4.Keys.Add(new Entry<ObjectRef>('z', null));

            rootRight.ChildNodes.Add(rootRight1.NodeId);
            rootRight.ChildNodes.Add(rootRight2.NodeId);
            rootRight.ChildNodes.Add(rootRight3.NodeId);
            rootRight.ChildNodes.Add(rootRight4.NodeId);

            tree.Root = root;
            return tree;
        }

        private PersistentBTree<IntData> BuildTree3(Guid storeId)
        {
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var tree = store.MakeNewTree<IntData>(5);

            // var tree = new PersistentBTree(2, 5, store);

            var root = tree.MakeNewNode();
            root.Keys.Add(new Entry<IntData>('m', null));

            var rootLeft = tree.MakeNewNode(root.NodeId);
            root.ChildNodes.Add(rootLeft.NodeId);

            var rootRight = tree.MakeNewNode(root.NodeId);
            root.ChildNodes.Add(rootRight.NodeId);

            // do root left
            rootLeft.Keys.Add(new Entry<IntData>('c', null));
            rootLeft.Keys.Add(new Entry<IntData>('f', null));
            rootLeft.Keys.Add(new Entry<IntData>('j', null));

            var rootLeft1 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft1.Keys.Add(new Entry<IntData>('a', null));
            rootLeft1.Keys.Add(new Entry<IntData>('b', null));

            var rootLeft2 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft2.Keys.Add(new Entry<IntData>('d', null));
            rootLeft2.Keys.Add(new Entry<IntData>('e', null));

            var rootLeft3 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft3.Keys.Add(new Entry<IntData>('g', null));
            rootLeft3.Keys.Add(new Entry<IntData>('i', null));

            var rootLeft4 = tree.MakeNewNode(rootLeft.NodeId);
            rootLeft4.Keys.Add(new Entry<IntData>('k', null));
            rootLeft4.Keys.Add(new Entry<IntData>('l', null));

            rootLeft.ChildNodes.Add(rootLeft1.NodeId);
            rootLeft.ChildNodes.Add(rootLeft2.NodeId);
            rootLeft.ChildNodes.Add(rootLeft3.NodeId);
            rootLeft.ChildNodes.Add(rootLeft4.NodeId);

            // do root right
            //rootRight._keys.Add(new Entry('m', null));
            rootRight.Keys.Add(new Entry<IntData>('r', null));
            rootRight.Keys.Add(new Entry<IntData>('u', null));

            var rootRight2 = tree.MakeNewNode(rootRight.NodeId);
            rootRight2.Keys.Add(new Entry<IntData>('n', null));
            rootRight2.Keys.Add(new Entry<IntData>('p', null));

            var rootRight3 = tree.MakeNewNode(rootRight.NodeId);
            rootRight3.Keys.Add(new Entry<IntData>('s', null));
            rootRight3.Keys.Add(new Entry<IntData>('t', null));

            var rootRight4 = tree.MakeNewNode(rootRight.NodeId);
            rootRight4.Keys.Add(new Entry<IntData>('x', null));
            rootRight4.Keys.Add(new Entry<IntData>('z', null));

            rootRight.ChildNodes.Add(rootRight2.NodeId);
            rootRight.ChildNodes.Add(rootRight3.NodeId);
            rootRight.ChildNodes.Add(rootRight4.NodeId);

            tree.Root = root;
            return tree;
        }


        [TestMethod]
        [ExpectedException(typeof(BrightstarInternalException))]
        public void TestExceptionWhenInsertingDuplicateKey()
        {
            var store = new Store();
            var btree = new PersistentBTree<IntData>(2, 3, store);
            btree.Insert(1, null);
            btree.Insert(1, null);
        }

        private static void AssertOrderInList<T>(List<Entry<T>> keys) where T : class, IStorable
        {
            for (int i=0;i<keys.Count-1;i++)
            {
                Assert.IsTrue(keys[i].Key < keys[i+1].Key);
            }            
        }

        private static List<Entry<T>> InOrderTraversal<T>(PersistentBTree<T> tree) where T : class, IStorable
        {
            var keys = new List<Entry<T>>();
            TraverseNode(tree, tree.Root, keys);
            return keys;
        }

        private static void TraverseNode<T>(PersistentBTree<T> tree, Node<T> node, List<Entry<T>> keys) where T : class, IStorable
        {
            if (node.ChildNodes.Count ==  0)
            {
                keys.AddRange(node.Keys);
                return;
            }

            for (int i = 0; i < node.Keys.Count; i++)
            {
                TraverseNode(tree, tree.LoadNode(node.ChildNodes[i]), keys);
                keys.Add(node.Keys[i]);
            }

            TraverseNode(tree, tree.LoadNode(node.ChildNodes.Last()), keys);
        }


#if !SILVERLIGHT // WP7 emulator dies with these tests, presumably due to memory limitations.
        [TestMethod]
        public void TestMillionIds()
        {
            var start = DateTime.UtcNow;
            var store = new Store();
            var btree = new PersistentBTree<IntData>(store.GetNextObjectId(), 401, store);
            for (ulong i = 0; i < 1000000; i++)
            {
                btree.Insert(i, null);                
            }
            var end = DateTime.UtcNow;
            Console.WriteLine(end.Subtract(start).TotalMilliseconds);
        }
        
        [TestMethod]
        public void TestMillionIdsPersisted()
        {
            var start = DateTime.UtcNow;
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<ObjectRef>(201);
            var btreeId = btree.ObjectId;

            for (ulong i = 0; i < 1000000; i++)
            {
                btree.Insert(i, null);
            }

            var end = DateTime.UtcNow;
            Console.WriteLine("Created Tree in " + end.Subtract(start).TotalMilliseconds);

            // do commit
            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Persisted Tree in " + end.Subtract(start).TotalMilliseconds);

            // load tree again
            start = DateTime.UtcNow;
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);
            AssertOrderInList(InOrderTraversal(btree));
            end = DateTime.UtcNow;
            Console.WriteLine("Tree Scan in " + end.Subtract(start).TotalMilliseconds);

            // test insert in big tree
            start = DateTime.UtcNow;
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);
            end = DateTime.UtcNow;
            Console.WriteLine("Read tree root:" + end.Subtract(start).TotalMilliseconds);

            start = DateTime.UtcNow;
            btree.Insert(1500000, null);
            end = DateTime.UtcNow;
            Console.WriteLine("Insert into big tree:" + end.Subtract(start).TotalMilliseconds);

            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Commit new changes:" + end.Subtract(start).TotalMilliseconds);
        }
#else
        [TestMethod]
        public void TestMillionIds()
        {
            var start = DateTime.UtcNow;
            var store = new Store();
            var btree = new PersistentBTree<IntData>(2, 51, store);
            for (ulong i = 0; i < 500000; i++)
            {
                btree.Insert(i, null);
            }
            var end = DateTime.UtcNow;
            Console.WriteLine(end.Subtract(start).TotalMilliseconds);
        }

        [TestMethod]
        public void TestManyBTreeInsertsPersisted()
        {
            var start = DateTime.UtcNow;
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<Bucket>(51);
            var btreeId = btree.ObjectId;

            for (ulong i = 0; i < 500000; i++)
            {
                btree.Insert(i, null);
            }

            var end = DateTime.UtcNow;
            Console.WriteLine("Created Tree in " + end.Subtract(start).TotalMilliseconds);

            // do commit
            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Persisted Tree in " + end.Subtract(start).TotalMilliseconds);

            // load tree again
            start = DateTime.UtcNow;
            store = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<Bucket>>(btreeId);
            AssertOrderInList(InOrderTraversal(btree));
            end = DateTime.UtcNow;
            Console.WriteLine("Tree Scan in " + end.Subtract(start).TotalMilliseconds);

            // test insert in big tree
            start = DateTime.UtcNow;
            store = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<Bucket>>(btreeId);
            end = DateTime.UtcNow;
            Console.WriteLine("Read tree root:" + end.Subtract(start).TotalMilliseconds);

            start = DateTime.UtcNow;
            btree.Insert(1500000, null);
            end = DateTime.UtcNow;
            Console.WriteLine("Insert into big tree:" + end.Subtract(start).TotalMilliseconds);

            start = DateTime.UtcNow;
            store.Commit(Guid.Empty);
            end = DateTime.UtcNow;
            Console.WriteLine("Commit new changes:" + end.Subtract(start).TotalMilliseconds);
        }

#endif

        [TestMethod]
        public void TestFirstInsertPersisted()
        {
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<ObjectRef>(3);
            var btreeId = btree.ObjectId;

            btree.Insert(1, null);

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(1ul, btree.Root.Keys[0].Key);

            store.Commit(Guid.Empty);

            // load tree again
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);

            Assert.IsNotNull(btree);
            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(1ul, btree.Root.Keys[0].Key);
        }

        [TestMethod]
        public void TestOutOfOrderInsertPersisted()
        {
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<ObjectRef>(3);
            var btreeId = btree.ObjectId;

            btree.Insert(6, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(1, null);
            btree.Insert(4, null);
            btree.Insert(18, null);
            btree.Insert(5, null);
            btree.Insert(7, null);
            btree.Insert(8, null);
            btree.Insert(9, null);
            btree.Insert(10, null);
            btree.Insert(11, null);
            btree.Insert(14, null);
            btree.Insert(15, null);
            btree.Insert(16, null);
            btree.Insert(17, null);
            btree.Insert(19, null);
            btree.Insert(12, null);
            btree.Insert(13, null);
            btree.Insert(20, null);

            AssertOrderInList(InOrderTraversal(btree));
            Assert.AreEqual(20, InOrderTraversal(btree).Count);

            store.Commit(Guid.Empty);

            // load tree again
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);

            AssertOrderInList(InOrderTraversal(btree));
            Assert.AreEqual(20, InOrderTraversal(btree).Count);

        }

        [TestMethod]
        public void TestOutOfOrderInsertPersistedWithTreeSize7()
        {
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<ObjectRef>(7);
            var btreeId = btree.ObjectId;

            btree.Insert(6, null);
            btree.Insert(2, null);
            btree.Insert(3, null);
            btree.Insert(1, null);
            btree.Insert(4, null);
            btree.Insert(18, null);
            btree.Insert(5, null);
            btree.Insert(7, null);
            btree.Insert(8, null);
            btree.Insert(9, null);
            btree.Insert(10, null);
            btree.Insert(11, null);
            btree.Insert(14, null);
            btree.Insert(15, null);
            btree.Insert(16, null);
            btree.Insert(17, null);
            btree.Insert(19, null);
            btree.Insert(12, null);
            btree.Insert(13, null);
            btree.Insert(20, null);

            AssertOrderInList(InOrderTraversal(btree));
            Assert.AreEqual(20, InOrderTraversal(btree).Count);

            store.Commit(Guid.Empty);

            // load tree again
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);

            AssertOrderInList(InOrderTraversal(btree));
            Assert.AreEqual(20, InOrderTraversal(btree).Count);

        }

        [TestMethod]
        public void TestInsertAfterRootSplitPersisted()
        {
            var storeId = Guid.NewGuid();
            var store = _storeManager.CreateStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            var btree = store.MakeNewTree<ObjectRef>(3);
            var btreeId = btree.ObjectId;

            btree.Insert(1, null);
            btree.Insert(2, null);
            btree.Insert(3, null);

            // save tree
            store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);

            Assert.IsNotNull(btree);
            AssertOrderInList(InOrderTraversal(btree));

            // do insert and check ok.
            btree.Insert(4, null);
            AssertOrderInList(InOrderTraversal(btree));

            Assert.IsNotNull(btree.Root);
            Assert.AreEqual(1, btree.Root.Keys.Count);
            Assert.AreEqual(2ul, btree.Root.Keys[0].Key);

            // root has two child nodes
            Assert.AreEqual(2, btree.Root.ChildNodes.Count);

            // left child has 1 key
            Assert.AreEqual(1ul, btree.LoadNode(btree.Root.ChildNodes[0]).Keys[0].Key);
            Assert.AreEqual(1, btree.LoadNode(btree.Root.ChildNodes[0]).Keys.Count);

            // right child has 2 keys
            Assert.AreEqual(2, btree.LoadNode(btree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual(3ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[0].Key);
            Assert.AreEqual(4ul, btree.LoadNode(btree.Root.ChildNodes[1]).Keys[1].Key);

            store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            btree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);

            Assert.IsNotNull(btree);
            AssertOrderInList(InOrderTraversal(btree));
        }

        [TestMethod]
        public void TestDeleteFromTree2Persisted()
        {
            var storeId = Guid.NewGuid();
            var tree = BuildTree2(storeId);
            var btreeId = tree.ObjectId;

            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            var store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);

            Assert.IsNotNull(tree);
            AssertOrderInList(InOrderTraversal(tree));

            // do delete
            tree.Delete('c');
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('m', tree.Root.Keys[0].Key);
            Assert.AreEqual(4, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[0]).ChildNodes[0]).Keys.Count);
            Assert.AreEqual('j', tree.LoadNode(tree.Root.ChildNodes[0]).Keys[1].Key);

            Assert.AreEqual(2, tree.LoadNode(tree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual('u', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);

            // persist and load and check ok again
            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(btreeId);
            Assert.IsNotNull(tree);
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('m', tree.Root.Keys[0].Key);
            Assert.AreEqual(4, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[0]).ChildNodes[0]).Keys.Count);
            Assert.AreEqual('j', tree.LoadNode(tree.Root.ChildNodes[0]).Keys[1].Key);

            Assert.AreEqual(2, tree.LoadNode(tree.Root.ChildNodes[1]).Keys.Count);
            Assert.AreEqual('u', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
        }

        [TestMethod]
        public void TestDeleteLeafNodeWithCollapsePersisted()
        {
            var storeId = Guid.NewGuid();
            var tree = BuildTree(storeId);

            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            var store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(tree.ObjectId);

            Assert.IsNotNull(tree);
            AssertOrderInList(InOrderTraversal(tree));

            // delete t
            tree.Delete('t');

            Assert.AreEqual(19, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            Assert.AreEqual('w', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('x', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);

            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(tree.ObjectId);
            Assert.IsNotNull(tree);
            AssertOrderInList(InOrderTraversal(tree));

            // now delete r which causes a borrow from x,y,z
            tree.Delete('r');
            Assert.AreEqual(18, InOrderTraversal(tree).Count);
            AssertOrderInList(InOrderTraversal(tree));

            // check x has moved up
            Assert.AreEqual('x', tree.LoadNode(tree.Root.ChildNodes[1]).Keys[1].Key);
            Assert.AreEqual(3, tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes.Count);

            Assert.AreEqual(2, tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys.Count);
            Assert.AreEqual('y', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[0].Key);
            Assert.AreEqual('z', tree.LoadNode(tree.LoadNode(tree.Root.ChildNodes[1]).ChildNodes[2]).Keys[1].Key);

            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(tree.ObjectId);
            Assert.IsNotNull(tree);
            AssertOrderInList(InOrderTraversal(tree));

            tree.Delete('h');
            AssertOrderInList(InOrderTraversal(tree));

            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(tree.ObjectId);
            Assert.IsNotNull(tree);
            // AssertOrderInList(InOrderTraversal(tree));

            tree.Delete('e');
            Assert.AreEqual(4, tree.Root.Keys.Count);

            tree.Store.Commit(Guid.Empty);

            // load tree and check its OK.
            store = _storeManager.OpenStore(BrightstarDB.Configuration.StoreLocation + "\\" + storeId) as Store;
            tree = store.LoadObject<PersistentBTree<ObjectRef>>(tree.ObjectId);
            Assert.IsNotNull(tree);
            AssertOrderInList(InOrderTraversal(tree));
        }
    }
}
#endif