using System;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Mobile.Tests
{
    [TestClass]
    public class IsoStoreTest
    {
        [TestMethod]
        public void TestIncrementalWrite()
        {
            var isoStore = IsolatedStorageFile.GetUserStoreForApplication();
            var dirName = Guid.NewGuid().ToString("N");
            var buff = new byte[] {0x1, 0x2, 0x3, 4, 5, 6, 7, 8, 9, 10};
            isoStore.CreateDirectory(dirName);
            var fileName = System.IO.Path.Combine(dirName, "data.hs");
            using (var fs = isoStore.CreateFile(fileName))
            {
                fs.Close();
            }

            using (var fs = isoStore.OpenFile(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                fs.Write(buff, 0, 10);
                fs.Flush();
                fs.Close();
            }

            using (var fs = isoStore.OpenFile(fileName, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                fs.Write(buff, 0, 10);
                fs.Flush();
                fs.Close();
            }

        }
    }
}
