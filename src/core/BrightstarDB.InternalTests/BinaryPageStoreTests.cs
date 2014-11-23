using System;
using System.IO;
using System.Threading;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;
using BrightstarDB.Utils;
using VDS.RDF.Parsing.Tokens;

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
        public void TestNewPageIsWriteable()
        {
            var p = new BinaryFilePage(1ul, 16, 1ul);
            Assert.That(p.IsWriteable);
        }

        [Test]
        public void TestPageReadFromStreamIsNotWriteable()
        {
            byte[] pageData = MakeTestPage(1, 1, 0, 0);
            var ms = new MemoryStream(pageData);
            var p = new BinaryFilePage(ms, 1ul, 16, 2ul, false);
            Assert.That(p.IsWriteable, Is.False);
        }

        [Test]
        public void TestCreateFromStream()
        {
            byte[] pageData = MakeTestPage(1, 1, 0, 0);
            var ms = new MemoryStream(pageData);
            var p = new BinaryFilePage(ms, 1ul, 16, 2ul, false);
            Assert.That(p.FirstTransactionId, Is.EqualTo(1ul));
            Assert.That(BitConverter.ToUInt64(p.FirstBuffer, 0), Is.EqualTo(1ul));
            Assert.That(p.SecondTransactionId, Is.EqualTo(0ul));
            Assert.That(BitConverter.ToUInt64(p.SecondBuffer, 0), Is.EqualTo(0ul));
        }

        [Test]
        public void TestMakePageWriteable()
        {
            byte[] pageData = MakeTestPage(1, 1, 0, 0);
            var ms = new MemoryStream(pageData);
            var p = new BinaryFilePage(ms, 1ul, 16, 2ul, false);
            p.MakeWriteable(2ul);
            // Data in first buffer should be copied to second buffer
            Assert.That(p.FirstTransactionId, Is.EqualTo(1ul));
            Assert.That(BitConverter.ToUInt64(p.FirstBuffer, 0), Is.EqualTo(1ul));
            Assert.That(p.SecondTransactionId, Is.EqualTo(2ul));
            Assert.That(BitConverter.ToUInt64(p.SecondBuffer, 0), Is.EqualTo(1ul));
        }

        [Test]
        public void TestWriteToPage()
        {
            byte[] pageData = MakeTestPage(1, 1, 0, 0);
            var ms = new MemoryStream(pageData);
            var p = new BinaryFilePage(ms, 1ul, 16, 2ul, false);
            p.MakeWriteable(2ul);
            p.SetData(BitConverter.GetBytes(2ul));
            // Data should get written into the second buffer
            Assert.That(BitConverter.ToUInt64(p.SecondBuffer, 0), Is.EqualTo(2ul));
        }

        private byte[] MakeTestPage(ulong firstTxnId, ulong firstData, ulong secondTxnId, ulong secondData)
        {
            byte[]pageData = new byte[32];
            Array.Copy(BitConverter.GetBytes(firstTxnId), 0, pageData, 0, 8);
            Array.Copy(BitConverter.GetBytes(firstData), 0, pageData, 8, 8);
            Array.Copy(BitConverter.GetBytes(secondTxnId), 0, pageData, 16, 8);
            Array.Copy(BitConverter.GetBytes(secondData), 0, pageData, 24, 8);
            return pageData;
        }

        [Test]
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
            using (var fs = new FileStream("TestCreateAndUpdatePage.dat", FileMode.Open, FileAccess.Read))
            {
                Assert.AreEqual(8192L, fs.Length);
            }

            using (var pageStore = new BinaryFilePageStore(_pm, "TestCreateAndUpdatePage.dat", 4096, false, 1, 2, false))
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
                pageStore.MarkDirty(2, 1ul);
                pageStore.Commit(2, null);
            }

            using (var pageStore = new BinaryFilePageStore(_pm, "TestCreateAndUpdatePage.dat", 4096, true, 2, 3, false))
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
            // Wait for the file to disappear
            while (File.Exists(fileName))
            {
                Thread.Sleep(100);
            }
            return new BinaryFilePageStore(_pm, fileName, 4096, false, 0, 1, false);
        }
    }
}
