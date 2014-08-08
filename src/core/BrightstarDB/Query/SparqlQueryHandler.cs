using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightstarDB.Storage;
using BrightstarDB.Update;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace BrightstarDB.Query
{
    internal class SparqlQueryHandler
    {
        private readonly List<Uri> _defaultGraphUris;
        private readonly SparqlResultsFormat _sparqlResultsFormat = SparqlResultsFormat.Xml;
        private readonly RdfFormat _rdfFormat = RdfFormat.RdfXml;

        public SparqlQueryHandler()
        {
            _defaultGraphUris = null;
        }


        public SparqlQueryHandler(IEnumerable<string> defaultGraphUris = null)
        {
            if (defaultGraphUris != null)
            {
                _defaultGraphUris = defaultGraphUris.Select(g => new Uri(g)).ToList();
            }
        }

        public SparqlQueryHandler(ISerializationFormat targetFormat,
                                  IEnumerable<string> defaultGraphUris)
        {
            if (targetFormat is SparqlResultsFormat)
            {
                _sparqlResultsFormat = targetFormat as SparqlResultsFormat;
            }
            if (targetFormat is RdfFormat)
            {
                _rdfFormat = targetFormat as RdfFormat;
            }
            if (defaultGraphUris != null)
            {
                _defaultGraphUris = defaultGraphUris.Select(g => new Uri(g)).ToList();
            }
        }

        private static ISparqlDataset MakeDataset(IStore store)
        {
            if (Configuration.EnableVirtualizedQueries)
            {
                return new VirtualizingSparqlDataset(store);
            }
            return new StoreSparqlDataset(store);
        }

        public BrightstarSparqlResultsType ExecuteSparql(SparqlQuery query, IStore store, TextWriter resultsWriter)
        {
            try
            {
                EnsureValidResultFormat(query);

                var dataset = MakeDataset(store);
                if (_defaultGraphUris != null)
                {
                    dataset.SetDefaultGraph(_defaultGraphUris);
                }

                var queryProcessor = new BrightstarQueryProcessor(store, dataset);
                var queryResult = queryProcessor.ProcessQuery(query);
                if (queryResult is SparqlResultSet)
                {
                    var sparqlResultSet = (SparqlResultSet) queryResult;
                    ISparqlResultsWriter sparqlResultsWriter = null;
                    if (_sparqlResultsFormat != null)
                    {
                        sparqlResultsWriter =
                            MimeTypesHelper.GetSparqlWriter(new string[] {_sparqlResultsFormat.ToString()});
                    }
                    if (sparqlResultsWriter == null)
                    {
                        throw new NoAcceptableFormatException(typeof (SparqlResultsFormat),
                                                              "No acceptable format provided for writing a SPARQL result set.");
                    }
                    sparqlResultsWriter.Save(sparqlResultSet, resultsWriter);
                    switch (sparqlResultSet.ResultsType)
                    {
                        case SparqlResultsType.Boolean:
                            return BrightstarSparqlResultsType.Boolean;
                        case SparqlResultsType.VariableBindings:
                            return BrightstarSparqlResultsType.VariableBindings;
                        default:
                            throw new BrightstarInternalException("Unrecognized SPARQL result type");
                    }
                }
                if (queryResult is IGraph)
                {
                    var g = (IGraph) queryResult;
                    var rdfWriter = _rdfFormat == null
                                        ? null
                                        : MimeTypesHelper.GetWriter(new string[] {_rdfFormat.ToString()});
                    if (rdfWriter == null)
                    {
                        throw new NoAcceptableFormatException(typeof (RdfFormat),
                                                              "No acceptable format provided for writing an RDF graph result.");
                    }
                    rdfWriter.Save(g, resultsWriter);
                    return BrightstarSparqlResultsType.Graph;
                }
                throw new BrightstarInternalException(
                    String.Format("Unexpected return type from QueryProcessor.ProcessQuery: {0}",
                                  queryResult.GetType()));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.SparqlExecutionError,
                                 "Error Executing query {0}. Cause: {1}",
                                 query.ToString(), ex);
                throw;
            }
        }

        private void EnsureValidResultFormat(SparqlQuery query)
        {
            if (query.QueryType == SparqlQueryType.Construct ||
                query.QueryType == SparqlQueryType.Describe ||
                query.QueryType == SparqlQueryType.DescribeAll)
            {
                if (_rdfFormat == null) throw new NoAcceptableFormatException(typeof (RdfFormat),
                    "CONSTRUCT and DESCRIBE queries require an RdfFormat specifier for the RDF graph serialization.");
            }
            else if (_sparqlResultsFormat == null)
            {
                throw new NoAcceptableFormatException(typeof (SparqlResultsFormat),
                    "Query requires a SparqlResultsFormat specifier for the results serialization.");
            }
        }

        
        /// <summary>
        /// Provides the required SPARQL query interface for the <see cref="BrightstarIOManager"/> used for SPARQL update support
        /// </summary>
        /// <param name="rdfHandler"></param>
        /// <param name="resultsHandler"></param>
        /// <param name="sparqlQuery"></param>
        /// <param name="store"></param>
        public void ExecuteSparql(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery, IStore store)
        {
            try
            {
                var query = ParseSparql(sparqlQuery);
                var dataset = MakeDataset(store);
                if (_defaultGraphUris != null)
                {
                    dataset.SetDefaultGraph(_defaultGraphUris);
                }
                var queryProcessor = new BrightstarQueryProcessor(store, dataset);
                queryProcessor.ProcessQuery(rdfHandler, resultsHandler, query);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.SparqlExecutionError,
                                 "Error Executing Sparql {0}. Cause: {1}",
                                 sparqlQuery, ex);
                throw;
            }
        }


        public BrightstarSparqlResultSet ExecuteSparql(SparqlQuery query, IStore store)
        {
            try
            {
                var dataset = MakeDataset(store);
                if (_defaultGraphUris != null)
                {
                    dataset.SetDefaultGraph(_defaultGraphUris);
                }
                var queryProcessor = new BrightstarQueryProcessor(store, dataset);
                return new BrightstarSparqlResultSet(queryProcessor.ProcessQuery(query));
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.SparqlExecutionError,
                    "Error Executing Sparql {0}. Cause: {1}", 
                    query, ex);
                throw;
            }
        }

        private static readonly object SparqlParserLock = new object();

        private static SparqlQuery ParseSparql(string exp)
        {
            var parser = new SparqlQueryParser(SparqlQuerySyntax.Extended);
            var expressionFactories = parser.ExpressionFactories.ToList();
            expressionFactories.Add(new BrightstarFunctionFactory());
            parser.ExpressionFactories = expressionFactories;
            lock (SparqlParserLock)
            {
                // Lock is required because dotNetRDF currently has
                // a contention issue in its Trie implementation
                // This may be fixed in 0.7.1
                // TODO: Check if this is needed when we upgrade to 0.7.x
                var query = parser.ParseFromString(exp);
                return query;
            }
        }
    }
}
