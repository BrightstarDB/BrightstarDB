using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace BrightstarDB.Utils
{
    /// <summary>
    /// Provides helper method for processing SPARQL query and update requests
    /// </summary>
    public class SparqlQueryHelper
    {
        /// <summary>
        /// Determines the type of model that will be returned by the specified SPARQL query
        /// </summary>
        /// <param name="sparqlQuery"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">raised if <paramref name="sparqlQuery"/> is NULL</exception>
        /// <exception cref="ArgumentException">rasised if <paramref name="sparqlQuery"/> is an empty string</exception>
        /// <exception cref="VDS.RDF.Parsing.RdfParseException">raised if the sparql query could not be parsed</exception>
        public static SerializableModel GetResultModel(string sparqlQuery)
        {
            if (sparqlQuery == null) throw new ArgumentNullException("sparqlQuery", Strings.BrightstarServiceClient_QueryMustNotBeNull);
            if (String.IsNullOrEmpty(sparqlQuery)) throw new ArgumentException(Strings.BrightstarServiceClient_QueryMustNotBeEmptyString, "sparqlQuery");
            var p = new SparqlQueryParser();
            var q = p.ParseFromString(sparqlQuery);
            switch (q.QueryType)
            {
                case SparqlQueryType.Construct:
                case SparqlQueryType.Describe:
                case SparqlQueryType.DescribeAll:
                    return SerializableModel.RdfGraph;
                default:
                    return SerializableModel.SparqlResultSet;
            }
        }
    }
}
