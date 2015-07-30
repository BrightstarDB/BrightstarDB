using System;
using System.IO;
using System.Text;
using BrightstarDB.EntityFramework.Query;
using VDS.RDF;
using VDS.RDF.Query;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Represents the results of running a SPARQL query against a Brightstar store
    /// </summary>
    public class SparqlResult : IDisposable
    {
        /// <summary>
        /// The SparqlQueryContext that generated this result
        /// </summary>
        public readonly SparqlQueryContext SourceSparqlQueryContext;

        internal SparqlResult(Stream resultStream, ISerializationFormat resultFormat, SparqlQueryContext sparqlQueryContext)
        {
            ResultStream = resultStream;
            ResultFormat = resultFormat;
            SourceSparqlQueryContext = sparqlQueryContext;
        }

        internal SparqlResult(string resultString, ISerializationFormat resultFormat, SparqlQueryContext sparqlQueryContext)
        {
            ResultStream = new MemoryStream(Encoding.UTF8.GetBytes(resultString));
            ResultFormat = resultFormat;
            SourceSparqlQueryContext = sparqlQueryContext;
        }

        /// <summary>
        /// The raw XML sparql result stream
        /// </summary>
        public Stream ResultStream { get; }

        public ISerializationFormat ResultFormat { get; }

        /// <summary>
        /// Returns true if the format specified by <see cref="ResultFormat"/> is an RDF graph format
        /// </summary>
        public bool IsGraphResult => ResultFormat is RdfFormat;

        /// <summary>
        /// Return the results processed into a <see cref="IGraph"/> for easier processing
        /// </summary>
        /// <returns>The parsed <see cref="IGraph"/></returns>
        /// <exception cref="InvalidOperationException">Raised if <see cref="ResultFormat"/> is not an <see cref="RdfFormat"/>.</exception>
        public IGraph GetResultsAsGraph()
        {
            if (!IsGraphResult) throw new InvalidOperationException("Result format is not an RDF Graph format");
            var g = new Graph();
            var reader = MimeTypesHelper.GetParser(ResultFormat.MediaTypes);
            using (var sr = new StreamReader(ResultStream))
            {
                reader.Load(g, sr);
            }
            return g;
        }

        /// <summary>
        /// Return the results processed into a <see cref="SparqlResultSet"/> for easier processing
        /// </summary>
        /// <returns>The parsed <see cref="SparqlResultSet"/></returns>
        /// <exception cref="InvalidOperationException">Raised if <see cref="ResultFormat"/> is not a <see cref="SparqlResultsFormat"/>.</exception>
        public SparqlResultSet GetResultAsSparqlResultSet()
        {
            if (IsGraphResult) throw new InvalidOperationException("Result format is not a SPARQL result format");
            var resultSet = new SparqlResultSet();
            var parser = MimeTypesHelper.GetSparqlParser(ResultFormat.MediaTypes[0]);
            parser.Load(resultSet, new StreamReader(ResultStream));
            return resultSet;
        }

        /// <summary>
        /// Disposes of the underlying resources. 
        /// </summary>
        public void Dispose()
        {
            ResultStream?.Dispose();
        }
    }
}
