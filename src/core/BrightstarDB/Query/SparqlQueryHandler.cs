using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace BrightstarDB.Query
{
    internal class SparqlQueryHandler
    {
        private readonly List<Uri> _defaultGraphUris;

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

        public void ExecuteSparql(IRdfHandler rdfHandler, ISparqlResultsHandler resultsHandler, string sparqlQuery, IStore store)
        {
            try
            {
                var query = ParseSparql(sparqlQuery);
                var dataset = new StoreSparqlDataset(store);
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


        public BrightstarSparqlResultSet ExecuteSparql(string expression, IStore store)
        {
            try
            {
                Logging.LogDebug("ExecuteSparql {0}", expression);
                var query = ParseSparql(expression);
                var dataset = new StoreSparqlDataset(store);
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
                    expression, ex);
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
