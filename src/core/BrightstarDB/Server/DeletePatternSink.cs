using System.Linq;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;

namespace BrightstarDB.Server
{
    internal class DeletePatternSink : ITripleSink
    {
        private readonly IStore _store;

        public DeletePatternSink(IStore store)
        {
            _store = store;
        }

        public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool isLiteral, string dataType, string langCode, string graphUri)
        {
            Logging.LogDebug("Delete Triple {0} {1} {2} {3} {4} {5} {6}", subject, predicate, obj, isLiteral, dataType,
                             langCode, graphUri);

            var wildCards = false;

            var s = subject;
            if (subject.Equals(Constants.WildcardUri))
            {
                s = null;
                wildCards = true;
            }


            var p = predicate;
            if (predicate.Equals(Constants.WildcardUri))
            {
                p = null;
                wildCards = true;
            }

            var o = obj;
            if (obj.Equals(Constants.WildcardUri))
            {
                o = null;
                wildCards = true;
            }

            var g = graphUri;
            if (graphUri.Equals(Constants.WildcardUri))
            {
                g = null;
                wildCards = true;
            }
            else if (g == null)
            {
                g = Constants.DefaultGraphUri;
            }

            if (isLiteral && dataType == null)
            {
                dataType = RdfDatatypes.PlainLiteral;
            }

            if (wildCards)
            {
                var triples = _store.Match(s, p, o, isLiteral, dataType, langCode, g).ToList();
                foreach (var t in triples)
                {
                    _store.DeleteTriple(t);
                }
            }
            else
            {
                _store.DeleteTriple(new Model.Triple
                                        {
                                            Graph = graphUri,
                                            IsLiteral = isLiteral,
                                            Subject = s,
                                            Predicate = p,
                                            Object = o,
                                            DataType = dataType,
                                            LangCode = langCode
                                        });
            }
        }

        public void Close()
        {
            // No-op
        }

        public void BNode(string bnodeId, string bnodeUri)
        {
            return;
        }

        
    }
}
