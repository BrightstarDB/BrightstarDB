using System;
using System.IO;
using System.Text;
using BrightstarDB.EntityFramework.Query;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Represents the results of running a SPARQL query against a Brightstar store
    /// </summary>
    public class SparqlResult : IDisposable, ISparqlResult
    {
        /// <summary>
        /// Get the results of a SPARQL CONSTRUCT or DESCRIBE query as an RDF graph.
        /// </summary>
        /// <remarks>This property will be NULL if the query was an ASK or SELECT query</remarks>
        public IGraph ResultGraph { get; private set; }

        /// <summary>
        /// Get the results of a SPARQL ASK or SELECT query
        /// </summary>
        /// <remarks>This property will be NULL if the query was a CONSTRUCT or DESCRIBE query.</remarks>
        public SparqlResultSet ResultSet { get; private set; }

        /// <summary>
        /// The SparqlQueryContext that generated this result
        /// </summary>
        public readonly SparqlQueryContext SourceSparqlQueryContext;

        internal SparqlResult(TextReader streamReader, ISerializationFormat resultFormat,
            SparqlQueryContext sparqlQueryContext)
        {
            ResultFormat = resultFormat;
            ParseResult(streamReader);
            SourceSparqlQueryContext = sparqlQueryContext;
        }

        internal SparqlResult(Stream resultStream, ISerializationFormat resultFormat, SparqlQueryContext sparqlQueryContext)
        {
            if (resultStream == null) throw new ArgumentNullException(nameof(resultStream));
            ResultFormat = resultFormat;
            SourceSparqlQueryContext = sparqlQueryContext;
            using (var reader = new StreamReader(resultStream))
            {
                ParseResult(reader);
            }
        }

        internal SparqlResult(string resultString, ISerializationFormat resultFormat, SparqlQueryContext sparqlQueryContext) :
            this(new StringReader(resultString), resultFormat, sparqlQueryContext )
        {
            
        }


        internal SparqlResult(object resultObject, SparqlQueryContext sparqlQueryContext)
        {
            if (resultObject == null) throw new ArgumentNullException(nameof(resultObject));
            if (sparqlQueryContext == null) throw new ArgumentNullException(nameof(sparqlQueryContext));
            SourceSparqlQueryContext = sparqlQueryContext;

            ResultGraph = resultObject as IGraph;
            if (ResultGraph == null)
            {
                ResultSet = resultObject as SparqlResultSet;
                if (ResultSet == null)
                {
                    throw new ArgumentException(
                        $"Result object must be either a {typeof (IGraph).FullName} or a {typeof (SparqlResultSet).FullName} instance. Got a {resultObject.GetType().FullName}");
                }
                ResultFormat = sparqlQueryContext.SparqlResultsFormat ?? SparqlResultsFormat.Xml;
            }

            
            if (resultObject is IGraph)
            {
                ResultGraph = resultObject as IGraph;
                ResultFormat = sparqlQueryContext.GraphResultsFormat ?? RdfFormat.RdfXml;
            }
        }

        private void ParseResult(TextReader reader)
        {
            if (ResultFormat is RdfFormat)
            {
                ResultGraph = new Graph();
                var parser = MimeTypesHelper.GetParser(ResultFormat.MediaTypes);
                parser.Load(ResultGraph, reader);
            }
            else
            {
                ResultSet = new SparqlResultSet();
                var parser = MimeTypesHelper.GetSparqlParser(ResultFormat.MediaTypes[0]);
                parser.Load(ResultSet, reader);
            }
        }

        /// <summary>
        /// Get the serialization format that the results were provided in
        /// </summary>
        public ISerializationFormat ResultFormat { get; }

        /// <summary>
        /// Returns true if the format specified by <see cref="ResultFormat"/> is an RDF graph format
        /// </summary>
        public bool IsGraphResult => ResultFormat is RdfFormat;

        /// <summary>
        /// Disposes of the underlying resources. 
        /// </summary>
        public void Dispose()
        {
            if (ResultGraph != null)
            {
                ResultGraph.Dispose();
                ResultGraph = null;
            }
            if (ResultSet != null)
            {
                ResultSet.Dispose();
                ResultSet = null;
            }
        }
    }
}
