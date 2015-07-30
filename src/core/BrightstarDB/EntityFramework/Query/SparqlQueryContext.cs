using System.Collections.Generic;
using Remotion.Linq.Clauses;

namespace BrightstarDB.EntityFramework.Query
{
    /// <summary>
    /// Manages the context information required during the processing of an entity framework query into SPARQL
    /// </summary>
    public class SparqlQueryContext
    {
        /// <summary>
        /// Get the SPARQL query that is generated from the LINQ
        /// </summary>
        public string SparqlQuery { get; private set; }
        /// <summary>
        /// Gets the list of tuples that map anonymous type members in the LINQ query result to SPARQL variables in the SPARQL query result
        /// </summary>
        public List<System.Tuple<string, string>> AnonymousMembersMap { get; private set; }

        /// <summary>
        /// Get the list of ordering directions for the sort expressions in the SPARQL query.
        /// </summary>
        public IList<OrderingDirection> OrderingDirections { get; private set; } 

        /// <summary>
        /// If true the result is expected to have triples and their subjects grouped.For each subject group a new entity is created.
        /// </summary>
        public bool ExpectTriplesWithOrderedSubjects { get; set; }

        /// <summary>
        /// Get or set the serialization to use for SPARQL queries that return a SPARQL results table or a single boolean value
        /// </summary>
        public SparqlResultsFormat SparqlResultsFormat { get; set; }

        /// <summary>
        /// Get or set the serialization to use for SPARQL queries that return an RDF graph
        /// </summary>
        public RdfFormat GraphResultsFormat { get; set; }

        internal SparqlQueryContext()
        {
            AnonymousMembersMap = new List<System.Tuple<string, string>>();
            OrderingDirections = new List<OrderingDirection>(0);
            SparqlResultsFormat = SparqlResultsFormat.Xml;
            GraphResultsFormat = RdfFormat.RdfXml;
        }

        /// <summary>
        /// Creates a new SparqlQueryContext instance
        /// </summary>
        /// <param name="sparqlQuery">sparql query</param>
        public SparqlQueryContext(string sparqlQuery) : 
            this(sparqlQuery, new System.Tuple<string, string>[0], new OrderingDirection[0], SparqlResultsFormat.Xml, RdfFormat.RdfXml)
        {
        }

        public SparqlQueryContext(string sparqlQuery, SparqlResultsFormat tableResultsFormat, RdfFormat graphResultsFormat) : 
            this(sparqlQuery, new System.Tuple<string, string>[0], new OrderingDirection[0], tableResultsFormat, graphResultsFormat) { }

        /// <summary>
        /// Creates a new SparqlQueryContext instance
        /// </summary>
        /// <param name="sparqlQuery">sparql query</param>
        /// <param name="anonymousMembersMap">mappings for anonymous types</param>
        public SparqlQueryContext(string sparqlQuery, IEnumerable<System.Tuple<string, string>> anonymousMembersMap) :
            this(sparqlQuery, anonymousMembersMap, new OrderingDirection[0], SparqlResultsFormat.Xml, RdfFormat.RdfXml)
        {
        }

        /// <summary>
        /// Creates a new SparqlQueryContext instance
        /// </summary>
        /// <param name="sparqlQuery">sparql query</param>
        /// <param name="anonymousMembersMap">mappings for anonymous types</param>
        /// <param name="orderingDirections">ordering direction for the sort expressions in the SPARQL query</param>
        public SparqlQueryContext(string sparqlQuery, IEnumerable<System.Tuple<string, string>> anonymousMembersMap, IEnumerable<OrderingDirection> orderingDirections):
            this(sparqlQuery, anonymousMembersMap, orderingDirections, SparqlResultsFormat.Xml, RdfFormat.RdfXml)
        {
        }

        public SparqlQueryContext(string sparqlQuery, IEnumerable<System.Tuple<string, string>> anonymousMembersMap,
            IEnumerable<OrderingDirection> orderingDirections,
            SparqlResultsFormat tableResultsFormat, RdfFormat graphResultsFormat)
        {
            SparqlQuery = sparqlQuery;
            AnonymousMembersMap = new List<System.Tuple<string, string>>(anonymousMembersMap);
            OrderingDirections = new List<OrderingDirection>(orderingDirections);
            SparqlResultsFormat = tableResultsFormat;
            GraphResultsFormat = graphResultsFormat;
        }
    }
}
