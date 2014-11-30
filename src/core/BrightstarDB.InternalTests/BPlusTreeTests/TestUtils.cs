using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BrightstarDB.Profiling;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests.BPlusTreeTests
{
    internal static class TestUtils
    {
        public static IEnumerable<int> MakeRandomInsertList(int count)
        {
            List<int> ret = new List<int>(count);
            List<int> sorted = new List<int>(count);
            var rng = new Random();
            for (int i = 0; i < count; i++)
            {
                sorted.Add(i);
            }
            while (sorted.Count > 0)
            {
                var sampleIx = rng.Next(sorted.Count);
                ret.Add(sorted[sampleIx]);
                sorted.RemoveAt(sampleIx);
            }
            return ret;
        }

        internal static readonly IPersistenceManager PersistenceManager;

        static TestUtils()
        {
#if SILVERLIGHT
            PersistenceManager = new IsolatedStoragePersistanceManager();
#else
            PersistenceManager = new FilePersistenceManager();
#endif
        }

        public static IPageStore CreateEmptyPageStore(string fileName, PersistenceType persistenceType = PersistenceType.AppendOnly, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("Create Empty Page Store"))
            {
                using (profiler.Step("Delete Existing"))
                {
                    if (PersistenceManager.FileExists(fileName)) PersistenceManager.DeleteFile(fileName);
                    //if (File.Exists(fileName)) File.Delete(fileName);
                }
                using (profiler.Step("Create New Page Store"))
                {
                    if (persistenceType == PersistenceType.AppendOnly)
                    {
                        return new AppendOnlyFilePageStore(PersistenceManager, fileName, 4096, false, false);
                    }
                    return new BinaryFilePageStore(PersistenceManager, fileName, 4096, false, 1, 2, false);
                }
            }
        }

        public static IPageStore OpenPageStore(string fileName, bool readOnly, PersistenceType persistenceType = PersistenceType.AppendOnly, ulong txnId = 1UL, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("Open Page Store"))
            {
                if (persistenceType == PersistenceType.AppendOnly)
                {
                    return new AppendOnlyFilePageStore(PersistenceManager, fileName, 4096, readOnly, false);
                }
                return new BinaryFilePageStore(PersistenceManager, fileName, 4096, readOnly, txnId, txnId + 1, false);
            }
        }

        public static byte[] StringToByteArray(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        public static void AssertBuffersEqual(byte[] expected, byte[] actual)
        {
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual[i], "Buffers differed at index {0}", i);
            }
        }

    }
}
