#if BTREESTORE
using System;
using System.IO;
using System.Linq;
using BrightstarDB.Model;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BTreeStore;
using BrightstarDB.Utils;
#if !SILVERLIGHT
#endif
#endif
using NUnit.Framework;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class BTreeStoreTests
    {
#if BTREESTORE
        private readonly IStoreManager _storeManager = StoreManagerFactory.GetStoreManager();

        [Test]
        public void TestCreateBtree()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid) as Store;

            var btree = store.MakeNewTree<ObjectRef>(5);

            store.Commit(Guid.Empty);

            var store1 = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid) as Store;
            var tree = store1.LoadObject<PersistentBTree<ObjectRef>>(btree.ObjectId);

            Assert.IsNotNull(tree);
            Assert.AreEqual(tree.ObjectId, btree.ObjectId);
        }


#if !SILVERLIGHT
        // on the phone we can't test import from / export to a file
        [Test]
        public void TestImport()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            var start = DateTime.UtcNow;

            using (var fs = new FileStream("simple.txt", FileMode.Open))
            {
                store.Import(Guid.Empty, fs);

                var end = DateTime.UtcNow;
                Console.WriteLine("Import triples took " + (end - start).TotalMilliseconds);

                start = DateTime.UtcNow;
                store.Commit(Guid.Empty);
                end = DateTime.UtcNow;
                Console.WriteLine("Commit triples took " + (end - start).TotalMilliseconds);

                store = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid) as Store;
                var triples = store.GetResourceStatements("http://example.org/resource23");

                Assert.AreEqual(2, triples.Count());
            }
        }

        [Test]
        public void TestImportEncodedUris()
        {
            var sid = "TestImportEncodedUris_" + DateTime.Now.Ticks;
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid) as Store;
            using (var fs = new FileStream("persondata_en_subset.nt", FileMode.Open))
            {
                store.Import(Guid.Empty, fs);
            }
            store.Commit(Guid.Empty);

            store = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid) as Store;
            var unencodedResourceUri = "http://dbpedia.org/resource/Aleksandar_" +
                                       Uri.UnescapeDataString("%C4%90or%C4%91evi%C4%87");
            var triples = store.GetResourceStatements(unencodedResourceUri);
            Assert.AreEqual(8, triples.Count());
        }

#endif

        [Test]
        public void TestHashIdUtilities()
        {
            var uri = "http://www.networkedplanet.com/people/gra";
            var hash = uri.GetBrightstarHashCode();
            var resourceId = ResourceIdIndex.MakeId(hash, 0);
            Assert.AreEqual(hash, resourceId);

            var resourceIdHash = ResourceIdIndex.GetResourceIdHashCode(resourceId);
            Assert.AreEqual(hash, resourceIdHash);

            var bucketOffset = ResourceIdIndex.GetResourceIdBucketOffset(resourceId);
            Assert.AreEqual(0u, bucketOffset);

            // test with bucket offset 1
            resourceId = ResourceIdIndex.MakeId(hash, 1);
            resourceIdHash = ResourceIdIndex.GetResourceIdHashCode(resourceId);
            Assert.AreEqual(hash, resourceIdHash);

            bucketOffset = ResourceIdIndex.GetResourceIdBucketOffset(resourceId);
            Assert.AreEqual(1u, bucketOffset);

            resourceId = ResourceIdIndex.MakeId(hash, 3);
            resourceIdHash = ResourceIdIndex.GetResourceIdHashCode(resourceId);
            Assert.AreEqual(hash, resourceIdHash);

            bucketOffset = ResourceIdIndex.GetResourceIdBucketOffset(resourceId);
            Assert.AreEqual(3u, bucketOffset);
        }

        [Test]
        public void TestReadStoreCacheValueRespected()
        {
            var sid = Guid.NewGuid().ToString();
            var store = _storeManager.CreateStore(Configuration.StoreLocation + "\\" + sid);

            var t = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "bob",
                DataType = RdfDatatypes.String,
                IsLiteral = true
            };
            store.InsertTriple(t);

            t = new Triple
            {
                Subject = "http://www.networkedplanet.com/people/10",
                Predicate = "http://www.networkedplanet.com/model/isa",
                Object = "kal",
                DataType = RdfDatatypes.String,
                IsLiteral = true
            };
            store.InsertTriple(t);

            store.Commit(Guid.Empty);

            Configuration.ReadStoreObjectCacheSize = 0;

            var store1 = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true) as Store;
            var triples = store1.GetResourceStatements("http://www.networkedplanet.com/people/10");
            Assert.AreEqual(2, triples.Count());
            Assert.AreEqual(0, store1.CacheCount);

            Configuration.ReadStoreObjectCacheSize = 100000;

            store1 = _storeManager.OpenStore(Configuration.StoreLocation + "\\" + sid, true) as Store;
            triples = store1.GetResourceStatements("http://www.networkedplanet.com/people/10");
            Assert.AreEqual(2, triples.Count());
            Assert.IsTrue(store1.CacheCount > 0);
        }
#endif
    }
}
