using System;
using System.Collections.Generic;
using BrightstarDB.Profiling;
using BrightstarDB.Storage;

namespace BrightstarDB.Rdf
{
    internal class StoreTripleSink : ITripleSink
    {
        private readonly IStore _store;
        private int _total;
        private readonly Guid _jobId;
        private readonly int _batchSize;
        private readonly bool _commitEachBatch;
        private readonly Dictionary<string, string> _bnodeMap = new Dictionary<string, string>();
        private BrightstarProfiler _profiler;

        /// <summary>
        /// The memory load (as a percent of available physical RAM) above which a flush will occur
        /// </summary>
        public const int HighMemoryLoad = 80;

        /// <summary>
        /// Creates a new store writing triple sink
        /// </summary>
        /// <param name="writeStore">The store to add the triples to</param>
        /// <param name="jobId">The unique identifier of the job that is writing to the store. May be Guid.Empty if the write is not part of any job.</param>
        /// <param name="batchSize">Number of triples to insert per batch</param>
        /// <param name="commitEachBatch">If true, then the inserts are committed to the store after each batch; if false then the server memory load is checked at the end of each batch and inserts are flushed only if the load exceeds a threshold (currently 80% of available physical RAM).</param>
        /// <param name="profiler"></param>
        public StoreTripleSink(IStore writeStore, Guid jobId, int batchSize = 10000, bool commitEachBatch = false, BrightstarProfiler profiler = null)
        {
            _batchSize = batchSize;
            _store = writeStore;
            _jobId = jobId;
            _commitEachBatch = commitEachBatch;
            _profiler = profiler;
        }

        public void Triple(string subject, bool subjectIsBNode,
            string predicate, bool predicateIsBNode,
            string obj, bool objIsBNode, 
            bool isLiteral, string dataType, string langCode, string graphUri)
        {
#if DEBUG_INSERTS
            Logging.LogDebug("Triple {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", 
                subject, subjectIsBNode, 
                predicate, predicateIsBNode, 
                obj, objIsBNode, 
                isLiteral, dataType, langCode, graphUri);
#endif
            using (_profiler.Step("Assert BNodes"))
            {
                // Convert BNode identifiers to internal BNode identifiers)
                if (subjectIsBNode) subject = AssertBNode(subject);
                if (predicateIsBNode) predicate = AssertBNode(predicate);
                if (objIsBNode) obj = AssertBNode(obj);
            }
#if DEBUG_INSERTS
            Logging.LogDebug("InsertTriple {0} {1} {2} {3} {4} {5} {6}",
                subject, predicate, obj, isLiteral, dataType, langCode, graphUri);
#endif
            _store.InsertTriple(subject, predicate, obj, isLiteral, dataType, langCode, graphUri, _profiler);
            _total++;

            if (_total % _batchSize == 0 )
            {
                if (_commitEachBatch)
                {
                    Logging.LogInfo("Committing...");
                    _store.Commit(_jobId, _profiler);
                }
#if MONITOR_MEMORY_LOAD
                else
                {
                    if (Server.MemoryUtils.GetMemoryLoad() > HighMemoryLoad)
                    {
                        Logging.LogInfo("Flushing...");
                        _store.FlushChanges(_profiler);
                        // Clear out any interned URIs to free up more memory
                        VDS.RDF.UriFactory.Clear();
                        Logging.LogDebug("Initiating garbage collection...");
                        GC.Collect();
                    }
                }
#endif
                Logging.LogInfo("Complete @ " + _total);
            }
        }

        public void Close()
        {
            // No op
        }

        private string AssertBNode(string bnodeId)
        {
            string internalBnodeId;
            if (!_bnodeMap.TryGetValue(bnodeId, out internalBnodeId))
            {
                internalBnodeId = Constants.GeneratedUriPrefix + Guid.NewGuid();
                _bnodeMap[bnodeId] = internalBnodeId;
            }
            return internalBnodeId;
        }
    }
}
