using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BrightstarDB.Rdf;
using BrightstarDB.Utils;
using Remotion.Linq.Clauses;
using VDS.RDF;
using VDS.RDF.Parsing;
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
        private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private readonly IDataObjectStore _storeContext;
        internal SparqlResultDataObjectHelper(IDataObjectStore storeContext)
        {
            _storeContext = storeContext;
        }

        public IEnumerable<IDataObject> BindDataObjects(SparqlResult sparqlResult, IList<OrderingDirection> orderingDirections = null )
        {
            var xmlReader = sparqlResult.ResultDocument.CreateReader();
            xmlReader.MoveToContent();
            if (xmlReader.IsStartElement())
            {
                if ("RDF".Equals(xmlReader.LocalName) && RdfNamespace.Equals(xmlReader.NamespaceURI))
                {
                    return orderingDirections == null ?
                        BindRdfDataObjects(xmlReader) :
                        BindRdfDataObjects(sparqlResult.ResultDocument, orderingDirections);
                }
            }
            return BindDataObjects(xmlReader, sparqlResult.SourceSparqlQueryContext.ExpectTriplesWithOrderedSubjects);
        }

        public IEnumerable<IDataObject> BindRdfDataObjects(XDocument rdfXmlDocument,
                                                           IList<OrderingDirection> orderingDirections)
        {
            var g = new Graph();
#if PORTABLE
			var parser = new RdfXmlParser(RdfXmlParserMode.Streaming);
			// This is pretty nasty, having to deserialize only to go through parsing again
			parser.Load(g, new System.IO.StringReader(rdfXmlDocument.ToString()));
#else
			var parser = new RdfXmlParser(RdfXmlParserMode.DOM);
            parser.Load(g, rdfXmlDocument.AsXmlDocument());
#endif
            var p = new VDS.RDF.Query.LeviathanQueryProcessor(new InMemoryDataset(g));
            var queryString = MakeOrderedResourceQuery(orderingDirections);
            var sparqlParser = new SparqlQueryParser();
            var query = sparqlParser.ParseFromString(queryString);
            var queryResultSet = p.ProcessQuery(query) as VDS.RDF.Query.SparqlResultSet;
            foreach (var row in queryResultSet.Results)
            {
                INode uriNode;
                if (row.TryGetBoundValue("x", out uriNode) && uriNode is IUriNode)
                {
                    yield return BindRdfDataObject(uriNode as IUriNode, g);
                }
            }
        } 

        private IDataObject BindRdfDataObject(IUriNode dataObjectResource, IGraph graph)
        {
            var triples = new List<Triple>();
            foreach (var t in graph.GetTriplesWithSubject(dataObjectResource))
            {
                if (t.Subject is IUriNode && t.Predicate is IUriNode)
                {
                    // Only handling triples that have a URI predicate
                    // subject will always be a UriNode, because that is what we used in the lookup
                    var subject = (t.Subject as IUriNode).Uri.ToString();
                    var predicate = (t.Predicate as IUriNode).Uri.ToString();
                    if (t.Object is ILiteralNode)
                    {
                        var lit = t.Object as ILiteralNode;
                        triples.Add(new Triple
                            {
                                Subject = subject,
                                Predicate = predicate,
                                IsLiteral = true,
                                Object = lit.Value,
                                DataType = lit.DataType == null ? Constants.DefaultDatatypeUri : lit.DataType.ToString(),
                                LangCode = lit.Language
                            });
                    }
                    else if (t.Object is IUriNode)
                    {
                        var uriNode = t.Object as IUriNode;
                        triples.Add(new Triple
                            {
                                Subject = subject,
                                Predicate = predicate,
                                IsLiteral = false,
                                Object = uriNode.Uri.ToString()
                            });
                    }
                }
            }
            var dataObject = _storeContext.MakeDataObject(dataObjectResource.Uri.ToString()) as DataObject;
            dataObject.BindTriples(triples);
            return dataObject;
        }

        private string MakeOrderedResourceQuery(IList<OrderingDirection> orderingDirections)
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
             
        public IEnumerable<IDataObject> BindRdfDataObjects(XmlReader xmlReader)
        {
            SkipToStartElement(xmlReader);
            while(!xmlReader.EOF)
            {
                if (xmlReader.NodeType != XmlNodeType.Element) SkipToStartElement(xmlReader);
                if (!xmlReader.EOF)
                {
                    var resource = xmlReader.GetAttribute("about", RdfNamespace);
                    if (resource != null)
                    {
                        var dataobject = BindRdfDataObject(resource, xmlReader.NamespaceURI + xmlReader.LocalName,
                                                           xmlReader);
                        if (dataobject != null) yield return dataobject;
                    }
                    else
                    {
                        xmlReader.ReadInnerXml();
                    }
                }
            }            
        } 

        private IDataObject BindRdfDataObject(string resourceAddress, string resourceType, XmlReader reader)
        {
            var resourceElementLevel = reader.Depth;
            if (SkipToStartElement(reader))
            {
                var dataObject = _storeContext.MakeDataObject(resourceAddress) as DataObject;
                var triples = new List<Triple>();
                if (!String.IsNullOrEmpty(resourceType))
                {
                    triples.Add(new Triple{Subject = resourceAddress, Predicate = DataObject.TypeDataObject.Identity, Object = resourceType});
                }
                while(!reader.EOF)
                {
                    if (reader.Depth == resourceElementLevel)
                    {
                        break;
                    }
                    if (reader.IsStartElement())
                    {
                        var pred = reader.NamespaceURI + reader.LocalName;
                        var obj = reader.GetAttribute("resource", RdfNamespace);
                        if (obj != null)
                        {
                            triples.Add(new Triple {Subject = resourceAddress, Predicate = pred, Object = obj});
                            reader.Read();
                        }
                        else if (!reader.IsEmptyElement)
                        {
                            var dt = reader.GetAttribute("datatype", RdfNamespace);
                            var lang = reader.GetAttribute("xml:lang");
                            obj = reader.ReadElementContentAsString();
                            triples.Add(new Triple
                                            {
                                                Subject = resourceAddress,
                                                Predicate = pred,
                                                IsLiteral = true,
                                                Object = obj,
                                                LangCode = lang,
                                                DataType = dt ?? RdfDatatypes.String
                                            });
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                } 
                dataObject.BindTriples(triples);
                return dataObject;
            }
            return null;
        }

        private bool SkipToStartElement(XmlReader reader)
        {
            while (!reader.EOF)
            {
                reader.Read();
                if (reader.IsStartElement()) return true;
            }
            return false;
        }

        public IEnumerable<IDataObject> BindDataObjects(XmlReader xmlReader, bool resultsAreOrdered = false)
        {
            var variables = new List<string>();
            var xmlResultNodeTripleValues = new List<string>();
            bool readingResults = false;
            var resourceTriples = new Dictionary<string, List<Triple>>();
            string lastLoadedSubject = null; //if resultsAreOrdered then once we get a new subject we can create the DataObject

            while (xmlReader.Read())
            {
                if (xmlReader.NamespaceURI.Equals("http://www.w3.org/2005/sparql-results#")
                    && xmlReader.NodeType == XmlNodeType.Element)
                {
                    var nodeName = xmlReader.Name.ToLower();
                    if (!readingResults) //header part
                    {
                        if (nodeName.Equals("variable"))
                        {
                            variables.Add(xmlReader.GetAttribute("name"));
                            continue;
                        }
                        else if (nodeName.Equals("results"))
                        {
                            readingResults = true;
                            if (variables.Count != 1 && variables.Count != 3)
                            {
                                throw new NotSupportedException("Sparql results can be a list of id's(1 variable) or a list of triples(3 variables). Variables found:" + variables.Count);
                            }
                            continue;
                        }
                    }
                    else //reading results
                    {
                        if (variables.Count == 1) //load id's
                        {
                            if (nodeName.Equals("uri"))
                            {
                                string uri = null;
                                try
                                {
                                    uri = xmlReader.ReadElementContentAsString();
                                }
                                catch (Exception ex)
                                {
                                    Logging.LogError(BrightstarEventId.ClientDataBindError,
                                        "Error binding to SPARQL results element. {0}", ex);
                                }
                                if (!String.IsNullOrEmpty(uri))
                                {
                                    yield return _storeContext.MakeDataObject(uri);
                                }
                            }
                        }
                        else //load triples
                        {
                            var isUri = nodeName.Equals("uri");
                            var isLiteral = nodeName.Equals("literal");
                            string literalDataType = null;
                            string literalLanguage = null;
                            if (isUri || isLiteral)
                            {
                                string elementContentAsString = null;
                                try
                                {
                                    literalDataType = xmlReader.GetAttribute("datatype");
                                    literalLanguage = xmlReader.GetAttribute("xml:lang");
                                    elementContentAsString = xmlReader.ReadElementContentAsString();
                                }
                                catch (Exception ex)
                                {
                                    Logging.LogError(BrightstarEventId.ClientDataBindError,
                                        "Error binding to SPARQL results element. {0}", ex);
                                }
                                if (!String.IsNullOrEmpty(elementContentAsString))
                                {
                                    xmlResultNodeTripleValues.Add(elementContentAsString);
                                }
                            }

                            //create new triple
                            if (xmlResultNodeTripleValues.Count == 3)
                            {
                                var s = xmlResultNodeTripleValues[0];

                                //if object was a literal it's the last read value => datatype, lang must match
                                var triple = new Triple
                                {
                                    Subject = s,
                                    Predicate = xmlResultNodeTripleValues[1],
                                    IsLiteral = isLiteral,
                                    Object = xmlResultNodeTripleValues[2],
                                    LangCode = literalLanguage,
                                    DataType = literalDataType ?? RdfDatatypes.String
                                };


                                if (resourceTriples.ContainsKey(s)) //resource already has triples
                                {
                                    resourceTriples[s].Add(triple);
                                }
                                else
                                {
                                    resourceTriples.Add(s, new List<Triple> { triple });
                                }

                                //if results are in order and we have new subject then we can create a new object for the previous one
                                if (resultsAreOrdered && lastLoadedSubject != null && lastLoadedSubject != s)
                                {
                                    var dataObject = _storeContext.MakeDataObject(lastLoadedSubject) as DataObject;
                                    dataObject.BindTriples(resourceTriples[lastLoadedSubject]);
                                    yield return dataObject;
                                }

                                lastLoadedSubject = s;
                                xmlResultNodeTripleValues.Clear();
                            }
                        }
                    }
                }

            }
            xmlReader.Close();


            if (resultsAreOrdered && lastLoadedSubject != null)
            {
                var dataObject = _storeContext.MakeDataObject(lastLoadedSubject) as DataObject;
                dataObject.BindTriples(resourceTriples[lastLoadedSubject]);
                yield return dataObject;
            }
            else
            {
                foreach (KeyValuePair<string, List<Triple>> resourceTriple in resourceTriples)
                {
                    var dataObject = _storeContext.MakeDataObject(resourceTriple.Key) as DataObject;
                    dataObject.BindTriples(resourceTriple.Value);

                    yield return dataObject;
                }
            }

        }

    }
}
