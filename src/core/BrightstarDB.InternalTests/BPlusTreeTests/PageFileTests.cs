using System;
using System.IO;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests.BPlusTreeTests
{
    [TestClass]
    public class PageFileTests
    {
        private const string PageFilePath = "readonly_store.bs";
        private static readonly byte[] TestBuffer;
        private static readonly IPersistenceManager PersistenceManager;

        static PageFileTests()
        {
#if SILVERLIGHT
            PersistenceManager = new IsolatedStoragePersistanceManager();
#else
            PersistenceManager = new FilePersistenceManager();
#endif
            TestBuffer = new byte[8192];
            for (ulong i = 0; i < 1024; i++ )
            {
                BitConverter.GetBytes(i).CopyTo(TestBuffer, (int)i*8);
            }
        }

        [TestMethod]
        public void TestCreateAppendOnlyPageStore()
        {
            if (PersistenceManager.FileExists(PageFilePath)) PersistenceManager.DeleteFile(PageFilePath);
            using (var readwritePageStore =
                                new AppendOnlyFilePageStore(PersistenceManager, PageFilePath,8192, false, false))
            {
                Assert.AreEqual(8192, readwritePageStore.PageSize);
                Assert.IsTrue(readwritePageStore.CanRead);
                Assert.IsTrue(readwritePageStore.CanWrite);

                using (var readonlyPageStore =
                    new AppendOnlyFilePageStore(PersistenceManager, PageFilePath, 8192, true, false))
                {
                    Assert.AreEqual(8192, readonlyPageStore.PageSize);
                    Assert.IsTrue(readonlyPageStore.CanRead);
                    Assert.IsFalse(readonlyPageStore.CanWrite);
                }
            }

            Assert.IsTrue(PersistenceManager.FileExists(PageFilePath));

            Assert.AreEqual(0, PersistenceManager.GetFileLength(PageFilePath));
        }

        [TestMethod]
        public void TestAddFirstPageToEmptyPageStore()
        {
            //if (File.Exists(PageFilePath)) File.Delete(PageFilePath);
            if (PersistenceManager.FileExists(PageFilePath)) PersistenceManager.DeleteFile(PageFilePath);
            using (var readWritePageStore = new AppendOnlyFilePageStore(PersistenceManager, PageFilePath, 8192, false, false))
            {
                var createdPage = readWritePageStore.Create(1);
                Assert.AreEqual(1ul, createdPage.Id);
                createdPage.SetData(TestBuffer);
                ValidateBuffer(readWritePageStore.Retrieve(createdPage.Id, null).Data);
                readWritePageStore.Commit(1ul, null);
            }
            using (var readPageStore = new AppendOnlyFilePageStore(PersistenceManager, PageFilePath, 8192, true, false))
            {
                var page = readPageStore.Retrieve(1ul, null);
                ValidateBuffer(page.Data);
            }
        }

        private static void ValidateBuffer(byte[] buffer)
        {
            Assert.AreEqual(8192, buffer.Length);
            for(int i = 0; i < buffer.Length;i++)
            {
                Assert.AreEqual(TestBuffer[i], buffer[i], "Mismatch in buffer at index {0}", i);
            }
        }
    }
}
