using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BTreeStore;

namespace BrightstarDB.Analysis
{
    /// <summary>
    /// Reads through the containers in a store and provides objects to one or more
    /// analyzers that are plugged in to it.
    /// </summary>
    public class StoreCrawler
    {
        private ObjectLocationManager _objectLocationManager;
        private readonly List<IStoreAnalyzer> _analyzers;
        private Store _store;
        private int _loadCount;
        private const int MaxLoadCount = 100;

        /// <summary>
        /// Creates a new crawler that will report to the specified analyzers
        /// </summary>
        /// <param name="analyzers">The analyzers that will receive crawl events</param>
        public StoreCrawler(params IStoreAnalyzer[] analyzers)
        {
            _analyzers = new List<IStoreAnalyzer>();
            if (analyzers != null) _analyzers.AddRange(analyzers);
        }

        /// <summary>
        /// Adds another analyzer to be notified as the store is crawled
        /// </summary>
        /// <param name="analyzer"></param>
        public void AddAnalyzer(IStoreAnalyzer analyzer)
        {
            _analyzers.Add(analyzer);
        }

        /// <summary>
        /// Starts the crawl of the store contained in the specified directory
        /// </summary>
        /// <param name="storePath">The full path to the directory that contains the store to be crawled</param>
        public void Run(string storePath)
        {
            var dataFile = new FileInfo(Path.Combine(storePath, AbstractStoreManager.DataFileName));
            var masterFile = new FileInfo(Path.Combine(storePath, AbstractStoreManager.MasterFileName));
            if (!dataFile.Exists) throw new FileNotFoundException("Cannot find data file", dataFile.FullName);

            string storeLocation;
            ulong nextObjectId, resourceIdIndexObjectId, graphUriToIdObjectId;
            PredicateIndexResourceToObjectIdIndex propertyTypeSubjectIndex;
            PredicateIndexResourceToObjectIdIndex propertyTypeObjectIndex;

            var sm = StoreManagerFactory.GetStoreManager() as AbstractStoreManager;
            var offset = sm.GetLatestStorePositionFromMasterFile(masterFile.FullName);

            // We need to introspect the datastream directly first because Store does not currently surface direct access to index object ids
            using (
                var dataStream =
                    new BinaryReader(new FileStream(dataFile.FullName, FileMode.Open, FileAccess.Read,
                                                    FileShare.ReadWrite)))
            {
                dataStream.BaseStream.Seek((long) offset, SeekOrigin.Begin);
                SerializationUtils.ReadVarint(dataStream);
                var storeLocationSize = (int) SerializationUtils.ReadVarint(dataStream);
                var locationBytes = dataStream.ReadBytes(storeLocationSize);
                storeLocation = Encoding.UTF8.GetString(locationBytes, 0, storeLocationSize);
                nextObjectId = SerializationUtils.ReadVarint(dataStream);
                resourceIdIndexObjectId = SerializationUtils.ReadVarint(dataStream);
                graphUriToIdObjectId = SerializationUtils.ReadVarint(dataStream);
                _objectLocationManager = new ObjectLocationManager();
                _objectLocationManager.Read(dataStream);
                propertyTypeObjectIndex = new PredicateIndexResourceToObjectIdIndex();
                propertyTypeObjectIndex.Read(dataStream);
                propertyTypeSubjectIndex = new PredicateIndexResourceToObjectIdIndex();
                propertyTypeSubjectIndex.Read(dataStream);
            }

            _store = sm.OpenStore(storePath, true) as Store;
            var lastCommit = _store.GetCommitPoints().First();

            foreach (var a in _analyzers)
            {
                a.OnStoreStart(_store.ObjectId, storeLocation, nextObjectId, lastCommit.CommitTime);
            }
            CrawlBTree<Bucket>(resourceIdIndexObjectId, "Resource String to Resource ID Index");
            CrawlBTree<Bucket>(graphUriToIdObjectId, "Graph URI to Resource ID Index");
            CrawlPredicateIndex(propertyTypeSubjectIndex, "Property Type Subject Index");
            CrawlPredicateIndex(propertyTypeObjectIndex, "Property Type Object Index");
            foreach (var a in _analyzers)
            {
                a.OnStoreEnd(_store.ObjectId);
            }
        }

        private void CrawlPredicateIndex(PredicateIndexResourceToObjectIdIndex predicateIndex, string indexName)
        {
            foreach (var a in _analyzers)
            {
                a.OnPredicateIndexStart(indexName, predicateIndex.Entries.Count());
            }
            foreach (var entry in predicateIndex.Entries)
            {
                var indexId = entry.IndexObjectId;
                var resource = _store.Resolve(entry.ResourceId);
                CrawlBTree<ObjectRef>(indexId, indexName + " : " + resource.LexicalValue);
            }
            foreach (var a in _analyzers)
            {
                a.OnPredicateIndexEnd(indexName);
            }
        }

        private void CrawlBTree<T>(ulong btreeId, string btreeName) where T : class, IStorable
        {
            if (btreeId == 0) return;
            var btree = LoadObject<PersistentBTree<T>>(btreeId);
            foreach (var a in _analyzers)
            {
                a.OnBTreeStart(btreeName, btreeId, btree.KeyCount, btree.KeysMinimum);
            }
            CrawlBTreeNode(btree.Root, 0);
            foreach (var a in _analyzers)
            {
                a.OnBTreeEnd(btreeName, btreeId);
            }
        }

        private void CrawlBTreeNode<T>(Node<T> node, int currentDepth) where T : class, IStorable
        {
            foreach (var a in _analyzers)
            {
                a.OnNodeStart(node.ObjectId, currentDepth, node.Keys.Count, node.ChildNodes.Count);
            }

            if (typeof (T) == typeof (ObjectRef))
            {
                foreach (var k in node.Keys)
                {
                    var objectRef = k.Value as ObjectRef;
                    if (objectRef != null)
                    {
                        if (objectRef.ObjectId != 0)
                        {
                            var rrl = LoadObject<RelatedResourceList>(objectRef.ObjectId);
                            foreach (var a in _analyzers)
                            {
                                a.OnRelatedResourceListStart("Related Resource List - " + k.Key, objectRef.ObjectId, rrl.KeyCount, rrl.KeysMinimum);
                            }
                            CrawlBTreeNode(rrl.Root, 0);
                            foreach (var a in _analyzers)
                            {
                                a.OnRelatedResourceListEnd(objectRef.ObjectId);
                            }
                        }
                    }
                }
            }

            foreach (var c in node.ChildNodes)
            {
                var childNode = LoadObject<Node<T>>(c);
                CrawlBTreeNode(childNode, currentDepth + 1);
            }

            foreach (var a in _analyzers)
            {
                a.OnNodeEnd(node.ObjectId);
            }
        }

        private T LoadObject<T>(ulong objectId) where T:class,IPersistable
        {
            if (_loadCount > MaxLoadCount)
            {
                _store.FlushChanges();
                _loadCount = 0;
            }
            _loadCount++;
            return _store.LoadObject<T>(objectId);
        }
    }
}
