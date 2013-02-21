using BrightstarDB.Storage.BPlusTreeStore.GraphIndex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class GraphIndexTests
    {
        [TestMethod]
        public void TestGraphIndexCrud()
        {
            using(var pageStore = TestUtils.CreateEmptyPageStore("TestGraphIndexCrud.data"))
            {
                var graphIndex = new ConcurrentGraphIndex(pageStore);
                var graphId1 = graphIndex.AssertGraphId("http://example.org/graph/1");
                Assert.AreEqual(0, graphId1); // Test first graph gets ID 0
                var graphId2 = graphIndex.AssertGraphId("http://example.org/graph/2");
                Assert.AreEqual(1, graphId2);
                Assert.AreEqual(graphId1, graphIndex.AssertGraphId("http://example.org/graph/1"));
                int foundId;
                Assert.IsTrue(graphIndex.TryFindGraphId("http://example.org/graph/2", out foundId));
                Assert.AreEqual(graphId2, foundId);
                graphIndex.DeleteGraph(graphId1);
                Assert.IsFalse(graphIndex.TryFindGraphId("http://example.org/graph/1", out foundId));

                var graphId3 = graphIndex.AssertGraphId("http://example.org/graph/1");
                Assert.AreNotEqual(graphId1, graphId3);
            }
        }
    }
}
