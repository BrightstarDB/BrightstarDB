using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Storage;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using NUnit.Framework;

namespace BrightstarDB.Portable.iOS.Tests
{
    [TestFixture]
    public class StorageTests
    {
        private readonly IPersistenceManager _pm = new PersistenceManager();

        [Test]
        public void TestCreateAndDeleteDirectory()
        {
            string dirName = Path.Combine(TestConfiguration.StoreLocation, "CreateDirectory_" + DateTime.Now.Ticks);
            Assert.False(_pm.DirectoryExists(dirName), "Directory exists before it is created");
            _pm.CreateDirectory(dirName);
            Assert.True(_pm.DirectoryExists(dirName), "Directory does not exist after creation");
            _pm.DeleteDirectory(dirName);
            Assert.False(_pm.DirectoryExists(dirName), "Directory exists after deletion");
        }

        [Test]
        public void TestCreateAndDeleteFile()
        {
            string fname = Path.Combine(TestConfiguration.StoreLocation, "CreateFile_" + DateTime.Now.Ticks);
            Assert.False(_pm.FileExists(fname), "File exists before creation");
            _pm.CreateFile(fname);
            Assert.True(_pm.FileExists(fname), "File does not exist after creation");
            Assert.False(_pm.DirectoryExists(fname), "File found by DirectoryExists");
            _pm.DeleteFile(fname);
            Assert.False(_pm.FileExists(fname), "File exists after deletion");
        }

        [Test]
        public void TestCreateSubdirectory()
        {
            string parent = Path.Combine(TestConfiguration.StoreLocation, "Parent_" + DateTime.Now.Ticks);
            string child = "Child1";

            _pm.CreateDirectory(Path.Combine(parent, child));
            Assert.True(_pm.DirectoryExists(Path.Combine(parent, child)), "Could not find child directory");
            Assert.True(_pm.DirectoryExists(parent), "Could not find parent directory");
        }
    }
}