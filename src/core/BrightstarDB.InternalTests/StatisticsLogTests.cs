using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Storage;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Storage.Statistics;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class StatisticsLogTests
    {
        private const string BaseDirectory = "StatisticsLogTests";
        private IPersistenceManager _persistenceManager;

        [TestFixtureSetUp]
        public void SetUp()
        {
            _persistenceManager = new FilePersistenceManager();
            if (Directory.Exists(BaseDirectory))
            {
                Directory.Delete(BaseDirectory, true);
            }
            Directory.CreateDirectory(BaseDirectory);
        }

        [Test]
        public void TestReadWithNoLogFile()
        {
            var storePath = Path.Combine(BaseDirectory, "ReadWithNoLogFile");
            Directory.CreateDirectory(storePath);
            var log = new PersistentStatisticsLog(_persistenceManager, storePath);
            Assert.AreEqual(0, log.GetStatistics().Count());
        }

        [Test]
        public void TestCreateNewLogFile()
        {
            var storePath = Path.Combine(BaseDirectory, "CreateNewLogFile");
            Directory.CreateDirectory(storePath);
            var log = new PersistentStatisticsLog(_persistenceManager, storePath);
            var timestamp = DateTime.Now;
            var predicates = new Dictionary<string, ulong> {{"http://example.org/p1", 1}, {"http://example.org/p2", 2}};
            log.AppendStatistics(new StoreStatistics(1, timestamp, 3, predicates));
            Assert.IsTrue(File.Exists(Path.Combine(storePath, "stats.bs")));
            Assert.IsTrue(File.Exists(Path.Combine(storePath, "statsheaders.bs")));

            var allStats = log.GetStatistics().ToList();
            Assert.AreEqual(1, allStats.Count);

            Assert.AreEqual(1UL, allStats[0].CommitNumber);
            Assert.AreEqual(timestamp, allStats[0].CommitTime);
            Assert.AreEqual(3UL, allStats[0].TripleCount);
            Assert.IsTrue(allStats[0].PredicateTripleCounts.ContainsKey("http://example.org/p1"));
            Assert.AreEqual(1UL, allStats[0].PredicateTripleCounts["http://example.org/p1"]);
            Assert.IsTrue(allStats[0].PredicateTripleCounts.ContainsKey("http://example.org/p2"));
            Assert.AreEqual(2UL, allStats[0].PredicateTripleCounts["http://example.org/p2"]);
            Assert.AreEqual(2, allStats[0].PredicateTripleCounts.Count);
        }

        [Test]
        public void TestAppendPredicateStats()
        {
            var storePath = Path.Combine(BaseDirectory, "AppendPredicateStats");

            Directory.CreateDirectory(storePath);
            var log = new PersistentStatisticsLog(_persistenceManager, storePath);
            var timestamp = DateTime.Now;
            var predicates = new Dictionary<string, ulong> { { "http://example.org/p1", 1 }, { "http://example.org/p2", 2 } };
            log.AppendStatistics(new StoreStatistics(1, timestamp, 3, predicates));
            Assert.IsTrue(File.Exists(Path.Combine(storePath, "stats.bs")));
            Assert.IsTrue(File.Exists(Path.Combine(storePath, "statsheaders.bs")));

            var allStats = log.GetStatistics().ToList();
            Assert.AreEqual(1, allStats.Count);

            // Create a new log instance
            log = new PersistentStatisticsLog(_persistenceManager, storePath);
            timestamp = DateTime.Now;
            predicates = new Dictionary<string, ulong>{{"http://example.org/p1", 2}, {"http://example.org/p2", 2}, {"http://example.org/p3", 3}};
            log.AppendStatistics(new StoreStatistics(2, timestamp, 7, predicates));

            // Retrieve stats via a new log instance
            log = new PersistentStatisticsLog(_persistenceManager, storePath);
            var stats = log.GetStatistics().FirstOrDefault();
            Assert.IsNotNull(stats);

            Assert.AreEqual(2UL, stats.CommitNumber);
            Assert.AreEqual(timestamp, stats.CommitTime);
            Assert.AreEqual(7UL, stats.TripleCount);
            Assert.IsTrue(stats.PredicateTripleCounts.ContainsKey("http://example.org/p1"));
            Assert.AreEqual(2UL, stats.PredicateTripleCounts["http://example.org/p1"]);
            Assert.IsTrue(stats.PredicateTripleCounts.ContainsKey("http://example.org/p2"));
            Assert.AreEqual(2UL, stats.PredicateTripleCounts["http://example.org/p2"]);
            Assert.IsTrue(stats.PredicateTripleCounts.ContainsKey("http://example.org/p3"));
            Assert.AreEqual(3UL, stats.PredicateTripleCounts["http://example.org/p3"]);
            Assert.AreEqual(3, stats.PredicateTripleCounts.Count);

        }

        [Test]
        public void TestReadEmptyRecord()
        {
            var storePath = Path.Combine(BaseDirectory, "ReadEmptyRecord");

            Directory.CreateDirectory(storePath);
            var log = new PersistentStatisticsLog(_persistenceManager, storePath);
            var timestamp = DateTime.Now;
            var predicates = new Dictionary<string, ulong>();
            log.AppendStatistics(new StoreStatistics(1, timestamp, 0, predicates));
            Assert.IsTrue(File.Exists(Path.Combine(storePath, "stats.bs")));
            Assert.IsTrue(File.Exists(Path.Combine(storePath, "statsheaders.bs")));

            var allStats = log.GetStatistics().ToList();
            Assert.AreEqual(1, allStats.Count);

            Assert.AreEqual(1UL, allStats[0].CommitNumber);
            Assert.AreEqual(timestamp, allStats[0].CommitTime);
            Assert.AreEqual(0UL, allStats[0].TripleCount);
            Assert.AreEqual(0, allStats[0].PredicateTripleCounts.Count);
        }
    }
}
