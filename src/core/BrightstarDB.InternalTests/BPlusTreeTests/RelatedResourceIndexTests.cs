using System.Linq;
using BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class RelatedResourceIndexTests
    {
        [TestMethod]
        public void TestInsertRelatedResource()
        {
            ulong relatedResourceIndexRoot;
            using(var pageStore = TestUtils.CreateEmptyPageStore("TestInsertRelatedResource.dat"))
            {
                var relatedResourceIndex = new RelatedResourceIndex(0, pageStore);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 3ul, 4);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 4, null).ToList();
                Assert.AreEqual(1, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                relatedResourceIndex.Save(0, null);
                relatedResourceIndexRoot = relatedResourceIndex.RootId;
                pageStore.Commit(0ul, null);
            }

            using(var pageStore = TestUtils.OpenPageStore("TestInsertRelatedResource.dat", true))
            {
                var relatedResourceIndex = new RelatedResourceIndex(pageStore, relatedResourceIndexRoot, null);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 4, null).ToList();
                Assert.AreEqual(1, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
            }
        }

        [TestMethod]
        public void TestEnumerateMultipleRelatedResources()
        {
            ulong relatedResourceIndexRoot;
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestMultipleRelatedResources.dat"))
            {
                var relatedResourceIndex = new RelatedResourceIndex(0, pageStore);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 3ul, 4);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 5ul, 4);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 6ul, 4);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 7ul, 8);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 4).ToList();
                Assert.AreEqual(3, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);

                relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul).ToList();
                Assert.AreEqual(4, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);
                Assert.AreEqual(7ul, relatedResourceIds[3].ResourceId);
                Assert.AreEqual(8, relatedResourceIds[3].GraphId);
                relatedResourceIndex.Save(0, null);
                relatedResourceIndexRoot = relatedResourceIndex.RootId;
                pageStore.Commit(0ul, null);
            }

            using (var pageStore = TestUtils.OpenPageStore("TestMultipleRelatedResources.dat", true))
            {
                var relatedResourceIndex = new RelatedResourceIndex(pageStore, relatedResourceIndexRoot, null);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1, 2, 4).ToList();
                Assert.AreEqual(3, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);

                relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1, 2).ToList();
                Assert.AreEqual(4, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);
                Assert.AreEqual(7ul, relatedResourceIds[3].ResourceId);
                Assert.AreEqual(8, relatedResourceIds[3].GraphId);
            }
        }

        [TestMethod]
        public void TestDeleteRelatedResource()
        {
            ulong relatedResourceIndexRoot;
            using (var pageStore = TestUtils.CreateEmptyPageStore("TestDeleteRelatedResource.dat"))
            {
                var relatedResourceIndex = new RelatedResourceIndex(0, pageStore);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 3ul, 4);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 5ul, 4);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 6ul, 4);
                relatedResourceIndex.AddRelatedResource(0, 1ul, 2ul, 7ul, 8);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 4).ToList();
                Assert.AreEqual(3, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);

                relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul).ToList();
                Assert.AreEqual(4, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);
                Assert.AreEqual(7ul, relatedResourceIds[3].ResourceId);
                Assert.AreEqual(8, relatedResourceIds[3].GraphId);
                relatedResourceIndex.Save(0, null);
                relatedResourceIndexRoot = relatedResourceIndex.RootId;
                pageStore.Commit(0ul, null);
            }

            using (var pageStore = TestUtils.OpenPageStore("TestDeleteRelatedResource.dat", false))
            {
                var relatedResourceIndex = new RelatedResourceIndex(pageStore, relatedResourceIndexRoot, null);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 4).ToList();
                Assert.AreEqual(3, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);

                relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul).ToList();
                Assert.AreEqual(4, relatedResourceIds.Count);
                Assert.AreEqual(3ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(5ul, relatedResourceIds[1].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[2].ResourceId);
                Assert.AreEqual(7ul, relatedResourceIds[3].ResourceId);
                Assert.AreEqual(8, relatedResourceIds[3].GraphId);

                relatedResourceIndex.DeleteRelatedResource(1, 1, 2, 3, 4, null);
                Assert.AreEqual(3, relatedResourceIndex.EnumerateRelatedResources(1,2).Count());

                relatedResourceIndex.DeleteRelatedResource(1, 1, 2, 7, 8, null);
                Assert.AreEqual(2, relatedResourceIndex.EnumerateRelatedResources(1, 2).Count());

                relatedResourceIndex.Save(1, null);
                relatedResourceIndexRoot = relatedResourceIndex.RootId;
                pageStore.Commit(1ul, null);
            }

            using (var pageStore = TestUtils.OpenPageStore("TestDeleteRelatedResource.dat", true))
            {
                var relatedResourceIndex = new RelatedResourceIndex(pageStore, relatedResourceIndexRoot, null);
                var relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 4).ToList();
                Assert.AreEqual(2, relatedResourceIds.Count);
                Assert.AreEqual(5ul, relatedResourceIds[0].ResourceId);
                Assert.AreEqual(6ul, relatedResourceIds[1].ResourceId);
                relatedResourceIds = relatedResourceIndex.EnumerateRelatedResources(1ul, 2ul, 8).ToList();
                Assert.AreEqual(0, relatedResourceIds.Count);
            }
        }
    }
}
