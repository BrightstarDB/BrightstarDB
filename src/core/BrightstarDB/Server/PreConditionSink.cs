using System.IO;
using System.Linq;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;

namespace BrightstarDB.Server
{
    internal class PreconditionSink: ITripleSink
    {
        private readonly IStore _store;
        private StringWriter _failedPreconditionsWriter;
        private NTriplesWriter _failedTriplesWriter;
        private int _failedTripleCount;

        public PreconditionSink(IStore store)
        {
            _store = store;
        }

        public bool PreconditionsFailed { get { return _failedTripleCount > 0; } }

        public int FailedPreconditionCount { get { return _failedTripleCount; } }

        public string GetFailedPreconditions()
        {
            _failedPreconditionsWriter.Flush();
            return _failedPreconditionsWriter.ToString();
        }

        public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool isLiteral, string dataType, string langCode, string graphUri)
        {
            Logging.LogDebug("Try and Match Precondition {0} {1} {2} {3} {4} {5} {6}", subject, predicate, obj, isLiteral, dataType,
                 langCode, graphUri);

            var triples = _store.Match(subject, predicate, obj, isLiteral, dataType, langCode, graphUri).ToList();
            if (triples.Count == 0)
            {
                if (_failedPreconditionsWriter == null)
                {
                    _failedPreconditionsWriter = new StringWriter();
                    _failedTriplesWriter = new NTriplesWriter(_failedPreconditionsWriter);
                }
                _failedTriplesWriter.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, obj, objIsBNode, isLiteral, dataType, langCode, graphUri);
                _failedTripleCount++;
            }
        }

    }
}
