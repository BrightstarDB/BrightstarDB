using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class ResourceTableTests
    {
        private static readonly IPersistenceManager PersistenceManager;

        static ResourceTableTests()
        {
#if SILVERLIGHT
            PersistenceManager = new IsolatedStoragePersistanceManager();
#else
            PersistenceManager = new FilePersistenceManager();
#endif
        }
        [TestMethod]
        public void TestResourceTableInsert()
        {
            const string testString = "this is a test string";
            ulong pageId;
            byte segmentId;
            using (var pageStore = TestUtils.CreateEmptyPageStore("ResourceTableInsert"))
            {
                var resourceTable = new ResourceTable(pageStore);
                resourceTable.Insert(0, testString, out pageId, out segmentId, null);
                Assert.AreEqual((ulong)1, pageId);
                Assert.AreEqual((byte)0, segmentId);

                // Test we can retrieve it from the currently open page store
                var resource = resourceTable.GetResource(pageId, segmentId, null);
                Assert.AreEqual(testString, resource);
                pageStore.Commit(0, null);
            }

            using (var pageStore = new AppendOnlyFilePageStore(PersistenceManager, "ResourceTableInsert", 4096, true, false))
            {
                var resourceTable = new ResourceTable(pageStore);
                var resource = resourceTable.GetResource(pageId, segmentId, null);
                Assert.AreEqual(testString, resource);
            }
        }

        [TestMethod]
        public void TestInsertLongResources()
        {
            var longResourceA = new string('A', 3070);
            var longResourceB = new string('B', 3070);
            var longResourceC = new string('C', 3070);
            ulong pageA, pageB, pageC;
            byte segA, segB, segC;

            using (var pageStore = TestUtils.CreateEmptyPageStore("ResourceTableInsertLong"))
            {
                var resourceTable = new ResourceTable(pageStore);
                resourceTable.Insert(0, longResourceA, out pageA, out segA, null);
                resourceTable.Insert(0, longResourceB, out pageB, out segB, null);
                resourceTable.Insert(0, longResourceC, out pageC, out segC, null);

                // Test we can retrieve the long resources from the currently open page store
                var resource = resourceTable.GetResource(pageA, segA, null);
                Assert.AreEqual(longResourceA, resource);
                resource = resourceTable.GetResource(pageB, segB, null);
                Assert.AreEqual(longResourceB, resource);
                resource = resourceTable.GetResource(pageC, segC, null);
                Assert.AreEqual(longResourceC, resource);
                pageStore.Commit(0, null);
            }

            using (var pageStore = new AppendOnlyFilePageStore(PersistenceManager, "ResourceTableInsertLong", 4096, true, false))
            {
                var resourceTable = new ResourceTable(pageStore);
                var resource = resourceTable.GetResource(pageA, segA, null);
                Assert.AreEqual(longResourceA, resource);
                resource = resourceTable.GetResource(pageB, segB, null);
                Assert.AreEqual(longResourceB, resource);
                resource = resourceTable.GetResource(pageC, segC, null);
                Assert.AreEqual(longResourceC, resource);
            }

        }
    }
}
