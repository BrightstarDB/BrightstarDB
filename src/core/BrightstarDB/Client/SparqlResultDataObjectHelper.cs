using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Remotion.Linq.Clauses;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using Triple = BrightstarDB.Model.Triple;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Given a sparql result stream will lazily return DataObjects via the specified context.
    /// All objects returned will be stub's and not have any data loaded.
    /// </summary>
    internal class SparqlResultDataObjectHelper
    {
        private readonly IDataObjectStore _storeContext;
        internal SparqlResultDataObjectHelper(IDataObjectStore storeContext)
        {
            _storeContext = storeContext;
        }

        public IEnumerable<IDataObject> BindDataObjects(SparqlResult sparqlResult, IList<OrderingDirection> orderingDirections = null )
        {
            if (sparqlResult.IsGraphResult)
            {
                return orderingDirections == null
                    ? BindRdfDataObjects(sparqlResult.ResultGraph)
                    : BindRdfDataObjects(sparqlResult.ResultGraph, orderingDirections);
            }
            return BindDataObjects(sparqlResult.ResultSet, sparqlResult.SourceSparqlQueryContext.ExpectTriplesWithOrderedSubjects);
        }

        public IEnumerable<IDataObject> BindRdfDataObjects(IGraph g,
                                                           IList<OrderingDirection> orderingDirections)
        {
            var p = new LeviathanQueryProcessor(new InMemoryDataset(g));
            var queryString = MakeOrderedResourceQuery(orderingDirections);
            var sparqlParser = new SparqlQueryParser();
            var query = sparqlParser.ParseFromString(queryString);
            var queryResultSet = p.ProcessQuery(query) as SparqlResultSet;
            if (queryResultSet != null)
            {
                foreach (var row in queryResultSet.Results)
                {
                    INode uriNode;
                    if (row.TryGetBoundValue("x", out uriNode) && uriNode is IUriNode)
                    {
                        yield return BindRdfDataObject((IUriNode) uriNode, g);
                    }
                }
            }
        } 

        private IDataObject BindRdfDataObject(IUriNode dataObjectResource, IGraph graph)
        {
            var triples =
                graph.GetTriplesWithSubject(dataObjectResource)
                    .Where(t => t.Subject is IUriNode && t.Predicate is IUriNode)
                    .Select(t => MakeTriple(t.Subject, t.Predicate, t.Object));
            return MakeDataObject(dataObjectResource.Uri.ToString(), triples);
        }

        private static string MakeOrderedResourceQuery(IList<OrderingDirection> orderingDirections)
        {
            var query = new StringBuilder();
            query.Append("SELECT ?x WHERE { ?x <" + Constants.SelectVariablePredicateUri + "> ?sv .");
            for (var i = 0; i < orderingDirections.Count; i++)
            {
                query.AppendFormat("?x <" + Constants.SortValuePredicateBase + "{0}> ?sortValue{0} .", i);
            }
            query.Append("}");
            if (orderingDirections.Count > 0)
            {
                query.Append(" ORDER BY ");
                for (var i = 0; i < orderingDirections.Count; i++)
                {
                    query.AppendFormat(
                        orderingDirections[i] == OrderingDirection.Desc ? " DESC(?sortValue{0})" : " ?sortValue{0} ", i);
                }
            }
            return query.ToString();
        }

        public IEnumerable<IDataObject> BindRdfDataObjects(IGraph graph)
        {
            var distinctSubjects = new System.Collections.Generic.HashSet<INode>();
            foreach (var t in graph.Triples)
            {
                distinctSubjects.Add(t.Subject);
            }
            return distinctSubjects.Select(s =>
                MakeDataObject(s.ToString(),
                    graph.GetTriplesWithSubject(s).Select(t =>
                        MakeTriple(t.Subject, t.Predicate, t.Object))));
        } 



        public IEnumerable<IDataObject> BindDataObjects(SparqlResultSet sparqlResultSet, bool resultsAreOrdered = false)
        {
            if (sparqlResultSet == null) throw new ArgumentNullException(nameof(sparqlResultSet));
            var resourceTriples = new Dictionary<string, List<Triple>>();
            string lastLoadedSubject = null;
            switch (sparqlResultSet.Variables.Count())
            {
                case 1:
                    // Single column results set contains only data object IRIs
                    foreach (var uriNode in sparqlResultSet.Select(row => row[0] as IUriNode).Where(uriNode => uriNode?.Uri != null))
                    {
                        yield return _storeContext.MakeDataObject(uriNode.Uri.ToString());
                    }
                    break;
                case 3:
                    // Columns are triples, s, p, o in that order
                    foreach (var t in sparqlResultSet.Select(row=>MakeTriple(row[0], row[1], row[2])))
                    {
                        if (resourceTriples.ContainsKey(t.Subject))
                        {
                            resourceTriples[t.Subject].Add(t);
                        }
                        else
                        {
                            resourceTriples[t.Subject] = new List<Triple> {t};
                        }
                        if (resultsAreOrdered && lastLoadedSubject != null && !lastLoadedSubject.Equals(t.Subject))
                        {
                            // Have collected all the triples we are going to see for the previously encountered subject, so emit its data object now
                            yield return MakeDataObject(lastLoadedSubject, resourceTriples[lastLoadedSubject]);
                        }
                        lastLoadedSubject = t.Subject;
                    }
                    if (resultsAreOrdered && lastLoadedSubject != null)
                    {
                        // Emit the final result
                        yield return MakeDataObject(lastLoadedSubject, resourceTriples[lastLoadedSubject]);
                    }
                    else
                    {
                        // We have batched up all of the triples and can now emit the separate data objects
                        foreach (var entry in resourceTriples)
                        {
                            yield return MakeDataObject(entry.Key, entry.Value);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException(
                       $"Expected a result set with either 1 or 3 columns. Got a result set with {sparqlResultSet.Variables.Count()} columns", nameof(sparqlResultSet));
            }
        }

        private DataObject MakeDataObject(string identity, IEnumerable<Triple> triples)
        {
            var dataObject = _storeContext.MakeDataObject(identity) as DataObject;
            if (dataObject == null) return null;
            dataObject.BindTriples(triples);
            return dataObject;
        }

        private static Triple MakeTriple(INode subjectNode, INode predicateNode, INode objectNode)
        {
            var litNode = objectNode as ILiteralNode;
            var t = new Triple
            {
                Subject = subjectNode.ToString(),
                Predicate = predicateNode.ToString(),
                IsLiteral = objectNode is ILiteralNode,
                Object = litNode != null ? litNode.Value : objectNode.ToString()
            };
            if (litNode == null) return t;
            t.DataType = litNode?.DataType?.ToString() ?? Constants.DefaultDatatypeUri;
            t.LangCode = litNode?.Language;
            return t;
        }

    }
}
