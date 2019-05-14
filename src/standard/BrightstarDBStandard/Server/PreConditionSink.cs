using System.IO;
using System.Linq;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;

namespace BrightstarDB.Server
{
    internal sealed class PreconditionSink: ITripleSink
    {
        private readonly IStore _store;
        private StringWriter _failedPreconditionsWriter;
        private NTriplesWriter _failedTriplesWriter;
        private readonly PreconditionType _preconditionType;

        internal enum PreconditionType
        {
            ExistsPrecondition,
            NotExistsPrecondition
        }

        public PreconditionSink(IStore store, PreconditionType preconditionType)
        {
            _store = store;
            _preconditionType = preconditionType;
        }

        public bool PreconditionsFailed { get { return FailedPreconditionCount > 0; } }

        public int FailedPreconditionCount { get; private set; }

        public string GetFailedPreconditions()
        {
            if (_failedPreconditionsWriter == null) return string.Empty;
            _failedPreconditionsWriter.Flush();
            return _failedPreconditionsWriter.ToString();
        }

        public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool isLiteral, string dataType, string langCode, string graphUri)
        {
            var triplesEnum = _store.Match((Constants.WildcardUri.Equals(subject)) ? null : subject,
                                           (Constants.WildcardUri.Equals(predicate)) ? null : predicate,
                                           (Constants.WildcardUri.Equals(obj) && !isLiteral) ? null : obj,
                                           isLiteral, dataType, langCode, 
                                           (Constants.WildcardUri.Equals(graphUri)) ? null : graphUri);
            if (_preconditionType == PreconditionType.ExistsPrecondition)
            {
                Logging.LogDebug("Check triple exists precondition {0} {1} {2} {3} {4} {5} {6}", subject, predicate, obj,
                                 isLiteral, dataType, langCode, graphUri);
                if (!triplesEnum.Any())
                {
                    FailedPrecondition(subject, subjectIsBNode, predicate, predicateIsBNode, obj, objIsBNode,
                                       isLiteral, dataType, langCode, graphUri);
                }
            }
            else
            {
                Logging.LogDebug("Check non-existance precondition {0} {1} {2} {3} {4} {5} {6}", subject, predicate, obj,
                                 isLiteral, dataType, langCode, graphUri);
                if (triplesEnum.Any())
                {
                    FailedPrecondition(subject, subjectIsBNode, predicate, predicateIsBNode, obj, objIsBNode,
                                       isLiteral, dataType, langCode, graphUri);
                }
            }
        }

        public void Close()
        {
            // No-op
        }

        private void FailedPrecondition(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool isLiteral, string dataType, string langCode, string graphUri)
        {
            if (_failedPreconditionsWriter == null)
            {
                _failedPreconditionsWriter = new StringWriter();
                _failedTriplesWriter = new NTriplesWriter(_failedPreconditionsWriter);
            }
            _failedTriplesWriter.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, obj, objIsBNode,
                                        isLiteral, dataType, langCode, graphUri);
            FailedPreconditionCount++;
        }
    }
}
