using VDS.RDF;
using VDS.RDF.Query;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Represents the results of running a SPARQL query against a Brightstar store
    /// </summary>
    public interface ISparqlResult
    {
        /// <summary>
        /// Get the results of a SPARQL CONSTRUCT or DESCRIBE query as an RDF graph.
        /// </summary>
        /// <remarks>This property will be NULL if the query was an ASK or SELECT query</remarks>
        IGraph ResultGraph { get; }

        /// <summary>
        /// Get the results of a SPARQL ASK or SELECT query
        /// </summary>
        /// <remarks>This property will be NULL if the query was a CONSTRUCT or DESCRIBE query.</remarks>
        SparqlResultSet ResultSet { get; }

        /// <summary>
        /// Returns true if the result is graph, false otherwise
        /// </summary>
        bool IsGraphResult { get; }
    }
    
}