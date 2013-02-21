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
            var config = new BPlusTreeConfiguration(8, 8, 4096);

            var leftNode = new LeafNode(1ul, 0ul, 0ul, config);
            var rightNode = new LeafNode(2ul, 0ul, 0ul, config);
            byte[] testBytes = new byte[]{1,2,3,4};
            leftNode.Insert(BitConverter.GetBytes(1ul), testBytes);
            leftNode.Insert(BitConverter.GetBytes(2ul), testBytes); 
            leftNode.Insert(BitConverter.GetBytes(3ul), testBytes); 
            leftNode.Insert(BitConverter.GetBytes(4ul), testBytes); 
            leftNode.Insert(BitConverter.GetBytes(5ul), testBytes);

            rightNode.Insert(BitConverter.GetBytes(10ul), testBytes);
            rightNode.Insert(BitConverter.GetBytes(11ul), testBytes);

            Assert.IsTrue(rightNode.RedistributeFromLeft(leftNode));
            Assert.IsTrue(rightNode.KeyCount == 3);
            
            TestUtils.AssertBuffersEqual(rightNode.LeftmostKey, BitConverter.GetBytes(5ul));
            Assert.IsTrue(leftNode.KeyCount == 4);
            TestUtils.AssertBuffersEqual(leftNode.RightmostKey, BitConverter.GetBytes(4ul));

            leftNode.Delete(BitConverter.GetBytes(1ul));
            leftNode.Delete(BitConverter.GetBytes(2ul));
            Assert.IsFalse(rightNode.RedistributeFromLeft(leftNode));
        }

        [TestMethod]
        public void TestBorrowRight()
        {
            var config = new BPlusTreeConfiguration(8, 8, 4096);
            var leftNode = new LeafNode(1, 0, 0, config);
            var rightNode = new LeafNode(2, 0, 0, config);

            byte[] testBytes = new byte[] { 1, 2, 3, 4 };

            leftNode.Insert(BitConverter.GetBytes(1ul), testBytes);
            leftNode.Insert(BitConverter.GetBytes(2ul), testBytes);

            rightNode.Insert(BitConverter.GetBytes(10ul), testBytes);
            rightNode.Insert(BitConverter.GetBytes(11ul), testBytes);
            rightNode.Insert(BitConverter.GetBytes(12ul), testBytes);
            rightNode.Insert(BitConverter.GetBytes(13ul), testBytes);
            rightNode.Insert(BitConverter.GetBytes(14ul), testBytes);

            Assert.IsTrue(leftNode.RedistributeFromRight(rightNode));
            Assert.IsTrue(leftNode.KeyCount == 3);
            TestUtils.AssertBuffersEqual(BitConverter.GetBytes(10ul), leftNode.RightmostKey);
            TestUtils.AssertBuffersEqual(BitConverter.GetBytes(11ul), rightNode.LeftmostKey);
            Assert.IsTrue(rightNode.KeyCount == 4);
            Assert.IsFalse(leftNode.RedistributeFromRight(rightNode));
        }


    }
}
