using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Update;
using VDS.RDF.Writing;
using StringWriter = System.IO.StringWriter;
using Triple = BrightstarDB.Model.Triple;

namespace BrightstarDB.Client
{
    internal class SparqlUpdatableStore : IUpdateableStore
    {
        private readonly ISparqlQueryProcessor _queryProcessor;
        private readonly ISparqlUpdateProcessor _updateProcessor;

        public SparqlUpdatableStore(ISparqlQueryProcessor queryProcessor, ISparqlUpdateProcessor updateProcessor)
        {
            _queryProcessor = queryProcessor;
            _updateProcessor = updateProcessor;
        }

        public Stream ExecuteQuery(string queryExpression, IList<string> datasetGraphUris)
        {
            var parser = new SparqlQueryParser();
            var query = parser.ParseFromString(queryExpression);
            var sparqlResults = _queryProcessor.ProcessQuery(query);
            var memoryStream = new MemoryStream();
            using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
            {
                if (sparqlResults is SparqlResultSet)
                {
                    var resultSet = sparqlResults as SparqlResultSet;
                    var writer = new SparqlXmlWriter();
                    writer.Save(resultSet, streamWriter);
                }
                else if (sparqlResults is IGraph)
                {
                    var g = sparqlResults as IGraph;
                    var writer = new RdfXmlWriter();
                    writer.Save(g, streamWriter);
                }
            }
            return new MemoryStream(memoryStream.ToArray());
            //return new MemoryStream(Encoding.UTF8.GetBytes(buff.ToString()), false);
        }

        public void ApplyTransaction(IList<Triple> preconditions, IList<Triple> deletePatterns, IList<Triple> inserts,
                                     string updateGraphUri)
        {
            if (preconditions.Count > 0)
            {
                throw new NotSupportedException("SparqlDataObjectStore does not support conditional updates");
            }

            var deleteOp = FormatDeletePatterns(deletePatterns);
            var insertOp = FormatInserts(inserts, updateGraphUri);

            var parser = new SparqlUpdateParser();
            var cmds = parser.ParseFromString(deleteOp + "\n" + insertOp);
            _updateProcessor.ProcessCommandSet(cmds);
        }

        private string FormatDeletePatterns(IEnumerable<Triple> deletePatterns)
        {
            int propId = 0;
            var deleteOp = new StringBuilder();
            deleteOp.AppendLine("DELETE {");
            foreach (var deleteGraphGroup in deletePatterns.GroupBy(d => d.Graph))
            {
                deleteOp.AppendFormat("GRAPH <{0}> {{", deleteGraphGroup.Key);
                deleteOp.AppendLine();
                foreach (var deletePattern in deleteGraphGroup)
                {
                    if (deletePattern.Predicate.Equals(Constants.WildcardUri))
                    {
                        deleteOp.AppendFormat("  <{0}> ?d{1} ?d{2} .", deletePattern.Subject, propId++, propId++);
                    }
                    else if (!deletePattern.IsLiteral && deletePattern.Object.Equals(Constants.WildcardUri))
                    {
                        deleteOp.AppendFormat("  <{0}> <{1}> ?d{2} .", deletePattern.Subject, deletePattern.Predicate,
                                              propId++);
                    }
                    else
                    {
                        AppendTriplePattern(deletePattern, deleteOp);
                        
                    }
                    deleteOp.AppendLine();
                }
                deleteOp.AppendLine("}");
            }
            deleteOp.AppendLine("}");
            return deleteOp.ToString();
        }

        private void AppendTriplePattern(Triple triple, StringBuilder builder)
        {
            builder.AppendFormat("  <{0}> <{1}> ", triple.Subject, triple.Predicate);
            if (triple.IsLiteral)
            {
                builder.AppendFormat("\"{0}\"", triple.Object);
                if (triple.DataType != null)
                {
                    builder.Append("^^");
                    builder.AppendFormat("<{0}>", triple.DataType);
                }
                if (triple.LangCode != null)
                {
                    builder.Append("@");
                    builder.Append(triple.LangCode);
                }
            }
            else
            {
                builder.AppendFormat("<{0}>", triple.Object);
            }
            builder.Append(" .");
        }


        private string FormatInserts(IEnumerable<Triple> inserts, string defaultGraphUri)
        {
            var op = new StringBuilder();
            op.AppendLine("INSERT {");
            foreach (var graphGroup in inserts.GroupBy(i => i.Graph))
            {
                op.AppendFormat("GRAPH <{0}> {{", graphGroup.Key ?? defaultGraphUri);
                op.AppendLine();
                foreach (var triple in graphGroup)
                {
                    AppendTriplePattern(triple, op);
                }
                op.AppendLine("}");
            }
            op.AppendLine("}");
            return op.ToString();
        }

        public void Cleanup()
        {
            // Nothing to do
        }
    }
}