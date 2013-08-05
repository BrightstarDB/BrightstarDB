using System.IO;
using System.Linq;
using BrightstarDB.Portable.Compatibility;
using BrightstarDB.Storage;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using FileMode=BrightstarDB.Portable.Compatibility.FileMode;

namespace BrightstarDB.Portable.Tests
{
    [TestClass]
    public class TestPersistenceManagerOperations
    {
        private readonly IPersistenceManager _pm;

        public TestPersistenceManagerOperations()
        {
            _pm = new PersistenceManager();
        }

        [TestMethod]
        public void TestCreateFile()
        {
            _pm.CreateFile("test.txt");
            Assert.IsTrue(_pm.FileExists("test.txt"));
        }

        [TestMethod]
        public void TestDeleteFile()
        {
            _pm.CreateFile("testdelete.txt");
            Assert.IsTrue(_pm.FileExists("testdelete.txt"));
            _pm.DeleteFile("testdelete.txt");
            Assert.IsFalse(_pm.FileExists("testdelete.txt"));
        }

        [TestMethod]
        public void TestCreateDirectory()
        {
            Assert.IsFalse(_pm.DirectoryExists("newdir"));
            _pm.CreateDirectory("newdir");
            Assert.IsTrue(_pm.DirectoryExists("newdir"));
        }

        [TestMethod]
        public void TestDeleteDirectory()
        {
            Assert.IsFalse(_pm.DirectoryExists("deletedir"));
            _pm.CreateDirectory("deletedir");
            Assert.IsTrue(_pm.DirectoryExists("deletedir"));
            _pm.DeleteDirectory("deletedir");
            Assert.IsFalse(_pm.DirectoryExists("deletedir"));
        }

        [TestMethod]
        public void TestDeleteNonEmptyDirectory()
        {
           _pm.CreateDirectory("notempty");
            _pm.CreateFile("notempty\\testfile.txt");
            Assert.IsTrue(_pm.DirectoryExists("notempty"));
            Assert.IsTrue(_pm.FileExists("notempty\\testfile.txt"));
            _pm.DeleteDirectory("notempty");
            Assert.IsFalse(_pm.DirectoryExists("notempty"));
            Assert.IsFalse(_pm.FileExists("notempty\\testfile.txt"));
        }

        [TestMethod]
        public void TestOpenForWriting()
        {
            Assert.IsFalse(_pm.FileExists("writeme.txt"));
            using (var writer = new StreamWriter(_pm.GetOutputStream("writeme.txt", FileMode.CreateNew)))
            {
                writer.Write("Hello world");
            }
            Assert.IsTrue(_pm.FileExists("writeme.txt"));
            Assert.AreEqual(11, _pm.GetFileLength("writeme.txt"));

            // Test using Open mode
            using (var writer = new StreamWriter(_pm.GetOutputStream("writeme.txt", FileMode.Open)))
            {
                writer.BaseStream.Seek(0, SeekOrigin.End);
                writer.Write(" and goodbye");
            }
            Assert.AreEqual(23, _pm.GetFileLength("writeme.txt"));

            // Test using Truncate mode
            using (var writer = new StreamWriter(_pm.GetOutputStream("writeme.txt", FileMode.Truncate)))
            {
                writer.Write("Hello world");
            }
            Assert.AreEqual(11, _pm.GetFileLength("writeme.txt"));

            // Test using Append mode
            using (var writer = new StreamWriter(_pm.GetOutputStream("writeme.txt", FileMode.Append)))
            {
                writer.Write(" and goodbye");
            }
            Assert.AreEqual(23, _pm.GetFileLength("writeme.txt"));
        }

        [TestMethod]
        public void TestOpenForReading()
        {
            Assert.IsFalse(_pm.FileExists("readme.txt"));
            using (var writer = new StreamWriter(_pm.GetOutputStream("readme.txt", FileMode.CreateNew)))
            {
                writer.Write("Hello world");
            }
            using (var reader = new StreamReader(_pm.GetInputStream("readme.txt")))
            {
                Assert.AreEqual("Hello world", reader.ReadLine());
            }
        }

        [TestMethod]
        public void TestListFolders()
        {
            try
            {
                _pm.ListSubDirectories("parent");
                Assert.Fail("Expected FileNotFoundException");
            }
            catch (FileNotFoundException)
            {
                // Expected
            }

            _pm.CreateDirectory("parent");
            Assert.AreEqual(0, _pm.ListSubDirectories("parent").Count());

            _pm.CreateDirectory("parent\\child1");
            var subdirs = _pm.ListSubDirectories("parent").ToList();
            Assert.AreEqual(1, subdirs.Count);
            Assert.IsTrue(subdirs.Any(x => x.Equals("child1")));

            _pm.CreateDirectory("parent\\child2");
            subdirs = _pm.ListSubDirectories("parent").ToList();
            Assert.AreEqual(2, subdirs.Count());
            Assert.IsTrue(subdirs.Any(x=>x.Equals("child1")));
            Assert.IsTrue(subdirs.Any(x=>x.Equals("child2")));

            _pm.CreateDirectory("parent\\child2\\grandchild1");
            subdirs = _pm.ListSubDirectories("parent").ToList();
            Assert.AreEqual(2, subdirs.Count());
            Assert.IsTrue(subdirs.Any(x => x.Equals("child1")));
            Assert.IsTrue(subdirs.Any(x => x.Equals("child2")));
            subdirs = _pm.ListSubDirectories("parent\\child2").ToList();
            Assert.AreEqual(1, subdirs.Count());
            Assert.IsTrue(subdirs.Any(x => x.Equals("grandchild1")));
        }

        [TestMethod]
        public void TestFileLengthForNonExistantFile()
        {
            // GetFileLength should return 0 if the file is not found, not throw an exception
            Assert.AreEqual(0, _pm.GetFileLength("nonexistantfile"));
        }
    }
}
