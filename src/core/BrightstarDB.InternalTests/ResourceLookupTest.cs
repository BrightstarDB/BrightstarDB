using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Rdf;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class ResourceLookupTest
    {
        private const ulong BadResourceId = 3435775048533671937;

        [Test]
        public void TestListCommitPoints()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            using (
                var store = storeManager.OpenStore("C:\\brightstar\\Data") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                var txnLog = storeManager.GetTransactionLog("C:\\brightstar\\Data");
                var txnEnumerator = txnLog.GetTransactionList();
                foreach(var c in store.GetCommitPoints().Take(5))
                {
                    txnEnumerator.MoveNext();
                    var txn = txnEnumerator.Current;
                    Console.WriteLine("Commit #:{0}, Commit Time: {1}, Offset:{2}, TransactionType:{3}", c.CommitNumber, c.CommitTime, c.LocationOffset, txn.TransactionType);
                }
            }
        }
        [Test]
        public void TestLookupResourceInBrokenIndex()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            using (var store = storeManager.OpenStore("C:\\brightstar\\Data") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                Assert.That(store, Is.Not.Null, "could not open store");
                var resource = store.Resolve(BadResourceId);
                Assert.That(resource, Is.Not.Null);
            }
        }

        [Test]
        public void TestDumpResourceIndex()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            using (var store = storeManager.OpenStore("C:\\brightstar\\Data") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                store.DumpResourceIndex();
            }
            
        }
        [Test]
        public void TestLookupResourceInBrokenIndexByCommitPoint()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            using (
                var store =
                    storeManager.OpenStore("C:\\brightstar\\Data", 17588ul) as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                Assert.That(store, Is.Not.Null, "could not open store");
                var resource = store.Resolve(BadResourceId);
                Assert.That(resource, Is.Not.Null);
            }
        }

        [Test]
        public void TestLookupResourceInBeforeIndex()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            using (var store = storeManager.OpenStore("C:\\brightstar\\DataBefore") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                Assert.That(store, Is.Not.Null, "could not open store");
                var resource = store.Resolve(BadResourceId);
                Assert.That(resource, Is.Not.Null);
            }
        }

        [Test]
        public void TestBreakTheStore()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            const string DeletePatterns =
                "<http://mcs.dataplatform.co.uk/data/wildlife> <http://rdfs.org/ns/void#dataDump> <http://www.brightstardb.com/.well-known/model/wildcard> <http://www.brightstardb.com/.well-known/model/wildcard> .";
            const string InsertTriples = 
                "<http://mcs.dataplatform.co.uk/data/wildlife> <http://rdfs.org/ns/void#dataDump> \"http://mcs.dataplatform.co.uk/download/data/wildlife_20150708112735.nt\"^^<http://www.w3.org/2001/XMLSchema#string> <http://www.brightstardb.com/.well-known/model/defaultgraph> .";

            using (
                var store = storeManager.OpenStore("C:\\brightstar\\Data") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                var jobId = Guid.NewGuid();
                var delSink = new DeletePatternSink(store);
                var parser = new NTriplesParser();
                parser.Parse(new StringReader(DeletePatterns), delSink, Constants.DefaultGraphUri);

                var insertSink = new StoreTripleSink(store, jobId );
                parser = new NTriplesParser();
                parser.Parse(new StringReader(InsertTriples), insertSink, Constants.DefaultGraphUri);

                store.Commit(jobId);
            }


            using (
                var store = storeManager.OpenStore("C:\\brightstar\\Data") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                Assert.That(store, Is.Not.Null, "could not open store");
                var resource = store.Resolve(BadResourceId);
                Assert.That(resource, Is.Not.Null);
            }
        }
    }
}
