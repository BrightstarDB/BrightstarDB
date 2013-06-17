using System;
using BrightstarDB.Storage.BPlusTreeStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class LeafNodeTests
    {
        [TestMethod]
        public void TestBorrowLeft()
        {
            var pageStore = new MemoryPageStore(4096);
            var config = new BPlusTreeConfiguration(pageStore, 8, 8, 4096);

            var leftNode = new LeafNode(pageStore.Create(0), 0ul, 0ul, config);
            var rightNode = new LeafNode(pageStore.Create(0), 0ul, 0ul, config);
            byte[] testBytes = new byte[]{1,2,3,4};
            leftNode.Insert(1, BitConverter.GetBytes(1ul), testBytes);
            leftNode.Insert(1, BitConverter.GetBytes(2ul), testBytes); 
            leftNode.Insert(1, BitConverter.GetBytes(3ul), testBytes); 
            leftNode.Insert(1, BitConverter.GetBytes(4ul), testBytes); 
            leftNode.Insert(1, BitConverter.GetBytes(5ul), testBytes);

            rightNode.Insert(1, BitConverter.GetBytes(10ul), testBytes);
            rightNode.Insert(1, BitConverter.GetBytes(11ul), testBytes);

            Assert.IsTrue(rightNode.RedistributeFromLeft(1, leftNode));
            Assert.IsTrue(rightNode.KeyCount == 3);
            
            TestUtils.AssertBuffersEqual(rightNode.LeftmostKey, BitConverter.GetBytes(5ul));
            Assert.IsTrue(leftNode.KeyCount == 4);
            TestUtils.AssertBuffersEqual(leftNode.RightmostKey, BitConverter.GetBytes(4ul));

            leftNode.Delete(1, BitConverter.GetBytes(1ul));
            leftNode.Delete(1, BitConverter.GetBytes(2ul));
            Assert.IsFalse(rightNode.RedistributeFromLeft(1, leftNode));
        }

        [TestMethod]
        public void TestBorrowRight()
        {
            var pageStore = new MemoryPageStore(4096);
            var config = new BPlusTreeConfiguration(pageStore, 8, 8, 4096);
            var leftNode = new LeafNode(pageStore.Create(0), 0, 0, config);
            var rightNode = new LeafNode(pageStore.Create(0), 0, 0, config);

            byte[] testBytes = new byte[] { 1, 2, 3, 4 };

            leftNode.Insert(1, BitConverter.GetBytes(1ul), testBytes);
            leftNode.Insert(1, BitConverter.GetBytes(2ul), testBytes);

            rightNode.Insert(1, BitConverter.GetBytes(10ul), testBytes);
            rightNode.Insert(1, BitConverter.GetBytes(11ul), testBytes);
            rightNode.Insert(1, BitConverter.GetBytes(12ul), testBytes);
            rightNode.Insert(1, BitConverter.GetBytes(13ul), testBytes);
            rightNode.Insert(1, BitConverter.GetBytes(14ul), testBytes);

            Assert.IsTrue(leftNode.RedistributeFromRight(1, rightNode));
            Assert.IsTrue(leftNode.KeyCount == 3);
            TestUtils.AssertBuffersEqual(BitConverter.GetBytes(10ul), leftNode.RightmostKey);
            TestUtils.AssertBuffersEqual(BitConverter.GetBytes(11ul), rightNode.LeftmostKey);
            Assert.IsTrue(rightNode.KeyCount == 4);
            Assert.IsFalse(leftNode.RedistributeFromRight(1, rightNode));
        }

        [TestMethod]
        public void TestLeafNodeSplit()
        {
            var pageStore = new MemoryPageStore(4096);
            var config = new BPlusTreeConfiguration(pageStore, 8, 8, 4096);
            var firstNode = new LeafNode(pageStore.Create(0), 0, 0, config);
            var testBytes = new byte[]{1,2,3,4,5,6,7,8};
            for (int i = 0; i < config.LeafLoadFactor; i++)
            {
                firstNode.Insert(0, BitConverter.GetBytes((ulong)i), testBytes);
            }

            var secondNodePage = pageStore.Create(0);
            var splitKey = new byte[8];
            var secondNode = firstNode.Split(0, secondNodePage, out splitKey);

            Assert.AreEqual(config.LeafSplitIndex, firstNode.KeyCount);
            Assert.AreEqual(config.LeafLoadFactor - config.LeafSplitIndex, secondNode.KeyCount);

            var firstNodePage = pageStore.Retrieve(0, null);
            var firstNodeKeyCount = BitConverter.ToInt32(firstNodePage.Data, 0);
            secondNodePage = pageStore.Retrieve(1, null);
            var secondNodeKeyCount = BitConverter.ToInt32(secondNodePage.Data, 0);
            Assert.AreEqual(config.LeafSplitIndex, firstNodeKeyCount);
            Assert.AreEqual(config.LeafLoadFactor - config.LeafSplitIndex, secondNodeKeyCount);
            
        }
    }
}
