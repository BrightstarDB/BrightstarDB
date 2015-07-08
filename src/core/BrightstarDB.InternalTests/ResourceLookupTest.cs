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
            using (
                var store = storeManager.OpenStore("C:\\brightstar\\Data") as BrightstarDB.Storage.BPlusTreeStore.Store)
            {
                var delSink = new DeletePatternSink(store);
                var parser = new NTriplesParser();
                parser.Parse(new StringReader(DeletePatterns), delSink, Constants.DefaultGraphUri);
                store.Commit(Guid.NewGuid());
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
