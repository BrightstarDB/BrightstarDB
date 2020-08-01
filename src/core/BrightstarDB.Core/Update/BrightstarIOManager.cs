using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Query;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;

namespace BrightstarDB.Update
{
    internal class BrightstarIOManager : IQueryableStorage
    {
        private readonly IStore _store;

        #region Implementation of IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            
        }

        #endregion

        public BrightstarIOManager(IStore store)
        {
            _store = store;
        }
        #region Implementation of IGenericIOManager

        public void LoadGraph(IGraph g, Uri graphUri)
        {
            throw new NotSupportedException();
        }

        public void LoadGraph(IGraph g, string graphUri)
        {
            throw new NotSupportedException();
        }

        public void LoadGraph(IRdfHandler handler, Uri graphUri)
        {
            throw new NotSupportedException();
        }

        public void LoadGraph(IRdfHandler handler, string graphUri)
        {
            throw new NotSupportedException();
        }

        public void SaveGraph(IGraph g)
        {
            throw new NotSupportedException();
        }

        public void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            UpdateGraph(graphUri == null ? null : graphUri.ToString(), additions, removals);
        }

        private static Model.Triple MakeBrighstarTriple(Triple t, string uniqueImportId)
        {
            var ret = new Model.Triple
            {
                Subject = Stringify(t.Subject, uniqueImportId),
                Predicate = Stringify(t.Predicate, uniqueImportId)
            };


            if (t.Object is UriNode)
            {
                ret.Object = t.Object.ToString();
            }
            else if (t.Object is LiteralNode)
            {
                var ln = (LiteralNode)t.Object;
                ret.DataType = ln.DataType == null ? Constants.DefaultDatatypeUri : ln.DataType.ToString();
                ret.IsLiteral = true;
                ret.Object = ln.Value;
                ret.LangCode = ln.Language;
            }
            else if (t.Object is BlankNode)
            {
                ret.Object = String.Format("{0}/{1}/{2}", Constants.GeneratedUriPrefix, uniqueImportId,
                                            ((BlankNode) t.Object).InternalID);
            }
            if (t.GraphUri != null)
            {
                ret.Graph = t.GraphUri.ToString();
            }
            return ret;
        }

        private static string Stringify(INode n, string uniqueImportId)
        {
            var node = n as BlankNode;
            if (node != null)
            {
                return String.Format("{0}/{1}/{2}", Constants.GeneratedUriPrefix, uniqueImportId,
                                            node.InternalID);
            }
            return n.ToString();
        }

        public void UpdateGraph(string graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            string uniqueImportId = Guid.NewGuid().ToString();
            if (graphUri == null) graphUri = Constants.DefaultGraphUri;
            if (removals != null)
            {
                var deleteSink = new DeletePatternSink(_store);
                foreach (var removal in removals)
                {
                    var node = removal.Object as ILiteralNode;
                    if (node != null)
                    {
                        deleteSink.Triple(
                            Stringify(removal.Subject, uniqueImportId), removal.Subject is IBlankNode,
                            Stringify(removal.Predicate, uniqueImportId), removal.Predicate is IBlankNode,
                            node.Value, false, true, node.DataType == null ? null : node.DataType.ToString(), node.Language,
                            removal.GraphUri == null ? graphUri : removal.GraphUri.ToString()
                            );
                    }
                    else
                    {
                        deleteSink.Triple(
                            Stringify(removal.Subject, uniqueImportId), removal.Subject is IBlankNode,
                            Stringify(removal.Predicate, uniqueImportId), removal.Predicate is IBlankNode,
                            Stringify(removal.Object, uniqueImportId), removal.Object is IBlankNode, false, null, null,
                            removal.GraphUri == null ? graphUri : removal.GraphUri.ToString()
                            );
                    }
                }
            }
            if (additions != null)
            {
                foreach (var addition in additions)
                {
                    var t = MakeBrighstarTriple(addition, uniqueImportId);
                    t.Graph = graphUri;
                    _store.InsertTriple(t);
                }
            }
        }

        public void DeleteGraph(Uri graphUri)
        {
            DeleteGraph(graphUri == null ? Constants.DefaultGraphUri : graphUri.ToString());
        }

        public void DeleteGraph(string graphUri)
        {
            _store.DeleteGraph(graphUri);
        }

        public void DeleteGraphs(IEnumerable<string> graphUris )
        {
            _store.DeleteGraphs(graphUris);
        }

        public IEnumerable<Uri> ListGraphs()
        {
            return _store.GetGraphUris().Where(g=>!Constants.DefaultGraphUri.Equals(g)).Select(g => new Uri(g));
        }

        public IStorageServer ParentServer
        {
            get { return null; }
        }

        public IOBehaviour IOBehaviour
        {
            get
            {
                return IOBehaviour.HasNamedGraphs | IOBehaviour.CanUpdateDeleteTriples | IOBehaviour.CanUpdateTriples |
                       IOBehaviour.CanUpdateAddTriples | IOBehaviour.HasDefaultGraph;
            }
        }

        public bool UpdateSupported
        {
            get { return true; }
        }

        public bool DeleteSupported
        {
            get { return true; }
        }

        public bool ListGraphsSupported
        {
            get { return true; }
        }

        public bool IsReady
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Implementation of IQueryableGenericIOManager

        public object Query(string sparqlQuery)
        {
            var sh = new SparqlQueryHandler();

            return sh.ExecuteSparql(ParseSparql(sparqlQuery), _store).RawResultSet;
        }

        public void Query(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery)
        {
            var sh = new SparqlQueryHandler();
            sh.ExecuteSparql(rdfHandler, resultsHandler, sparqlQuery, _store);
        }

        #endregion

        public void CopyGraph(Uri sourceUri, Uri destinationUri)
        {
            _store.CopyGraph(sourceUri == null ? Constants.DefaultGraphUri : sourceUri.ToString(),
                             destinationUri == null ? Constants.DefaultGraphUri : destinationUri.ToString());
        }
        public void MoveGraph(Uri sourceUri, Uri destinationUri)
        {
            _store.MoveGraph(sourceUri == null ? Constants.DefaultGraphUri : sourceUri.ToString(),
                             destinationUri == null ? Constants.DefaultGraphUri : destinationUri.ToString());
        }
        public void AddGraph(Uri sourceUri, Uri destinationUri)
        {
            _store.AddGraph(sourceUri == null ? Constants.DefaultGraphUri : sourceUri.ToString(),
                             destinationUri == null ? Constants.DefaultGraphUri : destinationUri.ToString());
        }

        private static SparqlQuery ParseSparql(string exp)
        {
            var parser = new SparqlQueryParser(SparqlQuerySyntax.Extended);
            var expressionFactories = parser.ExpressionFactories.ToList();
            expressionFactories.Add(new BrightstarFunctionFactory());
            parser.ExpressionFactories = expressionFactories;
            var query = parser.ParseFromString(exp);
            return query;
        }
    }
}
