using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Query.Datasets;

namespace BrightstarDB.Query
{
    internal class StoreSparqlDataset : ISparqlDataset
    {
        /// <summary>
        /// The store againts which we are running the query
        /// </summary>
        private readonly IStore _store;

        private List<string> _graphUris = new List<string>{Constants.DefaultGraphUri};
        private List<string> _defaultGraphUris = new List<string>{Constants.DefaultGraphUri};

        public StoreSparqlDataset(IStore store)
        {
            _store = store;
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
            var objLit = t.Object as LiteralNode;
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

        private static Triple MakeVdsTriple(Model.Triple triple)
        {
            if (triple.IsLiteral)
            {
                LiteralNode literalNode = BrightstarLiteralNode.Create(triple.Object, triple.DataType, triple.LangCode);
                return new Triple(MakeVdsNode(triple.Subject), MakeVdsNode(triple.Predicate), literalNode);
            }
            return new Triple(MakeVdsNode(triple.Subject), MakeVdsNode(triple.Predicate), MakeVdsNode(triple.Object));
        }

        private static INode MakeVdsNode(string identifier)
        {
            return new BrightstarUriNode(new Uri(identifier));

            //return identifier.StartsWith("_:")
            //           ? new BrightstarBlankNode(identifier.Substring(2))
            //           : new BrightstarUriNode(new Uri(identifier)) as INode;
        }

        public IEnumerable<Triple> GetTriplesWithSubject(INode subj)
        {
            // dotNetRDF doesn't filter out these cases
            if (subj.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }


            return _store.Match(GetNodeMatchString(subj), null, null, graphs: _graphUris).Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithPredicate(INode pred)
        {
            // dotNetRDF doesn't filter out these cases
            if (pred.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }

            return _store.Match(null, GetNodeMatchString(pred), null, graphs: _graphUris).Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithObject(INode obj)
        {
            var objLit = obj as LiteralNode;

            if (objLit != null)
            {
                var dataType = RdfDatatypes.PlainLiteral;
                if (objLit.DataType != null)
                {
                    dataType = objLit.DataType.ToString();
                }
                return
                    _store.Match(
                        null, null, objLit.Value,
                        true, dataType, objLit.Language, _graphUris)
                        .Select(MakeVdsTriple);
            }
            return _store.Match(null, null, GetNodeMatchString(obj), false, null, null, _graphUris).Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithSubjectPredicate(INode subj, INode pred)
        {
            // dotNetRDF doesn't filter out these cases
            if (subj.NodeType == NodeType.Literal || pred.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }
            return _store.Match(GetNodeMatchString(subj),
                                GetNodeMatchString(pred), 
                                null, 
                                graphs: _graphUris)
                .Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithSubjectObject(INode subj, INode obj)
        {
            // dotNetRDF doesn't filter out these cases
            if (subj.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }
            var objLit = obj as LiteralNode;
            if (objLit != null)
            {
                var dataType = RdfDatatypes.PlainLiteral;
                if (objLit.DataType != null)
                {
                    dataType = objLit.DataType.ToString();
                }
                return
                    _store.Match(GetNodeMatchString(subj),
                                 null,
                                 objLit.Value,
                                 true, dataType, objLit.Language,
                                 _graphUris)
                        .Select(MakeVdsTriple);
            }
            return
                _store.Match(GetNodeMatchString(subj),
                             null,
                             GetNodeMatchString(obj),
                             false, null, null,
                             _graphUris)
                    .Select(MakeVdsTriple);
        }

        public IEnumerable<Triple> GetTriplesWithPredicateObject(INode pred, INode obj)
        {
            // dotNetRDF doesn't filter out these cases
            if (pred.NodeType == NodeType.Literal)
            {
                // Can't be a match if there is a literal for subject or predicate
                return new Triple[0];
            }


            var objLit = obj as LiteralNode;

            if (objLit != null)
            {
                var dataType = RdfDatatypes.PlainLiteral;
                if (objLit.DataType != null)
                {
                    dataType = objLit.DataType.ToString();
                }
                return
                    _store.Match(
                        null,
                        GetNodeMatchString(pred),
                        objLit.Value,
                        true, dataType, objLit.Language,
                        _graphUris)
                        .Select(MakeVdsTriple);
            }
            return
                _store.Match(null,
                             GetNodeMatchString(pred),
                             GetNodeMatchString(obj),
                             false, null, null,
                             _graphUris)
                    .Select(MakeVdsTriple);
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
                //return _store.Match(null, null, null).Select(MakeVdsTriple);
                return _store.Match(null, null, null, false, null, null, _graphUris).Select(MakeVdsTriple);
            }
        }
    }
}