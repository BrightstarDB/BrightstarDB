using System.IO;
using System.Linq;
using System.Text;
using BrightstarDB.Query;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace BrightstarDB.InternalTests
{
    internal static class StoreExtensions
    {
        public static string ExecuteSparqlQuery(this IStore store, string sparqlExpression,
                                                SparqlResultsFormat resultsFormat)
        {
            var query = ParseSparql(sparqlExpression);
            var resultsStream = new MemoryStream();
            store.ExecuteSparqlQuery(query, resultsFormat.WithEncoding(new UTF8Encoding(false)), resultsStream);
            var ret = Encoding.UTF8.GetString(resultsStream.ToArray());
            return ret;
        }

        public static string Query(this StoreWorker storeWorker, string sparqlExpression,
                                   SparqlResultsFormat resultsFormat, string[] defaultGraphUris)
        {
            var query = ParseSparql(sparqlExpression);
            using (var resultsStream = new MemoryStream())
            {
                storeWorker.Query(query, resultsFormat.WithEncoding(new UTF8Encoding(false)), resultsStream, defaultGraphUris);
                return Encoding.UTF8.GetString(resultsStream.ToArray());
            }
        }

        public static SparqlQuery ParseSparql(string exp)
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