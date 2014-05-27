using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Client;
using BrightstarDB.Profiling;
using BrightstarDB.Query;
using BrightstarDB.Rdf;
using BrightstarDB.Storage.Persistence;
using VDS.RDF;
using VDS.RDF.Query;
using Triple = BrightstarDB.Model.Triple;

namespace BrightstarDB.Storage.BTreeStore
{
    /// <summary>
    /// Store class is really the entry point to the indexes
    /// </summary>
    internal sealed class Store : IPersistable, IStore, IDisposable
    {
        private const int TreeNodeSize = 51;

        // this will be assigned when we run multiple nodes.
        public static int NodeId = 1;

        /// <summary>
        /// Small list of graph uris.
        /// </summary>
        private readonly GraphIndex _graphIndex;

        private string _instanceId; // used to identify cache query results for this instantiation.
#if !SILVERLIGHT && !PORTABLE
        private string _tmpPath;
#endif
        private readonly PrefixManager _prefixManager;
        private List<IPersistable> _commitList;
        private Stream _inputStream;
        private ObjectCache _loadedObjects;

        /// <summary>
        /// The next node id is persisted as part of the store data.
        /// </summary>
        private ulong _nextObjectId;

        /// <summary>
        /// The mapping of object ids to location offsets
        /// </summary>
        private ObjectLocationManager _objectLocationManager;

        /// <summary>
        /// This index collection uses the hashcode of the property type to map to
        /// an index. The index uses the object proxy as the key, the value is a ordered list of subject values.
        /// </summary>
        private PredicateIndexResourceToObjectIdIndex _propertyTypeObjectIndex =
            new PredicateIndexResourceToObjectIdIndex();

        /// <summary>
        /// This index collection uses the hashcode of the property type to map to
        /// an index. The index uses the subject proxy as the key, the value is a ordered list property values.
        /// </summary>        
        private PredicateIndexResourceToObjectIdIndex _propertyTypeSubjectIndex =
            new PredicateIndexResourceToObjectIdIndex();

        /// <summary>
        /// Stores the mapping of resource hashcode to resource ID
        /// </summary>
        private ResourceIdIndex _resourceIdIndex;

        /// <summary>
        /// Object id for resourceId index
        /// </summary>
        private ulong _resourceIdIndexObjectId;

        /// <summary>
        /// The object id of the store
        /// </summary>
        private ulong _storeId;

        /// <summary>
        /// The folder where all data for this store is kept
        /// </summary>
        private string _storeLocation;

        /// <summary>
        /// Indicates if this store is read only
        /// </summary>
        private bool _isReadOnly;

        /// <summary>
        /// Get the full path to the store directory
        /// </summary>
        public string DirectoryPath
        {
            get { return _storeLocation; }
            internal set
            {
                _storeLocation = value;
                _objectLocationManager.StoreFileName = StoreDataFile;
            }
        }

        /// <summary>
        /// Each store has a location on disk. This location is a folder in which all data files for the store
        /// are maintained.
        /// </summary>
        /// <param name="storeLocation">The disk store location</param>
        /// <param name="readOnly">Create a read only store</param>
        internal Store(string storeLocation, bool readOnly)
        {
#if !SILVERLIGHT && !PORTABLE
            _tmpPath = Path.GetTempPath();
#endif
            _instanceId = Guid.NewGuid().ToString();
            _storeLocation = storeLocation;
            _loadedObjects = new ObjectCache(readOnly);
            _storeId = GetNextObjectId();
            _graphIndex = new GraphIndex();
            _prefixManager = new PrefixManager();
            _objectLocationManager = new ObjectLocationManager
                                         {
                                             StoreFileName = StoreDataFile
                                         };
        }

        /// <summary>
        /// Default constructor for serialisation
        /// </summary>
        public Store()
        {
            _graphIndex = new GraphIndex();
            _prefixManager = new PrefixManager();
#if !SILVERLIGHT && !PORTABLE
            _tmpPath = Path.GetTempPath();
#endif
            _instanceId = Guid.NewGuid().ToString();
            _loadedObjects = new ObjectCache(false);
        }

        public bool IsReadOnly
        {
            get { return _isReadOnly; }
            set
            {
                _loadedObjects = new ObjectCache(value);
                _isReadOnly = value;
            }
        }

        public int CacheCount
        {
            get { return _loadedObjects.CachedObjectCount; }
        }

        /// <summary>
        /// The store data file contains all indexes and proxy objects.
        /// </summary>
        public string StoreDataFile
        {
            get { return _storeLocation + "\\" + AbstractStoreManager.DataFileName; }
        }

        public string StoreConsolidateFile
        {
            get { return _storeLocation + "\\" + AbstractStoreManager.ConsolidateDataFileName; }
        }

        public void Consolidate(Guid jobId)
        {
            GetStoreManager().ConsolidateStore(this, _storeLocation, jobId);
        }

        public ulong CopyTo(IPageStore pageStore, ulong txnId)
        {
            throw new NotImplementedException();
        }

        private ResourceIdIndex ResourceIdIndex
        {
            get
            {
                if (_resourceIdIndex == null)
                {
                    if (_resourceIdIndexObjectId == 0)
                    {
                        PersistentBTree<Bucket> resourceIndexTree = MakeNewTree<Bucket>(51);
                        _resourceIdIndex = new ResourceIdIndex(this, resourceIndexTree);
                        _resourceIdIndexObjectId = resourceIndexTree.ObjectId;
                    }
                    else
                    {
                        var resourceIndexTree = LoadObject<PersistentBTree<Bucket>>(_resourceIdIndexObjectId);
                        _resourceIdIndex = new ResourceIdIndex(this, resourceIndexTree);
                    }
                }
                return _resourceIdIndex;
            }
        }

        public Stream InputDataStream
        {
            get
            {
                if (_inputStream == null)
                {
                    _inputStream = GetStoreManager().GetInputStream(StoreDataFile);
                }
                return _inputStream;
            }
        }

        private IStoreManager2 GetStoreManager()
        {
            IStoreManager2 ret = StoreManagerFactory.GetStoreManager() as IStoreManager2;
            if (ret == null)
            {
                throw new Exception("Invalid store manager instance returned by store manager factory");
            }
            return ret;
        }

        private List<IPersistable> CommitList
        {
            get
            {
                if (_commitList == null)
                {
                    _commitList = new List<IPersistable>();
                }
                return _commitList;
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region IPersistable Members

        public int Save(BinaryWriter dataStream, ulong offset)
        {
            int count = SerializationUtils.WriteVarint(dataStream, _storeId);
            count += SerializationUtils.WriteVarint(dataStream, _nextObjectId);
            count += SerializationUtils.WriteVarint(dataStream, _resourceIdIndexObjectId);
            count += _objectLocationManager.Save(dataStream, offset + (ulong) count);
            count += _propertyTypeObjectIndex.Save(dataStream, 0);
            count += _propertyTypeSubjectIndex.Save(dataStream, 0);
            count += _graphIndex.Save(dataStream, 0);
            count += _prefixManager.Save(dataStream, 0);
            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            _storeId = SerializationUtils.ReadVarint(dataStream);
            _nextObjectId = SerializationUtils.ReadVarint(dataStream);
            _resourceIdIndexObjectId = SerializationUtils.ReadVarint(dataStream);
            _objectLocationManager = new ObjectLocationManager();
            _objectLocationManager.Read(dataStream);
            _propertyTypeObjectIndex = new PredicateIndexResourceToObjectIdIndex();
            _propertyTypeObjectIndex.Read(dataStream);
            _propertyTypeSubjectIndex = new PredicateIndexResourceToObjectIdIndex();
            _propertyTypeSubjectIndex.Read(dataStream);
            _graphIndex.Read(dataStream);
            _prefixManager.Read(dataStream);
        }

        public ulong ObjectId
        {
            get { return _storeId; }
            set { _storeId = value; }
        }

        public bool ScheduledForCommit { get; set; }

        IStore IPersistable.Store
        {
            get { return this; }
            set
            {
                // no op.
            }
        }

        public ObjectLocationManager ObjectLocationManager
        {
            get { return _objectLocationManager; }
        }

        #endregion

        #region IStore Members

        /// <summary>
        /// Get an enumeration over all graph URIs in the store
        /// </summary>
        /// <returns>An enumeration of string values where each value is the URI of a graph that was previously added to the store</returns>
        public IEnumerable<string> GetGraphUris()
        {
            return _graphIndex.GetGraphUris();
        }


        public BrightstarSparqlResultsType ExecuteSparqlQuery(SparqlQuery query, ISerializationFormat targetFormat, Stream resultsStream,
                                                              IEnumerable<string> defaultGraphUris = null)
        {
            throw new NotImplementedException();
        }

        public void InsertTriple(string subject, string predicate, string objValue, bool isObjectLiteral,
                                 string dataType, string langCode, string graphUri, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("InsertTriple"))
            {
                // Normalize subject, predicate, objValue (if it is not a literal), dataType (if it is not null)
                // and graphUri (if it is not null)
                try
                {
                    subject = new Uri(subject, UriKind.Absolute).ToString();
                }
                catch (FormatException)
                {
                    throw new InvalidTripleException(
                        String.Format("The subject '{0}' could not be parsed as a valid URI.", subject));

                }

                try
                {
                    predicate = new Uri(predicate, UriKind.Absolute).ToString();
                }
                catch (FormatException)
                {
                    throw new InvalidTripleException(
                        String.Format("The predicate'{0}' could not be parsed as a valid URI.", predicate));
                }

                if (!isObjectLiteral)
                {
                    try
                    {
                        objValue = new Uri(objValue, UriKind.Absolute).ToString();
                    }
                    catch (FormatException)
                    {
                        throw new InvalidTripleException(
                            String.Format("The object '{0}' could not be parsed as a valid URI.", objValue));
                    }
                }

                if (isObjectLiteral && !String.IsNullOrEmpty(dataType))
                {
                    try
                    {
                        dataType = new Uri(dataType, UriKind.Absolute).ToString();
                    }
                    catch (FormatException)
                    {
                        throw new InvalidTripleException(
                            String.Format("The dataType '{0}' could not be parsed as a valid URI.", dataType));
                    }
                }

                if (!String.IsNullOrEmpty(graphUri))
                {
                    try
                    {
                        graphUri = new Uri(graphUri, UriKind.Absolute).ToString();
                    }
                    catch (FormatException)
                    {
                        throw new InvalidTripleException(
                            String.Format("The graphUri '{0}' could not be parsed as a valid URI.", graphUri));
                    }
                }

                if (isObjectLiteral && dataType == null)
                {
                    dataType = RdfDatatypes.PlainLiteral;
                }

                // Normalize language code to lower-case (per http://www.w3.org/TR/rdf-concepts/#section-Graph-Literal)
                if (langCode != null)
                {
                    langCode = langCode.ToLowerInvariant();
                }

                subject = _prefixManager.MakePrefixedUri(subject);
                predicate = _prefixManager.MakePrefixedUri(predicate);

                if (!isObjectLiteral)
                {
                    objValue = _prefixManager.MakePrefixedUri(objValue);
                }

                // 3 inserts into the resource index
                ulong sid = AssertInResourceIndex(subject, false, profiler:profiler);
                ulong pid = AssertInResourceIndex(predicate, false, profiler:profiler);
                ulong oid = AssertInResourceIndex(objValue, isObjectLiteral, dataType, langCode, !isObjectLiteral, profiler);

                // Assert the record of the graph URI
                ulong gid = _graphIndex.AssertInIndex(graphUri);

                // insert into subject property index
                UpdateSubjectPropertyIndex(sid, pid, oid, gid);

                // insert into object property index
                UpdateObjectPropertyIndex(sid, pid, oid, gid);
            }
        }

        public void InsertTriple(Triple triple)
        {
            InsertTriple(triple.Subject, triple.Predicate, triple.Object, triple.IsLiteral,
                         triple.DataType,
                         triple.LangCode,
                         triple.Graph ?? Constants.DefaultGraphUri);
        }

        public void DeleteTriple(Triple triple)
        {
            ulong sid = FindResourceId(triple.Subject);
            ulong pid = FindResourceId(triple.Predicate);
            string dtStr = null;
            if (triple.DataType != null)
            {
                dtStr = triple.DataType;
            }
            else if (triple.DataType == null && triple.IsLiteral)
            {
                dtStr = RdfDatatypes.PlainLiteral;
            }
            string lc = triple.LangCode == null ? null : triple.LangCode.ToLowerInvariant();
            ulong oid = FindResourceId(triple.Object, triple.IsLiteral, dtStr, lc);
            ulong gid = _graphIndex.LookupGraphId(triple.Graph);

            // if we dont have a resource id then it aint in here
            if (sid == StoreConstants.NullUlong || pid == StoreConstants.NullUlong || oid == StoreConstants.NullUlong || gid == StoreConstants.NullUlong) return;

            RemoveSubjectPropertyIndexEntry(sid, pid, oid, gid);
            RemoveObjectPropertyIndexEntry(sid, pid, oid, gid);
        }

        public void Commit(Guid jobId, BrightstarProfiler profiler = null)
        {
            Logging.LogDebug("Store.Commit: {0}", jobId);
            try
            {
                // Add the store to the end of the commit list so that it is the last object to get written.
                AddToCommitList(this);

                // store the objects
                var storeManager = StoreManagerFactory.GetStoreManager() as IStoreManager2;
                storeManager.StoreObjects(_commitList, _objectLocationManager, _storeLocation);

                // the new location of this store object is also updated in the offset index
                ulong storeOffset = _objectLocationManager.GetObjectOffset(ObjectId);

                // append the position in the data file of the store to the master file.
                storeManager.UpdateMasterFile(_storeLocation,
                                          new CommitPoint(storeOffset, 0, DateTime.UtcNow, jobId));

                // clear the commit list
                _commitList = new List<IPersistable>();
                ClearIndexes();
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.StoreCommitException, "Commit failed for job {0} on store {1}. Cause: {2}",
                                 jobId, _storeLocation, ex);
            }
        }

        public void FlushChanges(BrightstarProfiler profiler = null)
        {
            try
            {
                if (_commitList != null && _commitList.Any())
                {
                    // store the objects
                    IStoreManager2 storeManager = GetStoreManager();
                    storeManager.StoreObjects(_commitList, _objectLocationManager, _storeLocation);
                    // clear the commit list
                    _commitList = new List<IPersistable>();
                }
                ClearIndexes();
                Close(); // KA: Required so that caching block providers do not hold stale data.
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.StoreFlushException, "Flush failed for store {0}. Cause: {1}",
                                 _storeLocation, ex);
            }
        }

        public IEnumerable<Triple> GetResourceStatements(string resource, string graphUri = null)
        {
            if (string.IsNullOrEmpty(resource)) return new List<Triple>();
            if (string.IsNullOrEmpty(graphUri)) graphUri = Constants.DefaultGraphUri;

            ulong resourceId = FindResourceId(resource);
            ulong graphId = _graphIndex.LookupGraphId(graphUri);
            if (resourceId == StoreConstants.NullUlong) return new List<Triple>();

            return Bind(resourceId, graphs: new[] {graphId}).Select(MakeTriple);
        }

        public IEnumerable<Triple> Match(string subject,
                                         string predicate,
                                         string obj,
                                         bool isLiteral = false,
                                         string dataType = null,
                                         string langCode = null,
                                         string graph = null)
        {
            return Match(subject, predicate, obj, isLiteral, dataType, langCode, new[] {graph});
        }

        private static readonly List<ulong> AllGraphs = new List<ulong> {StoreConstants.NullUlong};

        private List<ulong> LookupGraphIds(IEnumerable<string> graphs)
        {
            if (graphs == null) return AllGraphs;
            var ret = new List<ulong>(1);
            foreach (var g in graphs)
            {
                var gid = _graphIndex.LookupGraphId(g);
                if (gid != StoreConstants.NullUlong) ret.Add(gid);
            }
            return ret;
        }

        public IEnumerable<Triple> Match(string subject,
                                         string predicate,
                                         string obj,
                                         bool isLiteral = false,
                                         string dataType = null,
                                         string langCode = null,
                                         IEnumerable<string> graphs = null)
        {
            // Normalize language code to lower case (per http://www.w3.org/TR/rdf-concepts/#section-Graph-Literal)
            if (langCode != null) langCode = langCode.ToLowerInvariant();

            ulong sid = FindResourceId(subject);
            ulong pid = FindResourceId(predicate);
            ulong oid = FindResourceId(obj, isLiteral, dataType, langCode);
            var gids = LookupGraphIds(graphs);

            if (sid == StoreConstants.NullUlong && !string.IsNullOrEmpty(subject)) return new List<Triple>();
            if (pid == StoreConstants.NullUlong && !string.IsNullOrEmpty(predicate)) return new List<Triple>();
            if (oid == StoreConstants.NullUlong && !string.IsNullOrEmpty(obj)) return new List<Triple>();

            if (gids.Count == 0)
            {
                return new List<Triple>();
        }

            return Bind(sid, pid, oid, gids).Select(MakeTriple);
        }


        ///// <summary>
        ///// Serialises the SPARQL result as XML into the stream provided.
        ///// </summary>
        ///// <param name="exp">The SPARQL query expression to execute</param>
        ///// <param name="resultsFormat"></param>
        ///// <param name="resultStream">The stream that the SPARQL results will be written to</param>
        ///// <param name="resultsType">Receives an enumeration values specifying the type of SPARQL result being written to the stream</param>
        //public void ExecuteSparqlQuery(string exp, SparqlResultsFormat resultsFormat, Stream resultStream, out BrightstarSparqlResultsType resultsType)
        //{
        //    var queryHandler = new SparqlQueryHandler();
        //    BrightstarSparqlResultSet result = queryHandler.ExecuteSparql(exp, this);

        //    // NOTE: streamWriter is not wrapped in a using because we don't want to close resultStream at this point
        //    var streamWriter = new StreamWriter(resultStream, resultsFormat.Encoding);
        //    resultsType = result.ResultType;
        //    streamWriter.Write(result.GetString(resultsFormat));
        //    streamWriter.Flush();
        //}

        ///// <summary>
        ///// Returns an XML string of the sparql result set.
        ///// </summary>
        ///// <param name="exp"></param>
        ///// <param name="resultsFormat"></param>
        ///// <param name="defaultGraphUris"></param>
        ///// <returns></returns>
        //public string ExecuteSparqlQuery(string exp, SparqlResultsFormat resultsFormat, IEnumerable<string> defaultGraphUris )
        //{
        //    var queryHandler = new SparqlQueryHandler(defaultGraphUris);
        //    BrightstarSparqlResultSet result = queryHandler.ExecuteSparql(exp, this);
        //    return result.GetString(resultsFormat);
        //}

        //public string ExecuteSparqlQuery(string exp, SparqlResultsFormat resultsFormat, out long rowCount)
        //{
        //    var queryHandler = new SparqlQueryHandler();
        //    BrightstarSparqlResultSet result = queryHandler.ExecuteSparql(exp, this);
        //    rowCount = result.Count;
        //    return result.GetString(resultsFormat);
        //}

        public IEnumerable<CommitPoint> GetCommitPoints()
        {
            IStoreManager storeManager = StoreManagerFactory.GetStoreManager();
            return storeManager.GetMasterFile(_storeLocation).GetCommitPoints();
        }

        public void RevertToCommitPoint(CommitPoint commitPoint)
        {
            GetStoreManager().UpdateMasterFile(_storeLocation, commitPoint);
        }

        #endregion

        private void ClearIndexes()
        {
            // No longer need to clear out the cache to reclaim memory as it uses weakrefs
            // _loadedObjects = new ObjectCache(IsReadOnly);
            _resourceIdIndex = null;
        }

        ~Store()
        {
            Close();
            _loadedObjects.Clear();
#if !SILVERLIGHT && !PORTABLE
            RemoveCachedQueries();
#endif
        }

#if !SILVERLIGHT && !PORTABLE
        private void RemoveCachedQueries()
        {
            var directoryInfo = new DirectoryInfo(_tmpPath);
            foreach (var file in directoryInfo.GetFiles().Where(f => f.Name.StartsWith(_instanceId)))
            {
                file.Delete();
            }
        }
#endif

        public void Close()
        {
            try
            {
                if (_inputStream != null)
                {
                    _inputStream.Dispose();
#if PORTABLE
                    System.CloseExtensions.Close(_inputStream);
#else
                    _inputStream.Close();
#endif
                    _inputStream = null;
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.StreamCloseError, "Unable to close store input stream: {0}", ex);
            }
        }

        public IEnumerable<string> GetPredicates(BrightstarProfiler profiler = null)
        {
            throw new NotImplementedException();
        }

        public ulong GetTripleCount(string predicateUri, BrightstarProfiler profiler = null)
        {
            throw new NotImplementedException();
        }

        public void WarmupPageCache(int pagesToPreload, BrightstarProfiler profiler = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// All persisted objects both data and index are given a unique integer id.
        /// </summary>
        internal ulong GetNextObjectId()
        {
            _nextObjectId++;
            return _nextObjectId;
        }

        /// <summary>
        /// Loads and then persists all objects from the current view to a new data file.
        /// This allows the data file size to be reduced.
        /// </summary>
        /// <param name="newDataFile"></param>
        public void Migrate(string newDataFile)
        {
            // iterate all the objects in the object table and write them to a new file.
        }

        private ulong FindResourceId(string resourceValue, bool isLiteral = false, string dataType = null,
                                     string langCode = null)
        {
            if (resourceValue == null) return StoreConstants.NullUlong;
            if (!isLiteral) resourceValue = _prefixManager.MakePrefixedUri(resourceValue);
            return ResourceIdIndex.GetResourceId(resourceValue, isLiteral, dataType, langCode, !isLiteral);
        }

        private ulong AssertInResourceIndex(string resourceValue, bool isLiteral, string dataType = null,
                                            string langCode = null, bool cache = true, BrightstarProfiler profiler = null)
        {
            return ResourceIdIndex.AssertResourceInIndex(resourceValue, isLiteral, dataType, langCode, cache, profiler);
        }

        private void RemoveSubjectPropertyIndexEntry(ulong subjectRid, ulong propertyRid, ulong valueRid, ulong graphRid)
        {
            ulong indexBtreeId;
            if (!_propertyTypeSubjectIndex.TryGetValue(propertyRid, out indexBtreeId))
            {
                // no index so just return
                Logging.LogInfo("No index found for property resource ID {0} in property type subject index.",
                                 propertyRid);
                return;
            }
            var indexTree = LoadObject<PersistentBTree<ObjectRef>>(indexBtreeId);

            // we have the btree for the index
            Node<ObjectRef> n;
            Entry<ObjectRef> e;
            indexTree.LookupEntry(subjectRid, out e, out n);

            if (e == null)
            {
                // log and return as we expect something to be present
                Logging.LogInfo(
                    "No entry found for subject resource ID {0} in the property type subject index for property type resource ID {1}",
                    subjectRid, propertyRid);
                return;
            }
            var resourceList = LoadObject<RelatedResourceList>(e.Value.ObjectId);
            resourceList.RemoveRelatedResource(valueRid, graphRid);

            if (resourceList.Root.Keys.Count == 0)
            {
                indexTree.Delete(e);
                _objectLocationManager.DeleteObjectOffset(resourceList.ObjectId);
            }
            else
            {
            AddToCommitList(resourceList);
        }
        }

        private RelatedResourceList MakeNewRelatedResourceList()
        {
            var list = new RelatedResourceList(GetNextObjectId(), TreeNodeSize, this);
            _loadedObjects.Add(list);
            return list;
        }

        private void UpdateSubjectPropertyIndex(ulong subjectRid, ulong propertyRid, ulong valueRid, ulong graphRid)
        {
            PersistentBTree<ObjectRef> indexTree;
            ulong indexBtreeId;
            if (!_propertyTypeSubjectIndex.TryGetValue(propertyRid, out indexBtreeId))
            {
                indexTree = MakeNewTree<ObjectRef>(TreeNodeSize);
                _propertyTypeSubjectIndex.InsertEntry(propertyRid, indexTree.ObjectId);
                RelatedResourceList resourceList = MakeNewRelatedResourceList();
                resourceList.AddRelatedResource(valueRid, graphRid);
                indexTree.Insert(new Entry<ObjectRef>(subjectRid, new ObjectRef(resourceList.ObjectId)));
                AddToCommitList(resourceList);
                return;
            }
            indexTree = LoadObject<PersistentBTree<ObjectRef>>(indexBtreeId);

            // we have the btree for the index
            Node<ObjectRef> n;
            Entry<ObjectRef> e;
            indexTree.LookupEntry(subjectRid, out e, out n);
            AddToCommitList(indexTree);

            if (e == null)
            {
                RelatedResourceList resourceList = MakeNewRelatedResourceList();
                resourceList.AddRelatedResource(valueRid, graphRid);
                indexTree.Insert(new Entry<ObjectRef>(subjectRid, new ObjectRef(resourceList.ObjectId)));
                AddToCommitList(resourceList);
            }
            else
            {
                var resourceList = LoadObject<RelatedResourceList>(e.Value.ObjectId);
                resourceList.AddRelatedResource(valueRid, graphRid);
                AddToCommitList(resourceList);
            }
        }

        private void RemoveObjectPropertyIndexEntry(ulong subjectRid, ulong propertyRid, ulong valueRid, ulong graphRid)
        {
            ulong indexBtreeId;
            if (!_propertyTypeObjectIndex.TryGetValue(propertyRid, out indexBtreeId)) return;
            var indexTree = LoadObject<PersistentBTree<ObjectRef>>(indexBtreeId);

            // we have the btree for the index
            Node<ObjectRef> n;
            Entry<ObjectRef> e;
            indexTree.LookupEntry(valueRid, out e, out n);

            if (e == null)
            {
                return;
            }

            var resourceList = LoadObject<RelatedResourceList>(e.Value.ObjectId);
            resourceList.RemoveRelatedResource(subjectRid, graphRid);

            if (resourceList.Root.Keys.Count == 0)
            {
                indexTree.Delete(e);
                _objectLocationManager.DeleteObjectOffset(resourceList.ObjectId);
            }
            else
            {
            AddToCommitList(resourceList);
            }
            // AddToCommitList(n);
        }

        private void UpdateObjectPropertyIndex(ulong subjectRid, ulong propertyRid, ulong valueRid, ulong graphRid)
        {
            PersistentBTree<ObjectRef> indexTree;
            ulong indexBtreeId;
            if (!_propertyTypeObjectIndex.TryGetValue(propertyRid, out indexBtreeId))
            {
                indexTree = MakeNewTree<ObjectRef>(TreeNodeSize);
                // _propertyTypeObjectIndex[propertyRid] = indexTree.ObjectId;
                _propertyTypeObjectIndex.InsertEntry(propertyRid, indexTree.ObjectId);

                RelatedResourceList resourceList = MakeNewRelatedResourceList();
                resourceList.AddRelatedResource(subjectRid, graphRid);
                indexTree.Insert(new Entry<ObjectRef>(valueRid, new ObjectRef(resourceList.ObjectId)));
                AddToCommitList(resourceList);
                return;
            }

            // we have the btree for the index
            indexTree = LoadObject<PersistentBTree<ObjectRef>>(indexBtreeId);
            Node<ObjectRef> n;
            Entry<ObjectRef> e;
            indexTree.LookupEntry(valueRid, out e, out n);
            AddToCommitList(indexTree);

            if (e == null)
            {
                RelatedResourceList resourceList = MakeNewRelatedResourceList();
                resourceList.AddRelatedResource(subjectRid, graphRid);
                indexTree.Insert(new Entry<ObjectRef>(valueRid, new ObjectRef(resourceList.ObjectId)));
                AddToCommitList(resourceList);
            }
            else
            {
                var resourceList = LoadObject<RelatedResourceList>(e.Value.ObjectId);
                resourceList.AddRelatedResource(subjectRid, graphRid);
                AddToCommitList(resourceList);
            }
        }


        /// <summary>
        /// Looks in the object offset index to find the offset position for the provided object
        /// id. 
        /// </summary>
        /// <typeparam name="T">The type of object to be loaded.</typeparam>
        /// <param name="objectId">The object id</param>
        /// <returns>Loaded object of type T or throws exception if no offset for the object id can be found.</returns>
        public T LoadObject<T>(ulong objectId) where T : class, IPersistable
        {
            // see if object is in the cache
            IPersistable obj;
            if (_loadedObjects.TryGetValue(objectId, out obj)) return (T) obj;

            ulong offset = _objectLocationManager.GetObjectOffset(objectId);
            IStoreManager2 storeManager = GetStoreManager();
            obj = storeManager.ReadObject<T>(InputDataStream, offset);

            // KA: Added this test to try and force an early fail if the data read from the store is corrupted somehow.
            if (obj.ObjectId != objectId)
            {
                Logging.LogError(BrightstarEventId.ObjectReadError,
                    "Invalid object detected in LoadObject({0}). Attempted to load object from offset {1} and read in an object with ObjectId set to {2}.", 
                    objectId, offset, obj.ObjectId);
                throw new BrightstarInternalException(String.Format("Invalid object detected in LoadObject({0}). Attempted to load object from offset {1} and read in an object with ObjectId set to {2}.", objectId, offset, obj.ObjectId));
            }

            _loadedObjects.Add(obj);
            obj.Store = this;
            return (T) obj;
        }

        /// <summary>
        /// Called by the btree's that are part of this store.
        /// </summary>
        /// <param name="obj">object being persisted</param>
        internal void AddToCommitList(IPersistable obj)
        {
            if (_loadedObjects != null)
            {
                // Ensure that we have this object in the cache
                _loadedObjects.Add(obj);
            }

            if (!obj.ScheduledForCommit)
            {
                CommitList.Add(obj);
                obj.ScheduledForCommit = true;
            }
        }

        public PersistentBTree<T> MakeNewTree<T>(int keyCount) where T : class, IStorable
        {
            var tree = new PersistentBTree<T>(GetNextObjectId(), keyCount, this);
            AddToCommitList(tree);
            _loadedObjects.Add(tree);
            return tree;
        }


        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> Bind(ulong s = StoreConstants.NullUlong, ulong p = StoreConstants.NullUlong,
                                                                    ulong o = StoreConstants.NullUlong,
                                                                    IEnumerable<ulong> graphs = null)
        {
            if (s != StoreConstants.NullUlong && p != StoreConstants.NullUlong && o != StoreConstants.NullUlong)
            {
                return BindSubjectPredicateObject(s, p, o, graphs);
            }
            if (s != StoreConstants.NullUlong && p != StoreConstants.NullUlong)
            {
                return BindSubjectPredicate(s, p, graphs);
            }
            if (s != StoreConstants.NullUlong && o != StoreConstants.NullUlong)
            {
                return BindSubjectObject(s, o, graphs);
            }
            if (p != StoreConstants.NullUlong && o != StoreConstants.NullUlong)
            {
                return BindPredicateObject(p, o, graphs);
            }
            if (s != StoreConstants.NullUlong)
            {
                return BindSubject(s, graphs);
            }
            if (p != StoreConstants.NullUlong)
            {
                return BindPredicate(p, graphs);
            }
            if (o != StoreConstants.NullUlong)
            {
                return BindObject(o, graphs);
            }
            return BindAll(graphs);
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindAll(IEnumerable<ulong> graphs)
        {
            foreach (
                PredicateIndexResourceToObjectIdIndex.Entry subjectPredicateIndexEntry in
                    _propertyTypeSubjectIndex.Entries)
            {
                ulong predicateId = subjectPredicateIndexEntry.ResourceId;
                var subjectPredicateIndex =
                    LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexEntry.IndexObjectId);
                foreach (var resourceListRef in subjectPredicateIndex.InOrderTraversal())
                {
                    ulong subjectResourceId = resourceListRef.Key;
                    var resourceList = LoadObject<RelatedResourceList>(resourceListRef.Value.ObjectId);
                    foreach (RelatedResource resource in resourceList.Members)
                    {
                        if (graphs.Any(g => g == StoreConstants.NullUlong))
                        {
                            foreach (var gid in resource.Graph)
                            {
                            yield return new Tuple<ulong, ulong, ulong, ulong>(
                                    subjectResourceId, predicateId, resource.Rid, gid);
                        }
                        }
                        else
                        {
                            foreach (var gid in resource.Graph.Intersect(graphs))
                            {
                                yield return new Tuple<ulong, ulong, ulong, ulong>(
                                    subjectResourceId, predicateId, resource.Rid, gid);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindObject(ulong o, IEnumerable<ulong> graphs)
        {
            foreach (
                PredicateIndexResourceToObjectIdIndex.Entry objectPredicateIndexEntry in
                    _propertyTypeObjectIndex.Entries)
            {
                ulong predicateId = objectPredicateIndexEntry.ResourceId;
                var objectPredicateIndex =
                    LoadObject<PersistentBTree<ObjectRef>>(objectPredicateIndexEntry.IndexObjectId);
                ObjectRef resourceListRef = objectPredicateIndex.FindValue(o);
                if (resourceListRef == null) continue;
                var relatedResourceList = LoadObject<RelatedResourceList>(resourceListRef.ObjectId);

                foreach (var resource in relatedResourceList.Members)
                    {
                    if (graphs.Any(g => g == StoreConstants.NullUlong))
                    {
                        foreach (var gid in resource.Graph)
                        {
                            yield return new Tuple<ulong, ulong, ulong, ulong>(resource.Rid, predicateId, o, gid);
                        }
                    }
                    else
                    {
                        foreach (var gid in resource.Graph.Intersect(graphs))
                        {
                            yield return new Tuple<ulong, ulong, ulong, ulong>(resource.Rid, predicateId, o, gid);
                    }
                }
            }
        }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindPredicate(ulong p, IEnumerable<ulong> graphs)
        {
            ulong subjectPredicateIndexId;
            _propertyTypeSubjectIndex.TryGetValue(p, out subjectPredicateIndexId);
            var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);
            foreach (var indexEntry in subjectPredicateIndex.InOrderTraversal())
            {
                ulong subjectId = indexEntry.Key;
                ulong resourceListId = indexEntry.Value.ObjectId;
                var relatedResourceList = LoadObject<RelatedResourceList>(resourceListId);
                foreach (var resource in relatedResourceList.Members)
                {
                    if (graphs.Any(g => g == StoreConstants.NullUlong))
                    {
                        foreach (var gid in resource.Graph)
                        {
                            yield return new Tuple<ulong, ulong, ulong, ulong>(subjectId, p, resource.Rid, gid);
                        }
                    }
                    else
                    {
                        foreach (var gid in resource.Graph.Intersect(graphs))
                        {
                            yield return new Tuple<ulong, ulong, ulong, ulong>(subjectId, p, resource.Rid, gid);
                    }
                }
            }
        }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindSubject(ulong s, IEnumerable<ulong> graphs)
        {
            foreach (
                PredicateIndexResourceToObjectIdIndex.Entry subjectPredicateIndexEntry in
                    _propertyTypeSubjectIndex.Entries)
            {
                ulong subjectPredicateIndexId = subjectPredicateIndexEntry.IndexObjectId;
                var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);
                ObjectRef objectRef = subjectPredicateIndex.FindValue(s);
                if (objectRef == null) continue;
                var relatedResourceList = LoadObject<RelatedResourceList>(objectRef.ObjectId);

                foreach (var resource in relatedResourceList.Members)
                {
                    if (graphs.Any(g => g == StoreConstants.NullUlong))
                    {
                        foreach (var gid in resource.Graph)
                        {
                            yield return
                                new Tuple<ulong, ulong, ulong, ulong>(s, subjectPredicateIndexEntry.ResourceId,
                                                                      resource.Rid, gid);
                        }
                    }
                    else
                    {
                        foreach (var gid in resource.Graph.Intersect(graphs))
                        {
                        yield return
                                new Tuple<ulong, ulong, ulong, ulong>(s, subjectPredicateIndexEntry.ResourceId,
                                                                      resource.Rid, gid);
                    }
                }
            }
        }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindPredicateObject(ulong p, ulong o,
                                                                                   IEnumerable<ulong> graphs)
        {
            ulong objectPredicateIndexId = _propertyTypeObjectIndex.GetObjectId(p);
            var objectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(objectPredicateIndexId);
            ObjectRef objectRef = objectPredicateIndex.FindValue(o);
            if (objectRef == null) yield break;
            var relatedResourceList = LoadObject<RelatedResourceList>(objectRef.ObjectId);

            foreach (var resource in relatedResourceList.Members)
            {
                if (graphs.Any(g => g == StoreConstants.NullUlong))
                {
                    foreach (var gid in resource.Graph)
                    {
                        yield return new Tuple<ulong, ulong, ulong, ulong>(resource.Rid, p, o, gid);
                    }
                }
                else
                {
                    foreach (var gid in resource.Graph.Intersect(graphs))
                    {
                        yield return new Tuple<ulong, ulong, ulong, ulong>(resource.Rid, p, o, gid);
                }
            }
        }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindSubjectObject(ulong s, ulong o,
                                                                                 IEnumerable<ulong> graphs)
        {
            foreach (
                PredicateIndexResourceToObjectIdIndex.Entry subjectPredicateIndexEntry in
                    _propertyTypeSubjectIndex.Entries)
            {
                ulong subjectPredicateIndexId = subjectPredicateIndexEntry.IndexObjectId;
                var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);
                ObjectRef objectRef = subjectPredicateIndex.FindValue(s);
                if (objectRef == null) continue;
                var relatedResourceList = LoadObject<RelatedResourceList>(objectRef.ObjectId);
                foreach (
                    RelatedResource resource in relatedResourceList.Members.Where(resource => resource.Rid.Equals(o)))
                {
                    if (graphs.Any(g => g == StoreConstants.NullUlong))
                    {
                        foreach (var gid in resource.Graph)
                        {
                            yield return
                                new Tuple<ulong, ulong, ulong, ulong>(s, subjectPredicateIndexEntry.ResourceId, o, gid);
                        }
                    }
                    else
                    {
                        foreach (var gid in resource.Graph.Intersect(graphs))
                        {
                            yield return
                                new Tuple<ulong, ulong, ulong, ulong>(s, subjectPredicateIndexEntry.ResourceId, o, gid);
                    }
                }
            }
        }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindSubjectPredicate(ulong s, ulong p,
                                                                                    IEnumerable<ulong> graphs)
        {
            // get the predicate index
            ulong subjectPredicateIndexId = _propertyTypeSubjectIndex.GetObjectId(p);
            var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);

            ObjectRef objectRef = subjectPredicateIndex.FindValue(s);
            if (objectRef == null) yield break;
            var relatedResourceList = LoadObject<RelatedResourceList>(objectRef.ObjectId);
            foreach (RelatedResource resource in relatedResourceList.Members)
            {
                if (graphs.Any(g => g == StoreConstants.NullUlong))
                {
                    foreach (var gid in resource.Graph)
                    {
                        yield return new Tuple<ulong, ulong, ulong, ulong>(s, p, resource.Rid, gid);
                    }
                }
                else
                {
                    foreach (var gid in resource.Graph.Intersect(graphs))
                    {
                        yield return new Tuple<ulong, ulong, ulong, ulong>(s, p, resource.Rid, gid);
                }
            }
        }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, ulong>> BindSubjectPredicateObject(ulong s, ulong p, ulong o,
                                                                                          IEnumerable<ulong> graphs)
        {
            // get the predicate index
            ulong subjectPredicateIndexId = _propertyTypeSubjectIndex.GetObjectId(p);
            var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);

            // complete match lookup
            var objectRef = subjectPredicateIndex.FindValue(s);
            if (objectRef == null) yield break;
            var relatedResourceList = LoadObject<RelatedResourceList>(objectRef.ObjectId);
            var resource = relatedResourceList.FindValue(o);
            if (resource != null)
            {
                if (graphs.Any(g => g == StoreConstants.NullUlong))
                {
                    foreach (var gid in resource.Graph)
                    {
                        yield return new Tuple<ulong, ulong, ulong, ulong>(s, p, o, gid);
                    }
                }
                else
                {
                    foreach (var gid in resource.Graph.Intersect(graphs))
                    {
                        yield return new Tuple<ulong, ulong, ulong, ulong>(s, p, o, gid);
                }
            }
        }
        }

        internal Resource Resolve(ulong resourceId)
        {
            return ResourceIdIndex.GetResource(resourceId);
        }

        public string ResolvePrefixedUri(string prefixedUri)
        {
            return _prefixManager.ResolvePrefixedUri(prefixedUri);
        }

        private Triple MakeTriple(Tuple<ulong, ulong, ulong, ulong> data)
        {
            string subject = _prefixManager.ResolvePrefixedUri(Resolve(data.Item1).LexicalValue);
            string predicate = _prefixManager.ResolvePrefixedUri(Resolve(data.Item2).LexicalValue);
            Resource obj = Resolve(data.Item3);
            string graph = _graphIndex.GetGraphUri((int) data.Item4);

            if (obj.IsLiteral)
            {
                Resource dataType = Resolve(obj.DataTypeResourceId);
                return new Triple
                           {
                               Subject = subject,
                               Predicate = predicate,
                               Object = obj.LexicalValue,
                               DataType = dataType.LexicalValue,
                               LangCode = obj.LanguageCode,
                               IsLiteral = true,
                               Graph = graph
                           };
            }
            return new Triple
                       {
                           Subject = subject,
                           Predicate = predicate,
                           Object = _prefixManager.ResolvePrefixedUri(obj.LexicalValue),
                           Graph = graph
                       };
        }

        public void Triple(string subject, string predicate, string obj, bool isLiteral, string dataType,
                           string langCode, string graphUri)
        {
            InsertTriple(subject, predicate, obj, isLiteral, dataType, langCode, Constants.DefaultGraphUri);
        }

        public void AddNewObject(IPersistable newObject)
        {
            _loadedObjects.Add(newObject);
            AddToCommitList(newObject);
        }

        public IEnumerable<ulong> GetMatchEnumeration(string subject, string predicate, string obj, bool isLiteral,
                                                      string dataType, string langCode, IEnumerable<string> graphs)
        {
            // Normalize language code to lower case (per http://www.w3.org/TR/rdf-concepts/#section-Graph-Literal)
            if (langCode != null) langCode = langCode.ToLowerInvariant();

            ulong sid = FindResourceId(subject);
            ulong pid = FindResourceId(predicate);
            ulong oid = FindResourceId(obj, isLiteral, dataType, langCode);
            IEnumerable<ulong> gids = graphs == null
                                          ? new ulong[] {StoreConstants.NullUlong}
                                          : graphs.Select(g => _graphIndex.LookupGraphId(g));


            if (sid == StoreConstants.NullUlong && !string.IsNullOrEmpty(subject)) yield break;
            if (pid == StoreConstants.NullUlong && !string.IsNullOrEmpty(predicate)) yield break;
            if (oid == StoreConstants.NullUlong && !string.IsNullOrEmpty(obj)) yield break;
            if (gids.All(g => StoreConstants.NullUlong == g) || !gids.Any())
            {
                yield break;
            }

            IEnumerable<ulong> ret = null;
            if (subject == null)
            {
                ret = Bind(sid, pid, oid, gids).Select(x => x.Item1);
            }
            else if (predicate == null)
            {
                ret = Bind(sid, pid, oid, gids).Select(x => x.Item2).OrderBy(x => x);
            }
            else if (obj == null)
            {
                ret = Bind(sid, pid, oid, gids).Select(x => x.Item3);
            }
            if (ret == null) yield break;
            foreach (var v in ret) yield return v;
        }

        public IEnumerable<ulong[]> GetSubjectObjectMatchEnumeration(string predicate, IEnumerable<string> graphUris)
        {
            ulong pid = FindResourceId(predicate);
            if (pid == StoreConstants.NullUlong) yield break;

            IEnumerable<ulong> gids = graphUris.Select(g => _graphIndex.LookupGraphId(g)).ToList();
            if (gids.All(g => g == StoreConstants.NullUlong)) yield break;

            ulong subjectPredicateIndexId;
            _propertyTypeSubjectIndex.TryGetValue(pid, out subjectPredicateIndexId);
            var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);
            foreach (var indexEntry in subjectPredicateIndex.InOrderTraversal())
            {
                ulong subjectId = indexEntry.Key;
                ulong resourceListId = indexEntry.Value.ObjectId;
                var relatedResourceList = LoadObject<RelatedResourceList>(resourceListId);
                foreach (
                    RelatedResource resource in
                        relatedResourceList.Members.Where(r => r.Graph.Any(g => gids.Contains(g))))
                {
                    yield return new[] {subjectId, resource.Rid};
                }
            }
        }

        public IEnumerable<ulong[]> GetObjectSubjectMatchEnumeration(string predicate, IEnumerable<string> graphUris)
        {
            ulong pid = FindResourceId(predicate);
            if (pid == StoreConstants.NullUlong) yield break;

            IEnumerable<ulong> gids = graphUris.Select(g => _graphIndex.LookupGraphId(g)).ToList();
            if (gids.All(g => g == StoreConstants.NullUlong)) yield break;

            ulong objectPredicateIndexId;
            _propertyTypeObjectIndex.TryGetValue(pid, out objectPredicateIndexId);
            var objectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(objectPredicateIndexId);
            foreach (var indexEntry in objectPredicateIndex.InOrderTraversal())
            {
                ulong objectId = indexEntry.Key;
                ulong resourceListId = indexEntry.Value.ObjectId;
                var relatedResourceList = LoadObject<RelatedResourceList>(resourceListId);
                foreach (var resource in relatedResourceList.Members.Where(r => r.Graph.Any(g => gids.Contains(g))))
                {
                    yield return new[] {objectId, resource.Rid};
                }
            }
        }

        /// <summary>
        /// Returns true if <paramref name="graphUris"/> matches all the graph URIs known to the store
        /// </summary>
        /// <param name="graphUris"></param>
        /// <returns></returns>
        private bool IsAllGraphs(IEnumerable<string> graphUris)
        {
            var allGraphs = GetGraphUris().ToList();
            return (allGraphs.Count.Equals(graphUris.Count()) && graphUris.All(allGraphs.Contains));
        }

        /* No longer used
        public IEnumerable<ulong[]> EnumerateSubjectsForPredicate(string predicate, IEnumerable<string> graphUris, bool subjectFirstInSortOrder )
        {
            ulong pid = FindResourceId(predicate);
            if (pid == StoreConstants.NullUlong)
            {
                // Unknown predicate, so there cannot be any matches
                yield break;
            }
            
            if (IsAllGraphs(graphUris))
            {
                // We can optimize by walking the keys in the predicateSubjectIndex
                ulong subjectPredicateIndexId;
                if (_propertyTypeSubjectIndex.TryGetValue(pid, out subjectPredicateIndexId))
                {
                    var subjectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(subjectPredicateIndexId);
                    foreach(var indexEntry in subjectPredicateIndex.InOrderTraversal())
                    {
                        yield return
                            subjectFirstInSortOrder
                                ? new[] {indexEntry.Key, StoreConstants.NullUlong}
                                : new[] {StoreConstants.NullUlong, indexEntry.Key};
                    }
                }
            }

            // Could not optimize because we are being asked for a subset of graphs or we couldn't find the correct index
            var results = subjectFirstInSortOrder
                              ? GetSubjectObjectMatchEnumeration(predicate, graphUris)
                              : GetObjectSubjectMatchEnumeration(predicate, graphUris);
            foreach (var result in results)
            {
                yield return result;
            }
        }
        */

        /* No longer used
        public IEnumerable<ulong[]> EnumerateObjectsForPredicate(string predicate, IEnumerable<string> graphUris, bool objectFirstInSortOrder)
        {
            ulong pid = FindResourceId(predicate);
            if (pid == StoreConstants.NullUlong)
            {
                // Unknown predicate, so there cannot be any matches
                yield break;
            }

            if (IsAllGraphs(graphUris))
            {
                // We can optimize by just walking the keys in the predicateObjectIndex
                ulong objectPredicateIndexId;
                if (_propertyTypeObjectIndex.TryGetValue(pid, out objectPredicateIndexId))
                {
                    var objectPredicateIndex = LoadObject<PersistentBTree<ObjectRef>>(objectPredicateIndexId);
                    foreach (var indexEntry in objectPredicateIndex.InOrderTraversal())
                    {
                        yield return
                            objectFirstInSortOrder
                                ? new[] {indexEntry.Key, StoreConstants.NullUlong}
                                : new[] {StoreConstants.NullUlong, indexEntry.Key};
                    }
                }
            }
            // We cannot easily optimize because we are being asked for a subset of the graphs
            // Fall back on the normal object/subject enumeration
            if (objectFirstInSortOrder)
            {
                foreach (var result in GetObjectSubjectMatchEnumeration(predicate, graphUris))
                {
                    yield return result;
                }
            }
            else
            {
                foreach (var result in GetSubjectObjectMatchEnumeration(predicate, graphUris))
                {
                    yield return result;
                }
            }
        }
        */

        public IEnumerable<ulong[]> GetPredicateSubjectMatchEnumeration(string value, bool isLiteral, string dataType,
                                                                        string languageCode,
                                                                        IEnumerable<string> graphUris)
        {
            ulong oid = FindResourceId(value, isLiteral, dataType, languageCode);
            if (oid == StoreConstants.NullUlong) yield break;

            IEnumerable<ulong> gids = graphUris.Select(g => _graphIndex.LookupGraphId(g)).ToList();
            if (gids.All(g => g == StoreConstants.NullUlong)) yield break;

            foreach (var entry in _propertyTypeObjectIndex.Entries)
            {
                var predicateResourceId = entry.ResourceId;
                var index = LoadObject<PersistentBTree<ObjectRef>>(entry.IndexObjectId);
                var relatedResourceListRef = index.FindValue(oid);
                if (relatedResourceListRef != null)
                {
                    var relatedResourceList = LoadObject<RelatedResourceList>(relatedResourceListRef.ObjectId);
                    foreach (var resource in relatedResourceList.Members.Where(r => r.Graph.Any(g => gids.Contains(g))))
                    {
                        yield return new[] {predicateResourceId, resource.Rid};
                    }
                }
            }
        }

        public IEnumerable<ulong[]> GetPredicateObjectMatchEnumeration(string value, IEnumerable<string> graphUris)
        {
            ulong sid = FindResourceId(value);
            if (sid == StoreConstants.NullUlong) yield break;
            IEnumerable<ulong> gids = graphUris.Select(g => _graphIndex.LookupGraphId(g)).ToList();
            if (gids.All(g => g == StoreConstants.NullUlong)) yield break;

            foreach (var entry in _propertyTypeSubjectIndex.Entries)
            {
                var predicateResourceId = entry.ResourceId;
                var index = LoadObject<PersistentBTree<ObjectRef>>(entry.IndexObjectId);
                var relatedResourceListRef = index.FindValue(sid);
                if (relatedResourceListRef != null)
                {
                    var relatedResourceList = LoadObject<RelatedResourceList>(relatedResourceListRef.ObjectId);
                    foreach (var resource in relatedResourceList.Members.Where(r => r.Graph.Any(g => gids.Contains(g))))
                    {
                        yield return new[] {predicateResourceId, resource.Rid};
                    }
                }
            }
        }

        public IEnumerable<ulong[]> MatchAllTriples(IEnumerable<string> graphUris)
        {
            IEnumerable<ulong> gids = graphUris.Select(g => _graphIndex.LookupGraphId(g)).ToList();
            if (gids.All(g => g == StoreConstants.NullUlong)) yield break;
            foreach (var entry in BindAll(gids))
            {
                yield return new[] {entry.Item2, entry.Item1, entry.Item3};
            }
        }

        public void DeleteGraph(string graphUri)
        {
            ulong gid = _graphIndex.LookupGraphId(graphUri);
            if (gid == StoreConstants.NullUlong) return;
            Func<Entry<ObjectRef>, List<Entry<ObjectRef>>, bool> removeGraph =
                (resourceListRef, emptiedEntries) =>
                    {
                        var modified = false;
                        var resourceList =
                            LoadObject<RelatedResourceList>(resourceListRef.Value.ObjectId);
                        foreach (RelatedResource resource in resourceList.Members.ToList())
                        {
                            if (resource.Graph.Contains(gid))
                            {
                                resource.Graph.Remove(gid);
                                modified = true;
    }
                            if (resource.Graph.Count == 0)
                            {
                                resourceList.Delete(resource.Rid);
                            }
                        }
                        if (resourceList.Root.Keys.Count == 0)
                        {
                            emptiedEntries.Add(resourceListRef);
                        }
                        return modified;
                    };

            VisitResourceLists(_propertyTypeObjectIndex, removeGraph);
            VisitResourceLists(_propertyTypeSubjectIndex, removeGraph);


        }

        public void DeleteGraphs(IEnumerable<string> graphUris)
        {
#if WINDOWS_PHONE || PORTABLE
            var graphUriSet = new VDS.RDF.HashSet<ulong>(graphUris.Select(g => _graphIndex.LookupGraphId(g)));
#else
            var graphUriSet = new HashSet<ulong>(graphUris.Select(g=>_graphIndex.LookupGraphId(g)));
#endif
            if (graphUriSet.All(g => g == StoreConstants.NullUlong)) return;
            Func<Entry<ObjectRef>, List<Entry<ObjectRef>>, bool> removeGraphs =
                (resourceListRef, emptiedEntries) =>
                    {
                        var modified = false;
                        var resourceList = LoadObject<RelatedResourceList>(resourceListRef.Value.ObjectId);
                        foreach (var resource in resourceList.Members.ToList())
                        {
                            var countBefore = resource.Graph.Count;
                            resource.Graph.RemoveAll(x => graphUriSet.Contains(x));
                            modified |= countBefore > resource.Graph.Count;
                            if (resource.Graph.Count == 0)
                            {
                                resourceList.Delete(resource.Rid);
                            }
                        }
                        if (resourceList.Root.Keys.Count == 0)
                        {
                            emptiedEntries.Add(resourceListRef);
                        }
                        return modified;
                    };
            VisitResourceLists(_propertyTypeObjectIndex,removeGraphs);
            VisitResourceLists(_propertyTypeSubjectIndex, removeGraphs);
        }

        public void CopyGraph(string srcGraphUri, string targetGraphUri)
        {
            var srcGid = _graphIndex.LookupGraphId(srcGraphUri);
            var targetGid = _graphIndex.LookupGraphId(targetGraphUri);
            if (srcGid == StoreConstants.NullUlong) return;
            if (targetGid == StoreConstants.NullUlong)
            {
                targetGid = _graphIndex.AssertInIndex(targetGraphUri);
            }

            Func<Entry<ObjectRef>, List<Entry<ObjectRef>>, bool> copyTriples =
                (resourceListRef, emptiedEntries) =>
                    {
                        var modified = false;
                        var resourceList = LoadObject<RelatedResourceList>(resourceListRef.Value.ObjectId);
                        foreach (var resource in resourceList.Members.ToList())
                        {
                            if (resource.Graph.Contains(targetGid))
                            {
                                resourceList.RemoveRelatedResource(resource.Rid, targetGid);
                                modified = true;
                            }
                            if (resource.Graph.Contains(srcGid))
                            {
                                resourceList.AddRelatedResource(resource.Rid, targetGid);
                                modified = true;
                            }
                            /*
                            if (resource.Graph.Count == 0)
                            {
                                resourceList.Delete(resource.Rid);
                                AddToCommitList(resourceList);
                                modified = true;
                            }
                             */
                        }
                        if (resourceList.Root.Keys.Count == 0)
                        {
                            emptiedEntries.Add(resourceListRef);
                        }
                        return modified;
                    };
            VisitResourceLists(_propertyTypeObjectIndex, copyTriples);
            VisitResourceLists(_propertyTypeSubjectIndex, copyTriples);
        }

        public void MoveGraph(string srcGraphUri, string targetGraphUri)
        {
            var srcGid = _graphIndex.LookupGraphId(srcGraphUri);
            var targetGid = _graphIndex.LookupGraphId(targetGraphUri);
            if (srcGid == StoreConstants.NullUlong) return;
            if (targetGid == StoreConstants.NullUlong)
            {
                targetGid = _graphIndex.AssertInIndex(targetGraphUri);
            }
            Func<Entry<ObjectRef>, List<Entry<ObjectRef>>, bool> moveTriples =
                (resourceListRef, emptiedEntries) =>
                    {
                        var modified = false;
                        var resourceList = LoadObject<RelatedResourceList>(resourceListRef.Value.ObjectId);
                        foreach (var resource in resourceList.Members.ToList())
                        {
                            if (resource.Graph.Contains(targetGid))
                            {
                                resourceList.RemoveRelatedResource(resource.Rid, targetGid);
                                modified = true;
                            }
                            if (resource.Graph.Contains(srcGid))
                            {
                                resourceList.AddRelatedResource(resource.Rid, targetGid);
                                resourceList.RemoveRelatedResource(resource.Rid, srcGid);
                                modified = true;
                            }
                            //if (resource.Graph.Count == 0)
                            //{
                            //    resourceList.Delete(resource.Rid);
                            //    modified = true;
                            //}
                        }
                        if (resourceList.Root.Keys.Count == 0)
                        {
                            emptiedEntries.Add(resourceListRef);
                        }
                        return modified;
                    };
            VisitResourceLists(_propertyTypeObjectIndex, moveTriples);
            VisitResourceLists(_propertyTypeSubjectIndex, moveTriples);
        }

        public void AddGraph(string srcGraphUri, string targetGraphUri)
        {
            var srcGid = _graphIndex.LookupGraphId(srcGraphUri);
            var targetGid = _graphIndex.LookupGraphId(targetGraphUri);
            if (srcGid == StoreConstants.NullUlong) return;
            if (targetGid == StoreConstants.NullUlong)
            {
                targetGid = _graphIndex.AssertInIndex(targetGraphUri);
            }
            Func<Entry<ObjectRef>, List<Entry<ObjectRef>>, bool> addTriples =
                (resourceListRef, emptiedEntries) =>
                    {
                        var modified = false;
                        var resourceList = LoadObject<RelatedResourceList>(resourceListRef.Value.ObjectId);
                        foreach (var resource in resourceList.Members.ToList())
                        {
                            if (resource.Graph.Contains(srcGid) && !resource.Graph.Contains(targetGid))
                            {
                                resourceList.AddRelatedResource(resource.Rid, targetGid);
                                modified = true;
                            }
                        }
                        return modified;
                    };
            VisitResourceLists(_propertyTypeObjectIndex, addTriples);
            VisitResourceLists(_propertyTypeSubjectIndex, addTriples);
        }

        private void VisitResourceLists(PredicateIndexResourceToObjectIdIndex rootIndex,
                                        Func<Entry<ObjectRef>, List<Entry<ObjectRef>>, bool> visitAction)
        {
            foreach (PredicateIndexResourceToObjectIdIndex.Entry predicateIndexEntry in
                rootIndex.Entries)
            {
                var predicateIndex =
                    LoadObject<PersistentBTree<ObjectRef>>(predicateIndexEntry.IndexObjectId);
                var emptyEntries = new List<Entry<ObjectRef>>();
                foreach (var resourceListRef in predicateIndex.InOrderTraversal())
                {
                    if (visitAction(resourceListRef, emptyEntries))
                    {
                        // Is this required ?
                        //AddToCommitList(predicateIndex);
                    }
                }
                foreach (var emptyEntry in emptyEntries)
                {
                    predicateIndex.Delete(emptyEntry);
                    _objectLocationManager.DeleteObjectOffset(emptyEntry.Value.ObjectId);
                }
            }
        }

    }
}