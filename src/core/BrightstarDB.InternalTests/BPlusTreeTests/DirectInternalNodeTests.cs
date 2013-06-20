using System;
using System.Linq;
using System.Text;
using BrightstarDB.Storage.BPlusTreeStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class DirectInternalNodeTests
    {
        [TestMethod]
        public void TestRightShiftFrom()
        {
            var pageStore = new MemoryPageStore(1024);
            var config = new BPlusTreeConfiguration( pageStore, 8, 8, 1024);
            var n = new InternalNode(pageStore.Create(0), BitConverter.GetBytes(50ul), 2,3, config);
            n.Insert(1, BitConverter.GetBytes(100ul), 4);
            Assert.AreEqual(50ul, BitConverter.ToUInt64(n.LeftmostKey, 0));
            Assert.AreEqual(100ul, BitConverter.ToUInt64(n.RightmostKey, 0));
        }
    }
}
