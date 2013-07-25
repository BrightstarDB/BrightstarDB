using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests.BPlusTreeTests
{
    [TestFixture]
    public class ReadWriteStoreModifiedTests
    {
        [Test]
        [ExpectedException(typeof (ReadWriteStoreModifiedException))]
        public void TestStoreModifiedExceptionThrown()
        {
            ulong pageId;
            byte[] buffer1 = new byte[] {1, 1, 1, 1};
            byte[] buffer2 = new byte[] {2, 2, 2, 2};
            using (
                var pageStore = TestUtils.CreateEmptyPageStore("TestStoreModifiedExceptionThrown.data",
                                                               PersistenceType.Rewrite))
            {
                pageId = pageStore.Create();
                pageStore.Write(1, pageId, buffer1, 0, 0, 4);
                pageStore.Commit(1, null);
            }

            using (
                var readStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                        "TestStoreModifiedExceptionThrown.data",
                                                        BPlusTreeStoreManager.PageSize, true, 1))
            {
                var page = readStore.Retrieve(1, null);
                Assert.AreEqual(1, page[0]);

                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, true, 1))
                {
                    writeStore.Write(2, pageId, buffer2, 0, 0, 4);
                    writeStore.Commit(2, null);
                }

                page = readStore.Retrieve(1, null);
                Assert.AreEqual(1, page[0]);

                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, true, 2))
                {
                    writeStore.Write(3, pageId, buffer2, 0, 0, 4);
                    writeStore.Commit(3, null);
                }

                page = readStore.Retrieve(1, null);
                Assert.AreEqual(1, page[0]);
                // We should not reach this assert because the ReadWriteStoreModifiedException should get thrown
            }
        }

        [Test]
        public void TestReopenAfterReadWriteStoreModifiedException()
        {
            ulong pageId;
            var buffer1 = new byte[] {1, 1, 1, 1};
            var buffer2 = new byte[] {2, 2, 2, 2};
            var buffer3 = new byte[] {3, 3, 3, 3};

            using (
                var pageStore = TestUtils.CreateEmptyPageStore("TestStoreModifiedExceptionThrown.data",
                                                               PersistenceType.Rewrite))
            {
                pageId = pageStore.Create();
                pageStore.Write(1, pageId, buffer1, 0, 0, 4);
                pageStore.Commit(1, null);
            }

            var readStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                    "TestStoreModifiedExceptionThrown.data",
                                                    BPlusTreeStoreManager.PageSize, true, 1);
            try
            {
                var page = readStore.Retrieve(1, null);
                Assert.AreEqual(1, page[0]);

                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, true, 1))
                {
                    writeStore.Write(2, pageId, buffer2, 0, 0, 4);
                    writeStore.Commit(2, null);
                }

                page = readStore.Retrieve(1, null);
                Assert.AreEqual(1, page[0]);

                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, true, 2))
                {
                    writeStore.Write(3, pageId, buffer3, 0, 0, 4);
                    writeStore.Commit(3, null);
                }

                try
                {
                    readStore.Retrieve(1, null);
                    Assert.Fail("Expected ReadWriteStoreModifiedException to be thrown");
                }
                catch (ReadWriteStoreModifiedException)
                {
                    readStore.Close();
                    readStore.Dispose();
                    readStore = null;
                    readStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                        "TestStoreModifiedExceptionThrown.data",
                                                        BPlusTreeStoreManager.PageSize, true, 3);
                    page = readStore.Retrieve(1, null);
                    Assert.AreEqual(3, page[0]);
                }
            }
            finally
            {
                if (readStore != null)
                {
                    readStore.Dispose();
                }
            }
        }
    }
}

