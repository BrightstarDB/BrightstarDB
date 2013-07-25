using System;
using System.IO;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;
using BrightstarDB.Utils;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
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

        [Test]
        public void TestCreateAndUpdatePage()
        {
            ulong newPage;
            using (var pageStore = CreateEmptyPageStore("TestCreateAndUpdatePage.dat"))
            {
                newPage = pageStore.Create();
                pageStore.Write(1, newPage, _testBuffer1);
                pageStore.Commit(1, null);
            }
            Assert.AreEqual(1ul, newPage);
            using (var fs = new FileStream("TestCreateAndUpdatePage.dat", FileMode.Open, FileAccess.Read))
            {
                Assert.AreEqual(8192L, fs.Length);
            }

            using (var pageStore = new BinaryFilePageStore(_pm, "TestCreateAndUpdatePage.dat", 4096, true, 1))
            {
                var pageData = pageStore.Retrieve(newPage, null);
                Assert.AreEqual(0, _testBuffer1.Compare(pageData));
                pageStore.Write(2, newPage, _testBuffer2);
                pageStore.Commit(2, null);
            }

            using (var pageStore = new BinaryFilePageStore(_pm, "TestCreateAndUpdatePage.dat", 4096, true, 2))
            {
                var pageData = pageStore.Retrieve(newPage, null);
                Assert.AreEqual(0, _testBuffer2.Compare(pageData));
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
