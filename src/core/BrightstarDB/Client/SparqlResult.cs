using System;
using System.IO;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.EntityFramework.Query;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Represents the results of running a SPARQL query against a Brightstar store
    /// </summary>
    public class SparqlResult : IDisposable
    {
        private Stream _resultStream;
        private readonly string _resultString;

        /// <summary>
        /// The SparqlQueryContext that generated this result
        /// </summary>
        public readonly SparqlQueryContext SourceSparqlQueryContext;

        internal SparqlResult(Stream resultStream, SparqlQueryContext sparqlQueryContext)
        {
            _resultStream = resultStream;
            SourceSparqlQueryContext = sparqlQueryContext;
        }

        internal SparqlResult(string xml, SparqlQueryContext sparqlQueryContext)
        {
            _resultString = xml;
            SourceSparqlQueryContext = sparqlQueryContext;
        }

        /// <summary>
        /// The raw XML sparql result stream
        /// </summary>
        public Stream ResultStream
        {
            get
            {
                if (_resultStream != null)
                {
                    return _resultStream;
                }
                _resultStream = new MemoryStream(Encoding.UTF8.GetBytes(_resultString));
                return _resultStream;
            }
        }

        /// <summary>
        /// The sparql result as an XDocument
        /// </summary>
        public XDocument ResultDocument
        {
            get
            {
                if (_resultString != null)
                {
                    return XDocument.Parse(_resultString);
                }
                return XDocument.Load(_resultStream);
            }
        }

        /// <summary>
        /// Disposes of the underlying resources. 
        /// </summary>
        public void Dispose()
        {
            if (_resultStream != null)
            {
                _resultStream.Dispose();
            }
        }
    }
}
