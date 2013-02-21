using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Query;
using BrightstarDB.Storage;
using VDS.RDF;
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
            var ret = new Model.Triple();
            if (t.Subject is BlankNode)
            {
                ret.Subject = String.Format("{0}/{1}/{2}", Constants.GeneratedUriPrefix, uniqueImportId,
                                            (t.Subject as BlankNode).InternalID);
            }
            else
            {
                ret.Subject = t.Subject.ToString();
            }

            if (t.Predicate is BlankNode)
            {
                ret.Predicate = String.Format("{0}/{1}/{2}", Constants.GeneratedUriPrefix, uniqueImportId,
                                            (t.Predicate as BlankNode).InternalID);
            }
            else
            {
                ret.Predicate = t.Predicate.ToString();
            }

            if (t.Object is UriNode)
            {
                ret.Object = t.Object.ToString();
            }
            else if (t.Object is LiteralNode)
            {
                var ln = t.Object as LiteralNode;
                ret.DataType = ln.DataType == null ? Constants.DefaultDatatypeUri : ln.DataType.ToString();
                ret.IsLiteral = true;
                ret.Object = ln.Value;
                ret.LangCode = ln.Language;
            }
            else if (t.Object is BlankNode)
            {
                ret.Object = String.Format("{0}/{1}/{2}", Constants.GeneratedUriPrefix, uniqueImportId,
                                            (t.Object as BlankNode).InternalID);
            }
            if (t.GraphUri != null)
            {
                ret.Graph = t.GraphUri.ToString();
            }
            return ret;
        }

        public void UpdateGraph(string graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            string uniqueImportId = Guid.NewGuid().ToString();
            if (graphUri == null) graphUri = Constants.DefaultGraphUri;
            if (removals != null)
            {
                foreach (var removal in removals)
                {
                    var t = MakeBrighstarTriple(removal, uniqueImportId);
                    t.Graph = graphUri;
                    _store.DeleteTriple(t);
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
            return sh.ExecuteSparql(sparqlQuery, _store).RawResultSet;
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
    }
}
