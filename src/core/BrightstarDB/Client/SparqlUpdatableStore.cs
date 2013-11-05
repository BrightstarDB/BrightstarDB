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

            var deleteOp = FormatDeletePatterns(deletePatterns, updateGraphUri);
            var insertOp = FormatInserts(inserts, updateGraphUri);

            var parser = new SparqlUpdateParser();
            var cmds = parser.ParseFromString(deleteOp + "\n" + insertOp);
            _updateProcessor.ProcessCommandSet(cmds);
        }

        private string FormatDeletePatterns(IList<Triple> deletePatterns, string updateGraphUri)
        {
            var deleteCmds = new StringBuilder();
            int propId = 0;
            if (deletePatterns.Any(p => IsGraphTargeted(p) && IsGrounded(p)))
            {
                deleteCmds.AppendLine("DELETE DATA {");
                foreach (var patternGroup in deletePatterns.Where(p => IsGraphTargeted(p) && IsGrounded(p)).GroupBy(p=>p.Graph))
                {
                    deleteCmds.AppendFormat("GRAPH <{0}> {{", patternGroup.Key);
                    deleteCmds.AppendLine();
                    foreach (var deletePattern in patternGroup)
                    {
                        AppendTriplePattern(deletePattern, deleteCmds);
                    }
                    deleteCmds.AppendLine("}");
                }
                deleteCmds.AppendLine("};");
            }
            foreach (var deletePattern in deletePatterns.Where(p=>IsGraphTargeted(p) && !IsGrounded(p)))
            {
                deleteCmds.AppendFormat("WITH <{0}> DELETE {{ {1} }} WHERE {{ {1} }};",
                                        deletePattern.Graph, FormatDeletePattern(deletePattern, ref propId));
            }
            if (deletePatterns.Any(p => !IsGraphTargeted(p) && IsGrounded(p)))
            {
                // Delete from default graph
                deleteCmds.AppendLine("DELETE DATA {");
                foreach (var p in deletePatterns.Where(p => !IsGraphTargeted(p) && IsGrounded(p)))
                {
                    AppendTriplePattern(p, deleteCmds);
                }
                // If an update graph is specified delete from that too
                if (updateGraphUri != null)
                {
                    deleteCmds.AppendFormat("GRAPH <{0}> {{", updateGraphUri);
                    deleteCmds.AppendLine();
                    foreach (var p in deletePatterns.Where(p => !IsGraphTargeted(p) && IsGrounded(p)))
                    {
                        AppendTriplePattern(p, deleteCmds);
                    }
                    deleteCmds.AppendLine("}");
                }
                deleteCmds.AppendLine("};");

            }
            foreach (var deletePattern in deletePatterns.Where(p => !IsGraphTargeted(p) && !IsGrounded(p)))
            {
                var cmd = String.Format("DELETE {{ {0} }} WHERE {{ {0} }};",
                                        FormatDeletePattern(deletePattern, ref propId));
                deleteCmds.AppendLine(cmd);
                if (updateGraphUri != null)
                {
                    deleteCmds.AppendFormat("WITH <{0}> ", updateGraphUri);
                    deleteCmds.AppendLine(cmd);
                }
            }
            return deleteCmds.ToString();
        }

        private static string FormatDeletePattern(Triple p, ref int propId)
        {
            return p.Predicate.Equals(Constants.WildcardUri)
                       ? String.Format("  <{0}> ?d{1} ?d{2} .", p.Subject, propId++, propId++)
                       : String.Format("  <{0}> <{1}> ?d{2} .", p.Subject, p.Predicate, propId++);
        }

        /// <summary>
        /// Creates a single DELETE DATA command that deletes a collection of grounded triples
        /// from a collection of named graphs
        /// </summary>
        /// <param name="graphUris">The URIs of the named graphs to delete from</param>
        /// <param name="deleteData">The collection of grounded triples to be deleted</param>
        /// <param name="buff">The buffer to write the generated command into</param>
        private static void FormatGroundedDeleteForGraphs(IEnumerable<string> graphUris, string deleteData,
                                                          StringBuilder buff)
        {
            
            buff.AppendLine("DELETE DATA {");
            foreach (var g in graphUris)
            {
                buff.AppendFormat("GRAPH <{0}> {{", g);
                buff.AppendLine();
                buff.Append(deleteData);
                buff.AppendLine("}");
            }
            buff.AppendLine("}");
        }

        /// <summary>
        /// Determines if the specified triple is targeted at a specific graph
        /// </summary>
        /// <param name="t">The triple to check</param>
        /// <returns></returns>
        private static bool IsGraphTargeted(Triple t)
        {
            return t.Graph != null && t.Graph != Constants.WildcardUri;
        }

        /// <summary>
        /// Determines if the specified triple consists of a grounded (non-wildcard)
        /// subject, predicate and object.
        /// </summary>
        /// <param name="t">The triple to check</param>
        /// <returns></returns>
        private static bool IsGrounded(Triple t)
        {
            return (t.Predicate != Constants.WildcardUri) && (t.Object != Constants.WildcardUri);
        }

        //private string FormatDeletePatterns(IEnumerable<Triple> deletePatterns)
        //{
        //    int propId = 0;
        //    var deleteOp = new StringBuilder();
        //    deleteOp.AppendLine("DELETE {");
        //    foreach (var deleteGraphGroup in deletePatterns.GroupBy(d => d.Graph))
        //    {
        //        deleteOp.AppendFormat("GRAPH <{0}> {{", deleteGraphGroup.Key);
        //        deleteOp.AppendLine();
        //        foreach (var deletePattern in deleteGraphGroup)
        //        {
        //            if (deletePattern.Predicate.Equals(Constants.WildcardUri))
        //            {
        //                deleteOp.AppendFormat("  <{0}> ?d{1} ?d{2} .", deletePattern.Subject, propId++, propId++);
        //            }
        //            else if (!deletePattern.IsLiteral && deletePattern.Object.Equals(Constants.WildcardUri))
        //            {
        //                deleteOp.AppendFormat("  <{0}> <{1}> ?d{2} .", deletePattern.Subject, deletePattern.Predicate,
        //                                      propId++);
        //            }
        //            else
        //            {
        //                AppendTriplePattern(deletePattern, deleteOp);
                        
        //            }
        //            deleteOp.AppendLine();
        //        }
        //        deleteOp.AppendLine("}");
        //    }
        //    deleteOp.AppendLine("}");
        //    return deleteOp.ToString();
        //}

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
            op.AppendLine("INSERT DATA {");
            foreach (var graphGroup in inserts.GroupBy(i => i.Graph))
            {
                var targetGraph = graphGroup.Key ?? defaultGraphUri;
                if (targetGraph != null)
                {
                    op.AppendFormat("GRAPH <{0}> {{", graphGroup.Key ?? defaultGraphUri);
                    op.AppendLine();
                }
                foreach (var triple in graphGroup)
                {
                    AppendTriplePattern(triple, op);
                }
                if (targetGraph != null)
                {
                    op.AppendLine("}");
                }
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