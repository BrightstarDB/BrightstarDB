using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BrightstarDB.Client;
using BrightstarDB.Model;
using BrightstarDB.Profiling;
using BrightstarDB.Query;
using BrightstarDB.Rdf;
using BrightstarDB.Storage.BPlusTreeStore.GraphIndex;
using BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex;
using BrightstarDB.Storage.BPlusTreeStore.ResourceIndex;
using BrightstarDB.Storage.Persistence;
using BrightstarDB.Utils;
using VDS.RDF.Query;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal class Store : IStore
    {
        private ulong _currentTxnId;
        private readonly IPageStore _pageStore;
        private IPageStoreGraphIndex _graphIndex;
        private IPageStoreResourceIndex _resourceIndex;
        private IPageStoreRelatedResourceIndex _subjectRelatedResourceIndex;
        private IPageStoreRelatedResourceIndex _objectRelatedResourceIndex;
        private IPageStorePrefixManager _prefixManager;
        private readonly IResourceTable _resourceTable;

        public Store(string storeLocation, IPageStore dataPageStore, IResourceTable resourceTable, ulong storePageId, BrightstarProfiler profiler)
        {
            using (profiler.Step("Load Store"))
            {
                DirectoryPath = storeLocation;
                _pageStore = dataPageStore;
                _resourceTable = resourceTable;
                var storePage = _pageStore.Retrieve(storePageId, profiler);
                Load(storePage, profiler);
            }
        }

        public Store(string storeLocation, IPageStore dataPageStore, IResourceTable resourceTable)
        {
            DirectoryPath = storeLocation;
            _pageStore = dataPageStore;
            _graphIndex = new ConcurrentGraphIndex(_pageStore);
            _subjectRelatedResourceIndex = new RelatedResourceIndex.RelatedResourceIndex(_currentTxnId + 1, _pageStore);
            _objectRelatedResourceIndex = new RelatedResourceIndex.RelatedResourceIndex(_currentTxnId + 1, _pageStore);
            _prefixManager = new PrefixManager(_pageStore);
            _resourceTable = resourceTable;
            _resourceIndex = new ResourceIndex.ResourceIndex(1, _pageStore, _resourceTable);
        }

        #region Implementation of IStore

        /// <summary>
        /// Get the full path to the store directory
        /// </summary>
        public string DirectoryPath { get; private set; }

        public IEnumerable<Triple> Match(string subject, string predicate, string obj, bool isLiteral, string dataType, string langCode, string graph)
        {
            return Match(subject, predicate, obj, isLiteral, dataType, langCode, graph == null ? null : new[] {graph});
        }

        public IEnumerable<Triple> Match(string subject, string predicate, string obj, bool isLiteral, string dataType, string langCode, IEnumerable<string> graphs)
        {
            if (langCode != null) langCode = langCode.ToLowerInvariant();
            ulong sid = FindResourceId(subject);
            ulong pid = FindResourceId(predicate);
            ulong oid = FindResourceId(obj, isLiteral, dataType, langCode);
            var gids = LookupGraphIds(graphs);

            if (sid == StoreConstants.NullUlong && !String.IsNullOrEmpty(subject)) return new Triple[0];
            if (pid == StoreConstants.NullUlong && !String.IsNullOrEmpty(predicate)) return new Triple[0];
            if (oid == StoreConstants.NullUlong && !String.IsNullOrEmpty(obj)) return new Triple[0];
            if (gids.Count == 0) return new Triple[0];

            return Bind(sid, pid, oid, gids).Select(MakeTriple);
        }

        public IEnumerable<Triple> GetResourceStatements(string resource, string graphUri)
        {
            ulong sid = FindResourceId(resource);
            if (sid == StoreConstants.NullUlong) return new Triple[0];
            int gid;
            if (String.IsNullOrEmpty(graphUri))
            {
                gid = -1;
            }
            else if (!_graphIndex.TryFindGraphId(graphUri, out gid))
            {
                return new Triple[0];
            }
            return BindSubject(sid, new List<int> {gid}).Select(MakeTriple);
        }

        public BrightstarSparqlResultsType ExecuteSparqlQuery(SparqlQuery query, ISerializationFormat targetFormat, Stream resultsStream,
            IEnumerable<string> defaultGraphUris, IStoreStatistics storeStatistics )
        {
            var queryHandler = new SparqlQueryHandler(targetFormat, defaultGraphUris, storeStatistics);
            // NOTE: streamWriter is not wrapped in a using because we don't want to close resultStream at this point
            
            var streamWriter = new StreamWriter(resultsStream, targetFormat.Encoding);
            var resultsType = queryHandler.ExecuteSparql(query, this, streamWriter);
            return resultsType;
        }

        /// <summary>
        /// Insert a triple into the store
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="predicate"></param>
        /// <param name="objValue"></param>
        /// <param name="isObjectLiteral"></param>
        /// <param name="dataType"></param>
        /// <param name="langCode"></param>
        /// <param name="graphUri"></param>
        /// <param name="profiler"></param>
        public void InsertTriple(string subject, string predicate, string objValue, bool isObjectLiteral, string dataType, string langCode, string graphUri, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("InsertTriple"))
            {
                using (profiler.Step("Normalization"))
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
                }

                using (profiler.Step("Make Prefixed Uris"))
                {
                    subject = _prefixManager.MakePrefixedUri(subject);
                    predicate = _prefixManager.MakePrefixedUri(predicate);
                    if (!isObjectLiteral)
                    {
                        objValue = _prefixManager.MakePrefixedUri(objValue);
                    }
                }

                var txnId = _currentTxnId + 1;
                ulong sid = _resourceIndex.AssertResourceInIndex(txnId, subject, profiler:profiler);
                ulong pid = _resourceIndex.AssertResourceInIndex(txnId, predicate, profiler:profiler);
                ulong oid = _resourceIndex.AssertResourceInIndex(txnId, objValue, isObjectLiteral, dataType, langCode,
                                                                 !isObjectLiteral, profiler);
                int gid = _graphIndex.AssertGraphId(graphUri, profiler);
                _subjectRelatedResourceIndex.AddRelatedResource(txnId, sid, pid, oid, gid, profiler);
                _objectRelatedResourceIndex.AddRelatedResource(txnId, oid, pid, sid, gid, profiler);
            }
        }

        /// <summary>
        /// Inserts a triple into the store
        /// </summary>
        /// <param name="triple"></param>
        public void InsertTriple(Triple triple)
        {
            InsertTriple(triple.Subject, triple.Predicate, triple.Object, triple.IsLiteral, triple.DataType,
                         triple.LangCode, triple.Graph ?? Constants.DefaultGraphUri);
        }

        /// <summary>
        /// Delete triple from store
        /// </summary>
        /// <param name="triple"></param>
        public void DeleteTriple(Triple triple)
        {
            int gid;
            if (!_graphIndex.TryFindGraphId(triple.Graph, out gid))
            {
                // No graph match, so no-op
                return;
            }

            ulong sid = FindResourceId(triple.Subject);
            if (sid == StoreConstants.NullUlong)
            {
                // No subject match, so no-op
                return;
            }

            ulong pid = FindResourceId(triple.Predicate);
            if (pid == StoreConstants.NullUlong)
            {
                // No predicate match, so no-op
                return;
            }

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
            if(oid == StoreConstants.NullUlong)
            {
                // No object match, so no-op
                return;
            }
            var txnId = _currentTxnId + 1;
            _subjectRelatedResourceIndex.DeleteRelatedResource(txnId, sid, pid, oid, gid, null);
            _objectRelatedResourceIndex.DeleteRelatedResource(txnId, oid, pid, sid, gid, null);
        }

        public void Commit(Guid jobId, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("Store Commit"))
            {
                ulong storePageId = Save(profiler);
                var storeManager = StoreManagerFactory.GetStoreManager();
                
                var mf = storeManager.GetMasterFile(DirectoryPath);
                mf.AppendCommitPoint(new CommitPoint(storePageId,_currentTxnId + 1,DateTime.UtcNow, jobId));
                _currentTxnId++;
            }
        }

        /// <summary>
        /// Commits all updates so far to the store and returns the start position of the Store object in the store file.
        /// </summary>
        public void FlushChanges(BrightstarProfiler profiler = null)
        {
            _subjectRelatedResourceIndex.FlushCache();
            _objectRelatedResourceIndex.FlushCache();
        }

        /// <summary>
        /// Returns a list of commit points with the most recent returned first
        /// </summary>
        /// <returns>Store commit points, most recent first.</returns>
        public IEnumerable<CommitPoint> GetCommitPoints()
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            return storeManager.GetMasterFile(DirectoryPath).GetCommitPoints();
        }

        /// <summary>
        /// Makes the provided commit point the most recent one.
        /// </summary>
        /// <param name="commitPoint">The commitpoint to make the most recent.</param>
        public void RevertToCommitPoint(CommitPoint commitPoint)
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            var masterFile = storeManager.GetMasterFile(DirectoryPath);
            if (masterFile.PersistenceType == PersistenceType.Rewrite)
            {
                throw new BrightstarClientException("Revert is not supported by a store using the binary page persistence type.");
            }
            masterFile.AppendCommitPoint(commitPoint);
        }

        /// <summary>
        /// Get an enumeration over all graph URIs in the store
        /// </summary>
        /// <returns>An enumeration of string values where each value is the URI of a graph that was previously added to the store</returns>
        public IEnumerable<string> GetGraphUris()
        {
            return _graphIndex.EnumerateEntries().Select(x => x.Uri).ToList();
        }

        /// <summary>
        /// Removes all unused data.
        /// </summary>
        /// <param name="jobId"></param>
        public void Consolidate(Guid jobId)
        
        {
            var storeManager = StoreManagerFactory.GetStoreManager();
            var consolidatePageStore = storeManager.CreateConsolidationStore(DirectoryPath);
            ulong txnId = _currentTxnId + 1;
            ulong storePageId;
            try
            {
                var graphIndexId = _graphIndex.Write(consolidatePageStore, txnId, null);
                var prefixManagerId = _prefixManager.Write(consolidatePageStore, txnId, null);
                var resourceIndexId = _resourceIndex.Write(consolidatePageStore, txnId, null);
                var subjectRelatedResourceIndexId = _subjectRelatedResourceIndex.Write(consolidatePageStore, txnId, null);
                var objectRelatedResourceIndexId = _objectRelatedResourceIndex.Write(consolidatePageStore, txnId, null);
                var buff = CreateStoreHeader(graphIndexId, prefixManagerId, resourceIndexId,
                                             subjectRelatedResourceIndexId,
                                             objectRelatedResourceIndexId);
                var storePage = consolidatePageStore.Create(txnId);
                storePage.SetData(buff);
                storePage.SetData(buff, 0, 128);
                storePageId = storePage.Id;
                consolidatePageStore.Commit(txnId, null);
                // Close the stores to allow the rename to happen
                Close();
            }
            finally
            {
                // Ensure we close the store even if an exception was raised occurred 
                consolidatePageStore.Close();
            }
            storeManager.ActivateConsolidationStore(DirectoryPath);
            storeManager.GetMasterFile(DirectoryPath).AppendCommitPoint(
                new CommitPoint(storePageId, txnId, DateTime.UtcNow, jobId), true);
        }

        /// <summary>
        /// Copies all the indexes from this store to the specified target page store
        /// </summary>
        /// <param name="pageStore">The page store to copy to</param>
        /// <param name="txnId">The transaction Id to use in the target page store for the write</param>
        /// <returns>The ID of the root store page in the target page store</returns>
        public ulong CopyTo(IPageStore pageStore, ulong txnId)
        {
            var graphIndexId = _graphIndex.Write(pageStore, txnId, null);
            var prefixManagerId = _prefixManager.Write(pageStore, txnId, null);
            var resourceIndexId = _resourceIndex.Write(pageStore, txnId, null);
            var subjectRelatedResourceIndexId = _subjectRelatedResourceIndex.Write(pageStore, txnId, null);
            var objectRelatedResourceIndexId = _objectRelatedResourceIndex.Write(pageStore, txnId, null);
            var buff = CreateStoreHeader(graphIndexId, prefixManagerId, resourceIndexId, subjectRelatedResourceIndexId,
                                         objectRelatedResourceIndexId);
            var storePage = pageStore.Create(txnId);
            storePage.SetData(buff);
            storePage.SetData(buff, 0, 128);
            pageStore.Commit(txnId, null);
            return storePage.Id;
        }

        public void CopyGraph(string srcGraphUri, string targetGraphUri)
        {
            // Clear out existing data in target graph (if any) by deleting the graph entry itself
            int graphId;
            if (_graphIndex.TryFindGraphId(targetGraphUri, out graphId))
            {
                _graphIndex.DeleteGraph(graphId);
            }

            // Copy all triples from source graph to target graph
            foreach (var t in Match(null, null, null, false, null, null, srcGraphUri))
            {
                t.Graph = targetGraphUri;
                InsertTriple(t);
            }
        }

        public void MoveGraph(string srcGraphUri, string targetGraphUri)
        {
            // Copy triples from source to target
            CopyGraph(srcGraphUri, targetGraphUri);
            // Delete source
            DeleteGraph(srcGraphUri);
        }

        public void AddGraph(string srcGraphUri, string targetGraphUri)
        {
            // TODO: This could be made more efficient by implementing it inside the resource index class.
            foreach(var t in Match(null, null, null, false, null, null, srcGraphUri))
            {
                t.Graph = targetGraphUri;
                InsertTriple(t);
            }
        }

        public void DeleteGraph(string graphUri)
        {
            int graphId;
            if (_graphIndex.TryFindGraphId(graphUri, out graphId))
            {
                _graphIndex.DeleteGraph(graphId);
            }
        }

        public void DeleteGraphs(IEnumerable<string> graphUris)
        {
            foreach(var g in graphUris)
            {
                DeleteGraph(g);
            }
        }

        public void Close()
        {
            _pageStore.Close();
            _pageStore.Dispose();
            _resourceIndex.Dispose();
            _resourceTable.Dispose();
        }

        public IEnumerable<string> GetPredicates(BrightstarProfiler profiler = null)
        {
            return
                from resource in
                    _subjectRelatedResourceIndex.EnumeratePredicates(profiler)
                                                .Select(rid => _resourceIndex.GetResource(rid))
                where resource != null && !resource.IsLiteral
                select _prefixManager.ResolvePrefixedUri(resource.Value);
        }

        public ulong GetTripleCount(string predicateUri, BrightstarProfiler profiler = null)
        {
            var predicateId = _resourceIndex.GetResourceId(_prefixManager.MakePrefixedUri(predicateUri));
            if (predicateId == StoreConstants.NullUlong) return 0L;
            return _subjectRelatedResourceIndex.CountPredicateRelationships(predicateId, profiler);
        }

        public void WarmupPageCache(int pagesToPreload, BrightstarProfiler profiler = null)
        {
            int totalLoaded = _subjectRelatedResourceIndex.Preload(pagesToPreload/3, profiler);
            totalLoaded += _objectRelatedResourceIndex.Preload(pagesToPreload - totalLoaded, profiler);
            totalLoaded += _resourceIndex.Preload(pagesToPreload - totalLoaded, profiler);
            _resourceTable.Preload(pagesToPreload - totalLoaded, profiler);
        }

        #endregion

        #region Serialization

        /*
         * Data format for the store page:
         * 
         * 00-03: Store format version (int)
         * 04-11: Transaction id (ulong)
         * 12-19: Graph Index start page id (ulong)
         * 20-27: Prefix Manager start page id (ulong)
         * 28-35: Resource Index start page id (ulong)
         * 36-43: Subject Related Resource Index start page id (ulong)
         * 44-51: Object Related Resource Index start page id (ulong)
         * 44-107: Reserved (all zeros for now)
         * 108-127: SHA1 hash of bytes 00-108
         * 128-255: Repeat of the above structure
        */

        private bool Load(IPage storePage, BrightstarProfiler profiler)
        {
            using (profiler.Step("Store.Load"))
            {
                // Validate the hash for the index bloc
                using (var sha1 = new SHA1Managed())
                {
                    var recordedHash = new byte[20];
                    Array.Copy(storePage.Data, 108, recordedHash, 0, 20);
                    var calculatedHash = sha1.ComputeHash(storePage.Data, 0, 108);
                    if (recordedHash.Compare(calculatedHash) != 0)
                    {
                        return false;
                    }
                }

                // Load indexes from the pointers
                int storeVersion = BitConverter.ToInt32(storePage.Data, 0);
                if (storeVersion == 1)
                {
                    _currentTxnId = BitConverter.ToUInt64(storePage.Data, 4);
                    var graphIndexId = BitConverter.ToUInt64(storePage.Data, 12);
                    _graphIndex = new ConcurrentGraphIndex(_pageStore, graphIndexId, profiler);
                    var prefixManagerId = BitConverter.ToUInt64(storePage.Data, 20);
                    _prefixManager = new PrefixManager(_pageStore, prefixManagerId, profiler);
                    var resourceIndexId = BitConverter.ToUInt64(storePage.Data, 28);
                    _resourceIndex = new ResourceIndex.ResourceIndex(_pageStore, _resourceTable, resourceIndexId);
                    var relatedResourceIndexId = BitConverter.ToUInt64(storePage.Data, 36);
                    _subjectRelatedResourceIndex = new RelatedResourceIndex.RelatedResourceIndex(_pageStore,
                                                                                                 relatedResourceIndexId, profiler);
                    var objectRelatedResourceIndexId = BitConverter.ToUInt64(storePage.Data, 44);
                    _objectRelatedResourceIndex = new RelatedResourceIndex.RelatedResourceIndex(_pageStore,
                                                                                                objectRelatedResourceIndexId, profiler);
                }
                return true;
            }
        }

        private ulong Save(BrightstarProfiler profiler)
        {
            using (profiler.Step("Store.Save"))
            {
                _resourceTable.Commit(_currentTxnId + 1, profiler);

                var txnId = _currentTxnId + 1;
                var graphIndexId = _graphIndex.Save(txnId, profiler);
                var prefixManagerId = _prefixManager.Save(txnId, profiler);
                var resourceIndexId = _resourceIndex.Save(txnId, profiler);
                var subjectRelatedResourceIndexId = _subjectRelatedResourceIndex.Save(txnId, profiler);
                var objectRelatedResourceIndexId = _objectRelatedResourceIndex.Save(txnId, profiler);
                var buff = CreateStoreHeader(graphIndexId, prefixManagerId, resourceIndexId,
                                             subjectRelatedResourceIndexId, objectRelatedResourceIndexId);

                var page = _pageStore.Create(txnId);
                page.SetData(buff);
                page.SetData(buff, 0, 128);
                _pageStore.Commit(txnId, profiler);
                return page.Id;
            }
        }

        private byte[] CreateStoreHeader(ulong graphIndexId, ulong prefixManagerId, ulong resourceIndexId, ulong subjectRelatedResourceIndexId, ulong objectRelatedResourceIndexId)
        {
            var buff = new byte[128];
            BitConverter.GetBytes(1).CopyTo(buff, 0);

            using (var sha1 = new SHA1Managed())
            {
                BitConverter.GetBytes(_currentTxnId + 1).CopyTo(buff, 4);
                BitConverter.GetBytes(graphIndexId).CopyTo(buff, 12);
                BitConverter.GetBytes(prefixManagerId).CopyTo(buff, 20);
                BitConverter.GetBytes(resourceIndexId).CopyTo(buff, 28);
                BitConverter.GetBytes(subjectRelatedResourceIndexId).CopyTo(buff, 36);
                BitConverter.GetBytes(objectRelatedResourceIndexId).CopyTo(buff, 44);
                var hash = sha1.ComputeHash(buff, 0, 108);
                hash.CopyTo(buff, 108);
            }
            return buff;
        }

        #endregion

        private ulong FindResourceId(string resourceValue, bool isLiteral = false, string dataType = null,
                                     string langCode = null)
        {
            if (resourceValue == null) return StoreConstants.NullUlong;
            if (!isLiteral)
            {
                try
                {
                    resourceValue = new Uri(resourceValue, UriKind.Absolute).ToString();
                }
                catch (FormatException)
                {
                    throw new InvalidTripleException(
                        String.Format("The string '{0}' could not be parsed as a valid URI.", resourceValue));
                }
                resourceValue = _prefixManager.MakePrefixedUri(resourceValue);
            }
            return _resourceIndex.GetResourceId(resourceValue, isLiteral, dataType, langCode, !isLiteral);
        }

        private static readonly List<int> AllGraphs = new List<int>{-1};
        private List<int> LookupGraphIds(IEnumerable<string> graphs)
        {
            if (graphs == null) return AllGraphs;
            var ret = new List<int>();
            foreach (var g in graphs)
            {
                int gid;
                if (_graphIndex.TryFindGraphId(g, out gid))
                {
                    ret.Add(gid);
                }
            }
            return ret;
        }

        private Triple MakeTriple(Tuple<ulong, ulong, ulong, int> data)
        {
            string subject = _prefixManager.ResolvePrefixedUri(Resolve(data.Item1).Value);
            string predicate = _prefixManager.ResolvePrefixedUri(Resolve(data.Item2).Value);
            IResource obj = Resolve(data.Item3);
            string graph = _graphIndex.GetGraphUri((int)data.Item4);

            if (obj.IsLiteral)
            {
                IResource dataType = Resolve(obj.DataTypeId);
                IResource lc = Resolve(obj.LanguageCodeId);
                return new Triple
                {
                    Subject = subject,
                    Predicate = predicate,
                    Object = obj.Value,
                    DataType = dataType == null ? null : dataType.Value,
                    LangCode = lc == null ? null : lc.Value,
                    IsLiteral = true,
                    Graph = graph
                };
            }
            return new Triple
            {
                Subject = subject,
                Predicate = predicate,
                Object = _prefixManager.ResolvePrefixedUri(obj.Value),
                Graph = graph
            };
        }

        public IResource Resolve(ulong resourceId)
        {
            var ret = _resourceIndex.GetResource(resourceId);
#if DEBUG
            if (ret == null && resourceId != StoreConstants.NullUlong)
            {
                throw new Exception(String.Format("Could not resolve resource id {0}", resourceId));
            }
#endif
            return ret;
        }

        public string ResolvePrefixedUri(string prefixedUri)
        {
            return _prefixManager.ResolvePrefixedUri(prefixedUri);
        }

        public ulong LookupResource(string uri)
        {
            var curie = _prefixManager.MakePrefixedUri(uri);
            return _resourceIndex.GetResourceId(curie);
        }

        public ulong LookupResource(string value, string datatype, string langCode)
        {
            return _resourceIndex.GetResourceId(value, true, datatype, langCode);
        }

        public int LookupGraph(string graphUri)
        {
            int graphId;
            if (_graphIndex.TryFindGraphId(graphUri, out graphId)) return graphId;
            return -1;
        }

        public string ResolveGraphUri(int graphId)
        {
            return _graphIndex.GetGraphUri(graphId);
        }

        public IEnumerable<Tuple<ulong, ulong, ulong, int>> GetBindings(string subject, string predicate, string obj, bool isLiteral = false, string dataType = null,
                                       string langCode = null, string graph = null)
        {
            return GetBindings(subject, predicate, obj, isLiteral, dataType, langCode, graph == null ? null : new[] { graph });
        }

        public IEnumerable<Tuple<ulong, ulong, ulong, int>> GetBindings(string subject, string predicate, string obj, bool isLiteral = false, string dataType = null,
                                       string langCode = null, IEnumerable<string> graphs = null)
        {
            if (langCode != null) langCode = langCode.ToLowerInvariant();
            ulong sid = FindResourceId(subject);
            ulong pid = FindResourceId(predicate);
            ulong oid = FindResourceId(obj, isLiteral, dataType, langCode);
            var gids = LookupGraphIds(graphs);

            if (sid == StoreConstants.NullUlong && !String.IsNullOrEmpty(subject)) return new Tuple<ulong, ulong, ulong, int>[0];
            if (pid == StoreConstants.NullUlong && !String.IsNullOrEmpty(predicate)) return new Tuple<ulong, ulong, ulong, int>[0];
            if (oid == StoreConstants.NullUlong && !String.IsNullOrEmpty(obj)) return new Tuple<ulong, ulong, ulong, int>[0];
            if (gids.Count == 0) return new Tuple<ulong, ulong, ulong, int>[0];

            return Bind(sid, pid, oid, gids);
        }

        public IEnumerable<Tuple<ulong, ulong, ulong, int>> GetBindings(ulong? subjNodeId, string subjValue, ulong? predNodeId, string predValue, ulong? objNodeId,
                                       string objValue, bool isLiteral, string dataType, string languageCode, List<string> graphUris)
        {
            if (languageCode != null) languageCode = languageCode.ToLowerInvariant();
            ulong sid = subjNodeId.HasValue ? subjNodeId.Value : FindResourceId(subjValue);
            ulong pid = predNodeId.HasValue ? predNodeId.Value : FindResourceId(predValue);
            ulong oid = objNodeId.HasValue
                            ? objNodeId.Value
                            : FindResourceId(objValue, isLiteral, dataType, languageCode);
            var gids = LookupGraphIds(graphUris);

            if (sid == StoreConstants.NullUlong && !String.IsNullOrEmpty(subjValue)) return new Tuple<ulong, ulong, ulong, int>[0];
            if (pid == StoreConstants.NullUlong && !String.IsNullOrEmpty(predValue)) return new Tuple<ulong, ulong, ulong, int>[0];
            if (oid == StoreConstants.NullUlong && !String.IsNullOrEmpty(objValue)) return new Tuple<ulong, ulong, ulong, int>[0];
            if (gids.Count == 0) return new Tuple<ulong, ulong, ulong, int>[0];

            return Bind(sid, pid, oid, gids);
        }

        #region Triple Pattern Binding
        private IEnumerable<Tuple<ulong, ulong, ulong, int>> Bind(ulong s = StoreConstants.NullUlong, ulong p = StoreConstants.NullUlong,
                                                                    ulong o = StoreConstants.NullUlong,
                                                                    List<int> graphs = null)
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

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindSubjectPredicateObject(ulong sid, ulong pid, ulong oid, List<int> graphs)
        {
            if (graphs.Any(g=> g<0))
            {
                // Wildcard match on graphs
                return _subjectRelatedResourceIndex.EnumerateRelatedResources(sid, pid).Where(r => r.ResourceId == oid).Select(
                    r => new Tuple<ulong, ulong, ulong, int>(sid, pid, oid, r.GraphId));
            }
            if (graphs.Count== 1)
            {
                // Single graph match
                if (_subjectRelatedResourceIndex.ContainsRelatedResource(sid, pid, oid, graphs[0], null))
                {
                    return new []
                               {
                                   new Tuple<ulong, ulong, ulong, int>(sid, pid, oid, graphs[0])
                               };
                }
                return new Tuple<ulong, ulong, ulong, int>[0];
            }
            // Filter on graph id list
            return _subjectRelatedResourceIndex.EnumerateRelatedResources(sid, pid)
                .Where(r => (r.ResourceId == oid) && graphs.Contains(r.GraphId))
                .Select(r => new Tuple<ulong, ulong, ulong, int>(sid, pid, oid, r.GraphId));
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindSubjectPredicate(ulong sid, ulong pid, List<int> graphs)
        {
            if (graphs.Any(g => g < 0))
            {
                // Wildcard match on graphs
                foreach (var r in _subjectRelatedResourceIndex.EnumerateRelatedResources(sid, pid))
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(sid, pid, r.ResourceId, r.GraphId);
                }
            }
            else
            {
                foreach (var g in graphs)
                {
                    foreach (var r in _subjectRelatedResourceIndex.EnumerateRelatedResources(sid, pid, g))
                    {
                        yield return new Tuple<ulong, ulong, ulong, int>(sid, pid, r.ResourceId, g);
                    }
                }
            }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindSubjectObject(ulong sid, ulong oid, List<int> graphs)
        {
            if (graphs.Any(g => g < 0))
            {
                foreach (
                    var r in _subjectRelatedResourceIndex.EnumerateRelatedResources(sid).Where(r => r.ResourceId == oid)
                    )
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(sid, r.PredicateId, oid, r.GraphId);
                }
            }
            else if (graphs.Count == 1)
            {
                foreach (
                    var r in
                        _subjectRelatedResourceIndex.EnumerateRelatedResources(sid, StoreConstants.NullUlong, graphs[0]).Where(r=>r.ResourceId == oid)
                    )
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(sid, r.PredicateId, oid, r.GraphId);
                }
            }
            else
            {
                foreach (
                    var r in
                        _subjectRelatedResourceIndex.EnumerateRelatedResources(sid).Where(
                            r => r.ResourceId == oid && graphs.Contains(r.GraphId)))
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(sid, r.PredicateId, oid, r.GraphId);
                }
            }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindPredicateObject(ulong pid, ulong oid, List<int> graphs)
        {
            if (graphs.Any(g => g < 0))
            {
                // Wildcard graph match
                foreach (var r in _objectRelatedResourceIndex.EnumerateRelatedResources(oid, pid))
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(r.ResourceId, pid, oid, r.GraphId);
                }
            }
            else if (graphs.Count == 1)
            {
                // Specific graph lookup
                foreach (var r in _objectRelatedResourceIndex.EnumerateRelatedResources(oid, pid, graphs[0]))
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(r.ResourceId, pid, oid, r.GraphId);
                }
            }
            else
            {
                // Filter by graph list
                foreach (
                    var r in
                        _objectRelatedResourceIndex.EnumerateRelatedResources(oid, pid).Where(
                            r => graphs.Contains(r.GraphId)))
                {
                    yield return new Tuple<ulong, ulong, ulong, int>(r.ResourceId, pid, oid, r.GraphId);
                }
            }
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindSubject(ulong sid, List<int> graphs)
        {
            if (graphs.Any(g=>g<0))
            {
                // Match all graphs
                return
                    _subjectRelatedResourceIndex.EnumerateRelatedResources(sid).Select(
                        r => new Tuple<ulong, ulong, ulong, int>(sid, r.PredicateId, r.ResourceId, r.GraphId));
            }
            if (graphs.Count == 1)
            {
                // Can do a direct graph lookup
                return _subjectRelatedResourceIndex.EnumerateRelatedResources(sid, 0, graphs[0]).Select(
                    r => new Tuple<ulong, ulong, ulong, int>(sid, r.PredicateId, r.ResourceId, r.GraphId));
            }
            return
                _subjectRelatedResourceIndex.EnumerateRelatedResources(sid).Where(r => graphs.Contains(r.GraphId)).
                    Select(r => new Tuple<ulong, ulong, ulong, int>(sid, r.PredicateId, r.ResourceId, r.GraphId));
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindPredicate(ulong pid, IEnumerable<int> graphs)
        {
            if (graphs.Any(g=>g < 0))
            {
                // Match all graphs
                return _subjectRelatedResourceIndex.EnumeratePredicateRelationships(pid, null).Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, r.RelatedResource, r.GraphId));
            }
            return _subjectRelatedResourceIndex.EnumeratePredicateRelationships(pid, null)
                .Where(r => graphs.Contains(r.GraphId))
                .Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, r.RelatedResource, r.GraphId));
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindObject(ulong oid, List<int> graphs)
        {
            if (graphs.Any(g=> g<0))
            {
                // Match all graphs
                return _objectRelatedResourceIndex.EnumerateRelatedResources(oid).Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, oid, r.GraphId));
            }
            if (graphs.Count == 1)
            {
                return _objectRelatedResourceIndex.EnumerateRelatedResources(oid, graphId: graphs[0]).Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, oid, r.GraphId));
            }
            return _objectRelatedResourceIndex.EnumerateRelatedResources(oid).Where(r => graphs.Contains(r.GraphId))
                .Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, oid, r.GraphId));
        }

        private IEnumerable<Tuple<ulong, ulong, ulong, int>> BindAll(IEnumerable<int> graphs)
        {
            if (graphs.Any(g=>g<0))
            {
                return _subjectRelatedResourceIndex.EnumerateAll(g => true, null).Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, r.RelatedResource, r.GraphId));
            }
            return _subjectRelatedResourceIndex
                .EnumerateAll(g => graphs.Contains(g), null)
                .Select(
                    r => new Tuple<ulong, ulong, ulong, int>(r.ResourceId, r.PredicateId, r.RelatedResource, r.GraphId));
        }

        #endregion

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        private bool _disposed;
        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Cleanup managed resources
                Close();
            }
            // Cleanup any unmanaged resource here

            // Record that disposing has been done
            _disposed = true;
        }

        ~Store()
        {
            Dispose(false);
        }
    }
}
