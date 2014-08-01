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
            IPage page;
            byte[] buffer1 = new byte[] {1, 1, 1, 1};
            byte[] buffer2 = new byte[] {2, 2, 2, 2};
            using (
                var pageStore = TestUtils.CreateEmptyPageStore("TestStoreModifiedExceptionThrown.data",
                                                               PersistenceType.Rewrite))
            {
                page = pageStore.Create(2);
                pageId = page.Id;
                page.SetData(buffer1, 0, 0, 4);
                pageStore.MarkDirty(2, pageId);
                pageStore.Commit(2, null);
            }

            using (
                var readStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                        "TestStoreModifiedExceptionThrown.data",
                                                        BPlusTreeStoreManager.PageSize, true, 2, 3))
            {
                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, false, 2, 3))
                {
                    var writePage = writeStore.Retrieve(pageId, null);
                    Assert.IsFalse(writeStore.IsWriteable(writePage));
                    writePage =  writeStore.GetWriteablePage(3, writePage);
                    writePage.SetData(buffer2, 0, 0, 4);
                    writeStore.MarkDirty(3, pageId);
                    writeStore.Commit(3, null);
                }

                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, false, 3, 4))
                {
                    var writePage = writeStore.Retrieve(pageId, null);
                    writePage = writeStore.GetWriteablePage(4, writePage);
                    writePage.SetData(buffer2, 0, 0, 4);
                    writeStore.MarkDirty(4, pageId);
                    writeStore.Commit(4, null);
                }

                page = readStore.Retrieve(1, null);
                Assert.AreEqual(1, page.Data[0]);
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
                var page = pageStore.Create(1);
                pageId = page.Id;
                page.SetData(buffer1, 0, 0, 4);
                pageStore.MarkDirty(2, pageId);
                pageStore.Commit(2, null);
            }

            var readStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                    "TestStoreModifiedExceptionThrown.data",
                                                    BPlusTreeStoreManager.PageSize, true, 2, 3);
            try
            {
        
                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, false, 2, 3))
                {
                    var writePage = writeStore.Retrieve(pageId, null);
                    writePage = writeStore.GetWriteablePage(3, writePage);
                    writePage.SetData(buffer2, 0, 0, 4);
                    writeStore.MarkDirty(3, pageId);
                    writeStore.Commit(3, null);
                }

                //var page = readStore.Retrieve(pageId, null);
                //Assert.AreEqual(1, page.Data[0]);

                using (
                    var writeStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                             "TestStoreModifiedExceptionThrown.data",
                                                             BPlusTreeStoreManager.PageSize, false, 3, 4))
                {
                    var writePage = writeStore.Retrieve(pageId, null);
                    writePage = writeStore.GetWriteablePage(4, writePage);
                    writePage.SetData(buffer3, 0, 0, 4);
                    writeStore.MarkDirty(4, pageId);
                    writeStore.Commit(4, null);
                }

                try
                {
                    readStore.Retrieve(pageId, null);
                    Assert.Fail("Expected ReadWriteStoreModifiedException to be thrown");
                }
                catch (ReadWriteStoreModifiedException)
                {
                    readStore.Close();
                    readStore.Dispose();
                    readStore = null;
                    readStore = new BinaryFilePageStore(TestUtils.PersistenceManager,
                                                        "TestStoreModifiedExceptionThrown.data",
                                                        BPlusTreeStoreManager.PageSize, true, 4, 5);
                    var page = readStore.Retrieve(pageId, null);
                    Assert.AreEqual(3, page.Data[0]);
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

