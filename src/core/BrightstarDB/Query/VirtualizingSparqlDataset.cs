using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Storage.Virtualisation;

namespace BrightstarDB.Query
{
    /// <summary>
    /// An implementation of the DotNetRDF ISparqlDataset interface that returns triples
    /// containing virtual nodes from its GetTriples... methods
    /// </summary>
    internal class VirtualizingSparqlDataset : ISparqlDataset
    {
        /// <summary>
        /// The store againts which we are running the query
        /// </summary>
        private readonly IStore _store;

        /// <summary>
        /// The instance responsible for materializing virtual nodes from this dataset.
        /// </summary>
        private IVirtualRdfProvider<ulong, int> _rdfProvider;

        private List<string> _graphUris = new List<string>{Constants.DefaultGraphUri};
        private List<string> _defaultGraphUris = new List<string>{Constants.DefaultGraphUri};

        public VirtualizingSparqlDataset(IStore store)
        {
            _store = store;
            _rdfProvider = new BrightstarRdfProvider(_store);
        }

        public void SetActiveGraph(IEnumerable<Uri> graphUris)
        {
            _graphUris = graphUris.Select(g => g.ToString()).ToList();
        }

        public void SetActiveGraph(Uri graphUri)
        {
            _graphUris.Clear();
            _graphUris.Add(graphUri.ToString());
        }

        public void SetDefaultGraph(Uri graphUri)
        {
            _defaultGraphUris.Clear();
            _defaultGraphUris.Add(graphUri.ToString());
        }

        public void SetDefaultGraph(IEnumerable<Uri> graphUris)
        {
            _defaultGraphUris = graphUris.Select(g => g.ToString()).ToList();
        }

        public void SetActiveGraph(IGraph g)
        {
            _graphUris.Clear();
            var graphUri = g == null ? Constants.DefaultGraphUri : g.BaseUri.ToString();
            _graphUris.Add(graphUri);
        }

        public void SetDefaultGraph(IGraph g)
        {
            //_defaultGraphUri = g == null ? Constants.DefaultGraphUri : g.BaseUri.ToString();
            if (g == null)
            {
                SetDefaultGraph(new Uri(Constants.DefaultGraphUri));
            }
            else
            {
                SetDefaultGraph(g.BaseUri);
            }
        }

        public void ResetActiveGraph()
        {
            _graphUris.Clear();
            _graphUris.AddRange(_defaultGraphUris);
        }

        public void ResetDefaultGraph()
        {
            _defaultGraphUris.Clear();
        }

        public bool AddGraph(IGraph g)
        {
            // we don't support IGraph based operations
            throw new NotSupportedException();
        }

        public bool RemoveGraph(Uri graphUri)
        {
            // we don't support IGraph based operations
            throw new NotSupportedException();
        }

        public bool HasGraph(Uri graphUri)
        {
            return _store.GetGraphUris().Contains(graphUri.ToString());
        }

        public IGraph GetModifiableGraph(Uri graphUri)
        {
            // we don't support IGraph based operations
            throw new NotSupportedException();
        }

        public bool ContainsTriple(Triple t)
        {
            var objLit = t.Object as ILiteralNode;
            if (objLit != null)
            {
                var dataType = RdfDatatypes.PlainLiteral;
                if (objLit.DataType != null) dataType = objLit.DataType.ToString();
                return
                    _store.Match(GetNodeMatchString(t.Subject),
                                 GetNodeMatchString(t.Predicate),
                                 objLit.Value,
                                 true, dataType,
                                 objLit.Language, _graphUris).Any();
            }
            return
                _store.Match(GetNodeMatchString(t.Subject),
                             GetNodeMatchString(t.Predicate),
                             GetNodeMatchString(t.Object),
                             false, null, null, _graphUris)
                    .Any();
        }

        private Triple MakeVdsTriple(Tuple<ulong, ulong, ulong, int> triple)
        {
            return new Triple(MakeVirtualNode(triple.Item1, triple.Item4),
                              MakeVirtualNode(triple.Item2, triple.Item4),
                              MakeVirtualNode(triple.Item3, triple.Item4));
        }

        private INode MakeVirtualNode(ulong nodeId, int graphId)
        {
            return new BrightstarVirtualNode(nodeId, graphId, _rdfProvider);
        }

        public IEnumerable<Triple> GetTriplesWithSubject(INode subj)
        {
            // dotNetRDF doesn't filter out these cases
            if (subj.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }
            return GetTriples(subj, null, null);
            //return _store.GetBindings(GetNodeMatchString(subj), null, null, graphs: _graphUris).Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(INode pred)
        {
            // dotNetRDF doesn't filter out these cases
            if (pred.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }
            return GetTriples(null, pred, null);
            //return _store.GetBindings(null, GetNodeMatchString(pred), null, graphs: _graphUris).Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithObject(INode obj)
        {
            return GetTriples(null, null, obj);
            //if (obj.NodeType == NodeType.Literal)
            //{
            //    var objLit = obj as ILiteralNode;
            //    var dataType = RdfDatatypes.PlainLiteral;
            //    if (objLit.DataType != null)
            //    {
            //        dataType = objLit.DataType.ToString();
            //    }
            //    return
            //        _store.GetBindings(
            //            null, null, objLit.Value,
            //            true, dataType, objLit.Language, _graphUris)
            //              .Select(MakeVdsTriple);
            //}
            //return _store.GetBindings(null, null, GetNodeMatchString(obj), false, null, null, _graphUris).Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            // dotNetRDF doesn't filter out these cases
            if (subj.NodeType == NodeType.Literal || pred.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }
            return GetTriples(subj, pred, null);
            //return _store.GetBindings(GetNodeMatchString(subj),
            //                    GetNodeMatchString(pred), 
            //                    null, 
            //                    graphs: _graphUris)
            //    .Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj)
        {
            // dotNetRDF doesn't filter out these cases
            if (subj.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }
            return GetTriples(subj, null, obj);
            //if (obj.NodeType == NodeType.Literal)
            //{
            //    var objLit = obj as ILiteralNode;
            //    var dataType = RdfDatatypes.PlainLiteral;
            //    if (objLit.DataType != null)
            //    {
            //        dataType = objLit.DataType.ToString();
            //    }
            //    return
            //        _store.GetBindings(GetNodeMatchString(subj),
            //                            null,
            //                            objLit.Value,
            //                            true, dataType, objLit.Language,
            //                            _graphUris)
            //                .Select(MakeVdsTriple);
            //}
            //return
            //    _store.GetBindings(GetNodeMatchString(subj),
            //                 null,
            //                 GetNodeMatchString(obj),
            //                 false, null, null,
            //                 _graphUris)
            //        .Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            // dotNetRDF doesn't filter out these cases
            if (pred.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }

            return GetTriples(null, pred, obj);

            //if (obj.NodeType == NodeType.Literal)
            //{
            //    var objLit = obj as ILiteralNode;
            //    var dataType = RdfDatatypes.PlainLiteral;
            //    if (objLit.DataType != null)
            //    {
            //        dataType = objLit.DataType.ToString();
            //    }
            //    return
            //        _store.GetBindings(
            //            null,
            //            GetNodeMatchString(pred),
            //            objLit.Value,
            //            true, dataType, objLit.Language,
            //            _graphUris)
            //                .Select(MakeVdsTriple);
            //}
            //return
            //    _store.GetBindings(null,
            //                 GetNodeMatchString(pred),
            //                 GetNodeMatchString(obj),
            //                 false, null, null,
            //                 _graphUris)
            //        .Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriples(INode subj, INode pred, INode obj)
        {
            ulong? subjNodeId = null, predNodeId = null, objNodeId = null;
            if (subj == null) subjNodeId = StoreConstants.NullUlong;
            if (pred == null) predNodeId = StoreConstants.NullUlong;
            if (obj == null) objNodeId = StoreConstants.NullUlong;
            if (subj is BrightstarVirtualNode) subjNodeId = (subj as BrightstarVirtualNode).VirtualID;
            if (pred is BrightstarVirtualNode) predNodeId = (pred as BrightstarVirtualNode).VirtualID;
            if (obj is BrightstarVirtualNode) objNodeId = (obj as BrightstarVirtualNode).VirtualID;

            string subjValue = null, predValue = null, objValue = null, dataType = RdfDatatypes.PlainLiteral, languageCode = null;
            bool objIsLiteral = false;

            if (!subjNodeId.HasValue)
            {
                if (subj is ILiteralNode) return new Triple[0];
                subjValue = GetNodeMatchString(subj);
            }
            if (!predNodeId.HasValue)
            {
                if (pred is ILiteralNode) return new Triple[0];
                predValue = GetNodeMatchString(pred);
            }
            if (!objNodeId.HasValue)
            {
                if (obj is ILiteralNode)
                {
                    var lit = obj as ILiteralNode;
                    objValue = lit.Value;
                    dataType = lit.DataType == null ? RdfDatatypes.PlainLiteral : lit.DataType.ToString();
                    languageCode = lit.Language;
                    objIsLiteral = true;
                }
                else
                {
                    objValue = GetNodeMatchString(obj);
                }
            }

            return _store.GetBindings(subjNodeId, subjValue,
                                      predNodeId, predValue,
                                      objNodeId, objValue, objIsLiteral, dataType, languageCode,
                                      _graphUris).Select(MakeVdsTriple);
        } 

        private static string GetNodeMatchString(INode node)
        {
            switch (node.NodeType)
            {
                case NodeType.Uri:
                    return ((IUriNode) node).Uri.ToString();
                case NodeType.Literal:
                    return ((ILiteralNode) node).Value;
                case NodeType.Blank:
                    // return ((IBlankNode)node).InternalID;
                    var s = node.ToString();
                    return s;
                default:
                    throw new BrightstarInternalException(
                        String.Format("Cannot convert node of type {0} to a node match string", node.GetType()));
            }
        }


        public void Flush()
        {
            return;
        }

        public void Discard()
        {
            // Nothing to do in this implementation yet
        }

        public IEnumerable<Uri> DefaultGraphUris
        {
            get { return _defaultGraphUris.Select(s=>new Uri(s)); }
        }

        public IEnumerable<string> GetActiveGraphUris()
        {
            return _graphUris;
        } 

        public IEnumerable<Uri> ActiveGraphUris
        {
            get { return _graphUris.Select(s=>new Uri(s)); }
        }

        public IGraph DefaultGraph
        {
            get { return null; }
        }

        public IGraph ActiveGraph
        {
            get { return null; }
        }

        public bool UsesUnionDefaultGraph
        {
            get { return true; }
        }

        public IEnumerable<IGraph> Graphs
        {
            get { return null; }
        }

        public IEnumerable<Uri> GraphUris
        {
            get
            {
                return _store.GetGraphUris().Where(x=>!x.Equals(Constants.DefaultGraphUri)).Select(x => new Uri(x));
            }
        }

        public IGraph this[Uri graphUri]
        {
            get { throw new NotSupportedException(); }
        }

        public bool HasTriples
        {
            get { return true; }
        }

        public IEnumerable<Triple> Triples
        {
            get
            {
                return _store.GetBindings(null, null, null, false, null, null, _graphUris).Select(MakeVdsTriple);
            }
        }
    }
}
