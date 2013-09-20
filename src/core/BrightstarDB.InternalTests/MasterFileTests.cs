using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class MasterFileTests
    {

        [Test]
        public void TestCreateNewMaster()
        {
            var pm = new FilePersistenceManager();
            var dirName = "TestCreateNewMaster1";
            EnsureEmptyDirectory(pm, dirName);
            var storeConfig = new StoreConfiguration {PersistenceType = PersistenceType.AppendOnly};
            var storeSetId = Guid.NewGuid();
            var mf = MasterFile.Create(pm, dirName, storeConfig, storeSetId);
            var storeId = mf.StoreId;
            mf = MasterFile.Open(pm, dirName);
            Assert.AreEqual(storeId, mf.StoreId);
            Assert.AreEqual(storeSetId, mf.StoreSetId);
            Assert.AreEqual(StoreType.Standard, mf.StoreType);
            Assert.AreEqual(PersistenceType.AppendOnly, mf.PersistenceType);

            dirName = "TestCreateNewMaster2";
            EnsureEmptyDirectory(pm, dirName);
            storeConfig.PersistenceType = PersistenceType.Rewrite;
            storeSetId = Guid.NewGuid();
            mf = MasterFile.Create(pm, dirName, storeConfig, storeSetId);
            storeId = mf.StoreId;
            mf = MasterFile.Open(pm, dirName);
            Assert.AreEqual(storeId, mf.StoreId);
            Assert.AreEqual(storeSetId, mf.StoreSetId);
            Assert.AreEqual(StoreType.Standard, mf.StoreType);
            Assert.AreEqual(PersistenceType.Rewrite, mf.PersistenceType);

            // Enumerating commit points of a new master file should not throw an error
            Assert.AreEqual(0, mf.GetCommitPoints().Count());
        }

        [Test]
        public void TestAppendCommitPoint()
        {
            var pm = new FilePersistenceManager();
            const string dirName = "TestAppendCommitPoint";
            EnsureEmptyDirectory(pm, dirName);

            var storeConfig = new StoreConfiguration {PersistenceType = PersistenceType.AppendOnly};
            var storeSetId = Guid.NewGuid();
            var mf = MasterFile.Create(pm, dirName, storeConfig, storeSetId);
            DateTime commit1Time = DateTime.UtcNow;
            Guid commit1JobId = Guid.NewGuid();
            mf = MasterFile.Open(pm, dirName);
            mf.AppendCommitPoint(new CommitPoint(1ul, 1ul, commit1Time, commit1JobId));
            DateTime commit2Time = DateTime.UtcNow;
            Guid commit2JobId = Guid.NewGuid();
            mf = MasterFile.Open(pm, dirName);
            mf.AppendCommitPoint(new CommitPoint(2ul, 2ul, commit2Time, commit2JobId));

            mf = MasterFile.Open(pm, dirName);
            var allCommits = mf.GetCommitPoints().ToList();
            Assert.AreEqual(2, allCommits.Count);
            Assert.AreEqual(2ul, allCommits[0].CommitNumber);
            Assert.AreEqual(2ul, allCommits[0].LocationOffset);
            Assert.AreEqual(commit2JobId, allCommits[0].JobId);
            Assert.AreEqual(commit2Time.Ticks, allCommits[0].CommitTime.Ticks);

            Assert.AreEqual(1ul, allCommits[1].CommitNumber);
            Assert.AreEqual(1ul, allCommits[1].LocationOffset);
            Assert.AreEqual(commit1JobId, allCommits[1].JobId);
            Assert.AreEqual(commit1Time.Ticks, allCommits[1].CommitTime.Ticks);

            var lastCommit = mf.GetLatestCommitPoint();
            Assert.AreEqual(2ul, lastCommit.CommitNumber);
            Assert.AreEqual(2ul, lastCommit.LocationOffset);
            Assert.AreEqual(commit2JobId, lastCommit.JobId);
            Assert.AreEqual(commit2Time.Ticks, lastCommit.CommitTime.Ticks);

        }

        [Test]
        public void TestCorruptCommitPoint()
        {
            var pm = new FilePersistenceManager();
            const string dirName = "TestCorruptCommitPoint";
            EnsureEmptyDirectory(pm, dirName);
            var storeConfig = new StoreConfiguration { PersistenceType = PersistenceType.AppendOnly };
            var storeSetId = Guid.NewGuid();
            var mf = MasterFile.Create(pm, dirName, storeConfig, storeSetId);
            DateTime commit1Time = DateTime.UtcNow;
            Guid commit1JobId = Guid.NewGuid();
            mf = MasterFile.Open(pm, dirName);
            mf.AppendCommitPoint(new CommitPoint(1ul, 1ul, commit1Time, commit1JobId));
            DateTime commit2Time = DateTime.UtcNow;
            Guid commit2JobId = Guid.NewGuid();
            mf = MasterFile.Open(pm, dirName);
            mf.AppendCommitPoint(new CommitPoint(2ul, 2ul, commit2Time, commit2JobId));

            mf = MasterFile.Open(pm, dirName);
            var allCommits = mf.GetCommitPoints().ToList();
            Assert.AreEqual(2, allCommits.Count);

            using (var fs = pm.GetOutputStream(Path.Combine(dirName, MasterFile.MasterFileName), FileMode.Open))
            {
                fs.Seek(-250, SeekOrigin.End);
                fs.WriteByte(255);
            }
            // Error in one half of commit point should not cause a problem
            mf = MasterFile.Open(pm, dirName);
            var lastCommit = mf.GetLatestCommitPoint();
            allCommits = mf.GetCommitPoints().ToList();
            Assert.AreEqual(2, allCommits.Count);
            Assert.AreEqual(2ul, lastCommit.CommitNumber);
            Assert.AreEqual(2ul, lastCommit.LocationOffset);
            Assert.AreEqual(commit2JobId, lastCommit.JobId);
            Assert.AreEqual(commit2Time.Ticks, lastCommit.CommitTime.Ticks);

            using(var fs = pm.GetOutputStream(Path.Combine(dirName, MasterFile.MasterFileName), FileMode.Open))
            {
                fs.Seek(-120, SeekOrigin.End);
                fs.WriteByte(255);
            }
            // Error in both halves of commit point should force a rewind to previous commit point
            mf = MasterFile.Open(pm, dirName);
            lastCommit = mf.GetLatestCommitPoint();
            allCommits = mf.GetCommitPoints().ToList();
            Assert.AreEqual(1, allCommits.Count);

            Assert.AreEqual(1ul, lastCommit.CommitNumber);
            Assert.AreEqual(1ul, lastCommit.LocationOffset);
            Assert.AreEqual(commit1JobId, lastCommit.JobId);
            Assert.AreEqual(commit1Time.Ticks, lastCommit.CommitTime.Ticks);

        }

        private void EnsureEmptyDirectory(IPersistenceManager pm, string dirName)
        {
            if (pm.DirectoryExists(dirName))
            {
                pm.DeleteDirectory(dirName);
            }
            Thread.Sleep(10);
            pm.CreateDirectory(dirName);
        }

    }
}
