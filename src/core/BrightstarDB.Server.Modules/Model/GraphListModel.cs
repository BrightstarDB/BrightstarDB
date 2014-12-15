using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;

namespace BrightstarDB.Server.Modules.Model
{
    public class GraphListModel
    {
        public const string SparqlResultVariableName = "graphUri";

        public List<string> Graphs { get; private set; } 

        public GraphListModel(IEnumerable<string> graphList)
        {
            Graphs= new List<string>(graphList);
        }

        public string AsString(SparqlResultsFormat format)
        {
            var g = new VDS.RDF.Graph();
            var results = new List<SparqlResult>();
            foreach (var graphUri in Graphs)
            {
                var s = new Set();
                s.Add(SparqlResultVariableName, g.CreateUriNode(new Uri(graphUri)));
                results.Add(new SparqlResult(s));
            }
            var rs = new SparqlResultSet(results);
            var writer = GetWriter(format);
            var sw = new StringWriter();
            writer.Save(rs, sw);
            sw.Flush();
            return sw.ToString();
        }

        private static ISparqlResultsWriter GetWriter(SparqlResultsFormat format)
        {
            if (format == SparqlResultsFormat.Csv)
            {
                return new SparqlCsvWriter();
            }
            if (format == SparqlResultsFormat.Tsv)
            {
                return new SparqlTsvWriter();
            }
            if (format == SparqlResultsFormat.Json)
            {
                return new SparqlJsonWriter();
            }
            return new SparqlXmlWriter();
        }
    }
}
