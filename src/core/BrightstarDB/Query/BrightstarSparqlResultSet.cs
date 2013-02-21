using System;
using BrightstarDB.Client;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace BrightstarDB.Query
{
    internal class BrightstarSparqlResultSet
    {
        private readonly SparqlResultSet _resultSet;
        private readonly IGraph _graph;

        /// <summary>
        /// Gets an enumeration indicating the type of result found
        /// </summary>
        public BrightstarSparqlResultsType ResultType { get; private set; }

        public SparqlResultSet RawResultSet { get { return _resultSet; } }

        public BrightstarSparqlResultSet(object o)
        {
            if (o is SparqlResultSet)
            {
                _resultSet = o as SparqlResultSet;
                ResultType = _resultSet.ResultsType == SparqlResultsType.VariableBindings
                                 ? BrightstarSparqlResultsType.VariableBindings
                                 : BrightstarSparqlResultsType.Boolean;
            }
            else if (o is IGraph)
            {
                _graph = o as IGraph;
                ResultType = BrightstarSparqlResultsType.Graph;
            }
        }

        public long Count
        {
            get
            {
                if (_resultSet != null)
                {
                    return _resultSet.Count;
                }
                if (_graph != null)
                {
                    return _graph.Triples.Count;
                }
                return 0;
            }
        }
        public override string ToString()
        {
            return GetString(SparqlResultsFormat.Xml);
        }

        public string GetString(SparqlResultsFormat format, IRdfWriter graphWriter = null)
        {
            switch (ResultType)
            {
                case BrightstarSparqlResultsType.VariableBindings:
                case BrightstarSparqlResultsType.Boolean:
                    var stringWriter = new System.IO.StringWriter();
                    var sparqlXmlWriter = GetSparqlWriter(format); 
                    sparqlXmlWriter.Save(_resultSet, stringWriter);
                    return stringWriter.GetStringBuilder().ToString();
                case BrightstarSparqlResultsType.Graph:
                    if (graphWriter == null)
                    {
#if WINDOWS_PHONE
                        // Cannot use DTD because the mobile version of XmlWriter doesn't support writing a DOCTYPE.
                        graphWriter = new RdfXmlWriter(WriterCompressionLevel.High, false);
#else
                        graphWriter = new RdfXmlWriter();
#endif
                    }
                    return StringWriter.Write(_graph, graphWriter);
                default:
                    throw new BrightstarInternalException(
                        String.Format("Unrecognized result type when serializing results string: {0}",
                                      ResultType));
            }
        }

        private ISparqlResultsWriter GetSparqlWriter(SparqlResultsFormat format)
        {
            var ext = format.DefaultExtension;
            if (ext.Equals(SparqlResultsFormat.Xml.DefaultExtension))
                    return new SparqlXmlWriter();
            if (ext.Equals(SparqlResultsFormat.Json.DefaultExtension))
                    return new SparqlJsonWriter();
            if (ext.Equals(SparqlResultsFormat.Tsv.DefaultExtension))
                    return new SparqlTsvWriter();
            if (ext.Equals(SparqlResultsFormat.Csv.DefaultExtension))
                return new SparqlCsvWriter();
            throw new BrightstarInternalException("Unsupported SPARQL results format");
        }
    }
}
