using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    internal class RelatedResourceIndex : BPlusTree, IPageStoreRelatedResourceIndex
    {
        private readonly Dictionary<ulong, PredicateRelatedResourceIndex> _predicateIndexes;
        internal const int KeySize = 20;

        public RelatedResourceIndex(ulong txnId, IPageStore pageStore): base(txnId, pageStore, 8, 8)
        {
            _predicateIndexes = new Dictionary<ulong, PredicateRelatedResourceIndex>();
        }

        public RelatedResourceIndex(IPageStore pageStore, ulong rootPageId, BrightstarProfiler profiler) : base(pageStore, rootPageId, 8, 8, profiler)
        {
            _predicateIndexes = new Dictionary<ulong, PredicateRelatedResourceIndex>();
        }

        #region Implementation of IRelatedResourceIndex

        /// <summary>
        /// Adds a related resource index entry
        /// </summary>
        /// <param name="txnId"> </param>
        /// <param name="resourceId">The resource ID of the "start" resource</param>
        /// <param name="predicateId">The resource ID of the predicate that relates the resources</param>
        /// <param name="relatedResourceId">The resource ID of the "related" resource</param>
        /// <param name="graphId">The resource ID of the graph containing the relationship</param>
        /// <param name="profiler"></param>
        public void AddRelatedResource(ulong txnId, ulong resourceId, ulong predicateId, ulong relatedResourceId, int graphId, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("Add Related Resource"))
            {
                var predicateIndex = AssertPredicateIndex(txnId, predicateId, profiler);
                var key = MakePredicateIndexKey(resourceId, graphId, relatedResourceId);
                try
                {
                    using (profiler.Step("Insert Into Predicate Index"))
                    {
                        predicateIndex.Insert(txnId, key, null, profiler: profiler);
                    }
                }
                catch (DuplicateKeyException)
                {
                    // Ignore duplicate key exceptions 
                }
            }
        }

        /// <summary>
        /// Removes a related resource index entry
        /// </summary>
        /// <param name="txnId"> </param>
        /// <param name="resourceId">The resource ID of the "start" resource</param>
        /// <param name="predicateId">The resource ID of the predicate that relates the resources</param>
        /// <param name="relatedResourceId">The resource ID of the "related" resource to be removed</param>
        /// <param name="graphId">The resource ID of the graph containing the relationship</param>
        /// <param name="profiler"></param>
        public void DeleteRelatedResource(ulong txnId, ulong resourceId, ulong predicateId, ulong relatedResourceId, int graphId, BrightstarProfiler profiler)
        {
            var predicateIndex = AssertPredicateIndex(txnId, predicateId, null);
            predicateIndex.Delete(txnId, MakePredicateIndexKey(resourceId, graphId, relatedResourceId), profiler);
        }

        /// <summary>
        /// Enumerates the resources related to a start resource by a specific predicate
        /// </summary>
        /// <param name="resourceId">The resource ID of the start resource</param>
        /// <param name="predicateId">The resource ID of the predicate that relates the resources. If set to Constants.NullUlong, returns relationships for all predicates</param>
        /// <param name="graphId">The resource ID of the graph containing the relationships. If set to Constants.NullUlong, returns relationships from all graphs</param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        public IEnumerable<IRelatedResource> EnumerateRelatedResources(ulong resourceId, ulong predicateId = StoreConstants.NullUlong, int graphId = -1, BrightstarProfiler profiler = null)
        {
            using (profiler.Step("EnumerateRelatedResources"))
            {
                if (predicateId != StoreConstants.NullUlong)
                {
                    byte[] minKey = MakePredicateIndexKey(resourceId,
                                                          graphId < 0 ? 0 : graphId,
                                                          UInt64.MinValue);
                    byte[] maxKey = MakePredicateIndexKey(resourceId,
                                                          graphId < 0 ? Int32.MaxValue : graphId,
                                                          UInt64.MaxValue);
                    var predicateIndex = GetPredicateIndex(predicateId, profiler);
                    if (predicateIndex != null)
                    {
                        foreach (var r in predicateIndex.Scan(minKey, maxKey, profiler).Select(
                            x =>
                            new RelatedResource(predicateId, GetGraphIdFromKey(x.Key),
                                                GetRelatedResourceIdFromKey(x.Key))))
                        {
                            yield return r;
                        }
                    }
                }
                else
                {
                    foreach (var entry in Scan(0ul, UInt64.MaxValue, profiler))
                    {
                        // Load the predicate index into the cache
                        GetPredicateIndex(entry.Key,BitConverter.ToUInt64(entry.Value, 0), profiler);
                        // Then a recursive call to enumerate the index tree
                        foreach (var r in EnumerateRelatedResources(resourceId, entry.Key, graphId, profiler))
                        {
                            yield return r;
                        }
                    }
                }
            }
        }

        public bool ContainsRelatedResource(ulong resourceId, ulong predicateId, ulong relatedResourceId, int graphId, BrightstarProfiler profiler)
        {
            using (profiler.Step("ContainsRelatedResource"))
            {
                byte[] valueBuff = null;
                PredicateRelatedResourceIndex predicateIndex = GetPredicateIndex(predicateId, profiler);
                return predicateIndex != null &&
                       predicateIndex.Search(MakePredicateIndexKey(resourceId, graphId, relatedResourceId), valueBuff,
                                             profiler);
            }
        }

        public IEnumerable<ulong> EnumeratePredicates(BrightstarProfiler profiler)
        {
            return Scan(0ul, ulong.MaxValue, profiler).Select(entry => entry.Key);
        }

        public ulong CountPredicateRelationships(ulong predicateId, BrightstarProfiler profiler)
        {
            PredicateRelatedResourceIndex predicateIndex = GetPredicateIndex(predicateId, profiler);
            if (predicateIndex != null)
            {
                return (ulong)predicateIndex.Scan(MakePredicateIndexKey(0, 0, 0),
                                    MakePredicateIndexKey(ulong.MaxValue, int.MaxValue, ulong.MaxValue), profiler)
                              .LongCount();
            }
            return 0UL;
        }

        /// <summary>
        /// Enumerates all of the related resources stored for a single predicate
        /// </summary>
        /// <param name="predicateId"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        public IEnumerable<IResourceRelationship> EnumeratePredicateRelationships(ulong predicateId, BrightstarProfiler profiler)
        {
            PredicateRelatedResourceIndex predicateIndex = GetPredicateIndex(predicateId, profiler);
            if (predicateIndex != null)
            {
                foreach(var entry in predicateIndex.Scan(MakePredicateIndexKey(0, 0, 0), MakePredicateIndexKey(ulong.MaxValue, int.MaxValue, ulong.MaxValue), profiler))
                {
                    var thisResource = GetResourceIdFromKey(entry.Key);
                    var graphId = GetGraphIdFromKey(entry.Key);
                    var relatedResource = GetRelatedResourceIdFromKey(entry.Key);
                    yield return new ResourceRelationship(thisResource, predicateId, relatedResource, graphId);
                }
            }
        }

        /// <summary>
        /// Enumerates all relationships in the index that match the specified graph filter
        /// </summary>
        /// <param name="graphFilter"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        public IEnumerable<IResourceRelationship> EnumerateAll(Func<int, bool> graphFilter, BrightstarProfiler profiler)
        {
            foreach(var predicateIndexEntry in EnumeratePredicateIndexes(profiler))
            {
                foreach(var relatedResourceEntry in predicateIndexEntry.Value.Scan(MakePredicateIndexKey(0, 0, 0), MakePredicateIndexKey(ulong.MaxValue, int.MaxValue, ulong.MaxValue), profiler))
                {
                    if (graphFilter(GetGraphIdFromKey(relatedResourceEntry.Key)))
                    {
                        yield return new ResourceRelationship(GetResourceIdFromKey(relatedResourceEntry.Key),
                                                              predicateIndexEntry.Key,
                                                              GetRelatedResourceIdFromKey(relatedResourceEntry.Key),
                                                              GetGraphIdFromKey(relatedResourceEntry.Key));
                    }
                }
            }
        }

        #endregion

        private IEnumerable<KeyValuePair<ulong, PredicateRelatedResourceIndex>> EnumeratePredicateIndexes(BrightstarProfiler profiler)
        {
            return Scan(0ul, ulong.MaxValue, profiler).Select(predicateEntry => new KeyValuePair<ulong, PredicateRelatedResourceIndex>(predicateEntry.Key, GetPredicateIndex(predicateEntry.Key, BitConverter.ToUInt64(predicateEntry.Value, 0), profiler)));
        }

        public ulong Write(IPageStore pageStore, ulong transactionId, BrightstarProfiler profiler)
        {
            using (profiler.Step("RelatedResourceIndex.Write"))
            {
                var targetConfiguration = new BPlusTreeConfiguration(pageStore, Configuration.KeySize,
                                                                     Configuration.ValueSize, Configuration.PageSize);
                var indexBuilder = new BPlusTreeBuilder(pageStore, targetConfiguration);
                return indexBuilder.Build(transactionId, WritePredicateIndexes(pageStore, transactionId, profiler),
                                          profiler);
            }
        }

        private IEnumerable<KeyValuePair<byte[], byte []>> WritePredicateIndexes(IPageStore pageStore, ulong transactionId, BrightstarProfiler profiler)
        {
            foreach (var entry in EnumeratePredicateIndexes(profiler))
            {
                var predicateId = entry.Key;
                var targetConfiguration = new BPlusTreeConfiguration(pageStore, entry.Value.Configuration.KeySize,
                                                                     entry.Value.Configuration.ValueSize,
                                                                     entry.Value.Configuration.PageSize);
                var builder = new BPlusTreeBuilder(pageStore, targetConfiguration);
                ulong newPredicateIndexId = builder.Build(transactionId, entry.Value.Scan(profiler), profiler);
                yield return new KeyValuePair<byte[], byte[]>(BitConverter.GetBytes(predicateId), BitConverter.GetBytes(newPredicateIndexId));
            }
        }

        #region Overrides of BPlusTree
        public override ulong Save(ulong transactionId, BrightstarProfiler profiler)
        {
            using (profiler.Step("RelatedResourceIndex.Save"))
            {
                foreach (var entry in _predicateIndexes)
                {
                    if (entry.Value.IsModified)
                    {
                        var indexRoot = entry.Value.Save(transactionId, profiler);
                        Insert(transactionId, entry.Key, BitConverter.GetBytes(indexRoot), true);
                    }
                }
                return base.Save(transactionId, profiler);
            }
        }

        public void FlushCache()
        {
            var unmodifiedIndexKeys  = _predicateIndexes.Where(e => !e.Value.IsModified).Select(e => e.Key).ToList();
            foreach(var k in unmodifiedIndexKeys)
            {
                _predicateIndexes.Remove(k);
            }
        }

        public int Preload(int maxPages, BrightstarProfiler profiler=null)
        {
            int pagesLoaded = this.PreloadTree(maxPages, profiler);
            int remainingPages = maxPages - pagesLoaded;
            if (remainingPages > 0)
            {
                // We have managed to load the complete index of predicate trees into memory
                // See if there is enough room to preload at least the root node of each predicate tree
                var numPredicatesToLoad = EnumeratePredicates(profiler).Count();
                if (remainingPages >= numPredicatesToLoad)
                {
                    // We are OK to load some pages from each predicate.
                    // It would be relatively expensive to precompute which indexes have most pages
                    // instead we will just enumerate through them loading remainingPages / numPredicatesToLoad
                    foreach (var predicateIndex in EnumeratePredicateIndexes(profiler))
                    {
                        pagesLoaded += predicateIndex.Value.PreloadTree(remainingPages/numPredicatesToLoad, profiler);
                        remainingPages = maxPages - pagesLoaded;
                        if (remainingPages <= 0) break;
                        numPredicatesToLoad--;
                    }
                }
            }
            return pagesLoaded;
        }

        #endregion
        private PredicateRelatedResourceIndex AssertPredicateIndex(ulong transactionId, ulong predicateId, BrightstarProfiler profiler)
        {
            using (profiler.Step("AssertPredicateIndex"))
            {
                PredicateRelatedResourceIndex predicateIndex = GetPredicateIndex(predicateId, profiler);
                if (predicateIndex == null)
                {
                    using (profiler.Step("New Predicate Index"))
                    {
                        predicateIndex = new PredicateRelatedResourceIndex(transactionId, PageStore);
                        _predicateIndexes[predicateId] = predicateIndex;
                    }
                }
                return predicateIndex;
            }
        }

        private PredicateRelatedResourceIndex GetPredicateIndex(ulong predicateId, ulong rootPageId, BrightstarProfiler profiler)
        {
            lock(_predicateIndexes)
            {
                PredicateRelatedResourceIndex predicateIndex;
                if (_predicateIndexes.TryGetValue(predicateId, out predicateIndex)) return predicateIndex;
                predicateIndex = new PredicateRelatedResourceIndex(PageStore, rootPageId);
                _predicateIndexes[predicateId] = predicateIndex;
                return predicateIndex;
            }
        }
        private PredicateRelatedResourceIndex GetPredicateIndex(ulong predicateId, BrightstarProfiler profiler)
        {
            lock (_predicateIndexes)
            {
                PredicateRelatedResourceIndex predicateIndex;
                if (_predicateIndexes.TryGetValue(predicateId, out predicateIndex))
                {
                    return predicateIndex;
                }
                var buff = new byte[8];

                if (Search(predicateId, buff, profiler))
                {
                    ulong predicateIndexRoot = BitConverter.ToUInt64(buff, 0);
                    predicateIndex = new PredicateRelatedResourceIndex(PageStore, predicateIndexRoot);
                    _predicateIndexes[predicateId] = predicateIndex;
                    return predicateIndex;
                }
                return null;
            }
        }

        private static byte[] MakePredicateIndexKey(ulong resourceId, int graphId, ulong relatedResourceId)
        {
            var ret = new byte[20];
            BitConverter.GetBytes(relatedResourceId).CopyTo(ret, 0);
            BitConverter.GetBytes(graphId).CopyTo(ret, 8);
            BitConverter.GetBytes(resourceId).CopyTo(ret, 12);
            return ret;
        }

        private static int GetGraphIdFromKey(byte[] key)
        {
            return BitConverter.ToInt32(key, 8);
        }

        private static ulong GetRelatedResourceIdFromKey(byte [] key)
        {
            return BitConverter.ToUInt64(key, 0);
        }

        private static ulong GetResourceIdFromKey(byte[] key)
        {
            return BitConverter.ToUInt64(key, 12);
        }
    }
}
