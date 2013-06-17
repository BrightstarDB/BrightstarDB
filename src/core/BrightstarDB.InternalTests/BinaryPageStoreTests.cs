using System;
using System.IO;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BrightstarDB.Utils;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class BinaryPageStoreTests
    {
        private readonly IPersistenceManager _pm = new FilePersistenceManager();
        private readonly byte[] _testBuffer1;
        private readonly byte[] _testBuffer2;

        public BinaryPageStoreTests()
        {
            _testBuffer1 = new byte[4088];
            _testBuffer2 = new byte[4088];
            for (ulong i = 0; i < 511; i++)
            {
                BitConverter.GetBytes(i).CopyTo(_testBuffer1, (int)i * 8);
                BitConverter.GetBytes(i*2).CopyTo(_testBuffer2, (int) i*8);
            }
        }

        [TestMethod]
        public void TestCreateAndUpdatePage()
        {
            using (var pageStore = CreateEmptyPageStore("TestCreateAndUpdatePage.dat"))
            {
                var newPage = pageStore.Create(1);
                newPage = pageStore.GetWriteablePage(1, newPage);
                newPage.SetData(_testBuffer1);
                pageStore.Commit(1, null);
                Assert.AreEqual(1ul, newPage.Id);
            }

            using (var fs = new FileStream("TestCreateAndUpdatePage.dat", FileMode.Open, FileAccess.Read))
            {
                Assert.AreEqual(8192L, fs.Length);
            }

            using (var pageStore = new BinaryFilePageStore(_pm, "TestCreateAndUpdatePage.dat", 4096, true, 1))
            {
                var page = pageStore.Retrieve(1ul, null);
                Assert.IsFalse(pageStore.IsWriteable(page));
                try
                {
                    page.SetData(_testBuffer2);
                    Assert.Fail("Expected an InvalidOperationException when attempting to write to a non-writeable page");
                }
                catch (InvalidOperationException)
                {
                    // Expected
                }

                page = pageStore.GetWriteablePage(2, page);
                Assert.IsTrue(pageStore.IsWriteable(page));
                Assert.AreEqual(0, _testBuffer1.Compare(page.Data));
                page.SetData(_testBuffer2);
                pageStore.Commit(2, null);
            }

            using (var pageStore = new BinaryFilePageStore(_pm, "TestCreateAndUpdatePage.dat", 4096, true, 2))
            {
                var page = pageStore.Retrieve(1ul, null);
                Assert.AreEqual(0, _testBuffer2.Compare(page.Data));
            }
        }

        private IPageStore CreateEmptyPageStore(string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            return new BinaryFilePageStore(_pm, fileName, 4096, false, 0);
        }
    }
}
