using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using BrightstarDB.Client;
using BrightstarDB.Rdf;
using BrightstarDB.Server;
using NUnit.Framework;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using SparqlResult = VDS.RDF.Query.SparqlResult;
#if PORTABLE
using Path = BrightstarDB.Portable.Compatibility.Path;

#endif

namespace BrightstarDB.Tests.SparqlDawgTests
{
    public class SparqlTest
    {
        private IBrightstarService _service;
        private string _storeName;

        public SparqlTest()
        {
            _service = BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar");
        }

        public void CreateStore()
        {
            _storeName = Guid.NewGuid().ToString();
            _service.CreateStore(_storeName);
        }

        public void DeleteStore()
        {
            _service.DeleteStore(_storeName);
            //_storeManager.DeleteStore(_storeLocation);
        }

        #region Support Methods

        private Dictionary<string, string> _bnodeMappings;

        protected void ImportData(string dataPath, string defaultGraphUri = null)
        {
            var g = new Graph();
            var importId = Guid.NewGuid();
#if PORTABLE
            using (var s = File.OpenRead(dataPath))
            {
                StreamLoader.Load(g, dataPath, s);
            }
#else
            FileLoader.Load(g, dataPath);
#endif
            _bnodeMappings = new Dictionary<string, string>();
            var sw = new StringWriter();
            var ntWriter = new NQuadsWriter(sw);
            foreach (var t in g.Triples)
            {
                if (t.Object.NodeType == NodeType.Literal)
                {
                    var litNode = t.Object as LiteralNode;
                    ntWriter.Triple(
                        GetNodeString(t.Subject),
                        false,
                        GetNodeString(t.Predicate),
                        false,
                        litNode.Value,
                        false,
                        true,
                        litNode.DataType == null ? null : litNode.DataType.ToString(),
                        litNode.Language,
                        defaultGraphUri ?? Constants.DefaultGraphUri
                        );
                }
                else
                {
                    ntWriter.Triple(
                        GetNodeString(t.Subject),
                        false,
                        GetNodeString(t.Predicate),
                        false,
                        GetNodeString(t.Object),
                        false,
                        false,
                        null,
                        null,
                        defaultGraphUri ?? Constants.DefaultGraphUri
                        );
                }
            }
            _service.ExecuteTransaction(_storeName, new UpdateTransactionData{InsertData= sw.ToString()});
            //_store.Commit(Guid.NewGuid());
        }

        private string GetNodeString(INode node)
        {
            if (node.NodeType == NodeType.Uri)
            {
                return (node as UriNode).Uri.ToString();
            }
            if (node.NodeType == NodeType.Blank)
            {
                var bnode = node as BlankNode;
                if (_bnodeMappings.ContainsKey(bnode.InternalID))
                {
                    return _bnodeMappings[bnode.InternalID];
                }
                else
                {
                    var skolemizedUri = Constants.GeneratedUriPrefix + Guid.NewGuid();
                    _bnodeMappings[bnode.InternalID] = skolemizedUri;
                    return skolemizedUri;
                }
            }
            Assert.Fail("Unexpected node type in GetNodeString: {0}", node.NodeType);
            return null;
        }


        protected void ImportGraph(string dataPath, Uri graphUri)
        {
            ImportData(dataPath, graphUri.ToString());
        }

        protected string ExecuteQuery(string queryPath, SparqlResultsFormat resultsFormat)
        {
            var queryExp = File.ReadAllText(queryPath);
            var resultsStream = _service.ExecuteQuery(_storeName, queryExp, resultsFormat: resultsFormat);
            using(var streamReader = new StreamReader(resultsStream))
            {
                return streamReader.ReadToEnd();
            }
        }

        protected void CheckResult(string results, string expectedResultPath, bool laxCardinality)
        {
            try
            {
                Assert.IsNotNull(results);
                var resultExtension = Path.GetExtension(expectedResultPath).ToLower();
                if (resultExtension.Equals(".srx"))
                {
                    CompareSparqlResults(results, expectedResultPath, laxCardinality);
                }
                else if (resultExtension.Equals(".srj"))
                {
                    CompareSparqlResults(results, expectedResultPath, laxCardinality, new SparqlJsonParser());
                }
                else if (resultExtension.Equals(".tsv"))
                {
                    CompareSparqlResults(results, expectedResultPath, laxCardinality, new SparqlTsvParser());
                }
                else if (resultExtension.Equals(".csv"))
                {
                    CompareSparqlResults(results, expectedResultPath, laxCardinality, new SparqlCsvParser());
                }
                else if (resultExtension.Equals(".ttl") || resultExtension.Equals(".rdf"))
                {
                    CompareResultGraphs(results, expectedResultPath, laxCardinality);
                }

                else
                {
                    Assert.Fail("Don't know how to compare results to results file {0}", expectedResultPath);
                }
            }
            catch (AssertionException ex)
            {
                throw new AssertionException(ex.Message + String.Format("Expected Results Path: {0}\nActual Results: {1}", expectedResultPath, results));
            }
        }

        protected void ValidateUnamedGraph(string dataPath)
        {
            var exportFileName = "export_default_" + _storeName;
            var exportJob = _service.StartExport(_storeName, exportFileName, Constants.DefaultGraphUri);
            while(!exportJob.JobCompletedOk && !exportJob.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                exportJob = _service.GetJobInfo(_storeName, exportJob.JobId);
            }
            Assert.IsTrue(exportJob.JobCompletedOk, "Export failed when attempting to validate unamed graph.");
            var results = File.ReadAllText("c:\\brightstar\\import\\" + exportFileName);
            CompareResultGraphs(results, dataPath, false);
        }

        protected void ValidateGraph(string dataPath, Uri graphUri)
        {
            var exportFileName = "export_" +graphUri.Segments[graphUri.Segments.Length-1] + _storeName;
            var exportJob = _service.StartExport(_storeName, exportFileName, graphUri.ToString());
            while (!exportJob.JobCompletedOk && !exportJob.JobCompletedWithErrors)
            {
                Thread.Sleep(50);
                exportJob = _service.GetJobInfo(_storeName, exportJob.JobId);
            }
            Assert.IsTrue(exportJob.JobCompletedOk, "Export failed when attempting to validate named graph {0}.", graphUri);
            var results = File.ReadAllText("c:\\brightstar\\import\\" + exportFileName);
            CompareResultGraphs(results, dataPath, false);
        }

        protected void ExecuteUpdate(string requestPath)
        {
            var updateExpression = File.ReadAllText(requestPath);
#if PORTABLE
            var result = _service.ExecuteUpdate(_storeName, updateExpression);
#else
            var result = _service.ExecuteUpdate(_storeName, updateExpression, true);
#endif
            Assert.IsTrue(result.JobCompletedOk, "SPARQL Update failed for update from file path {0} : {1} : {2}", requestPath, result.StatusMessage, result.ExceptionInfo);
        }

        private void CompareSparqlResults(string results, string expectedResultsPath, bool reduced, ISparqlResultsReader resultsReader = null)
        {
            if (resultsReader == null)
            {
                resultsReader = new SparqlXmlParser();
            }
            var actualResultSet = new SparqlResultSet();
            using (var tr = new StringReader(results))
            {
                resultsReader.Load(actualResultSet, tr);
            }
            var expectedResultSet = new SparqlResultSet();
            resultsReader.Load(expectedResultSet, new StreamReader(File.OpenRead(expectedResultsPath)));
            var bnodeMap = new Dictionary<string, string>();
            CompareSparqlResults(actualResultSet, expectedResultSet, reduced, bnodeMap);
        }

        private void CompareResultGraphs(string results, string expectedResultsPath, bool reduced)
        {
            var expectedResultGraph = new Graph();
#if PORTABLE
            using (var s = File.OpenRead(expectedResultsPath))
            {
                StreamLoader.Load(expectedResultGraph, expectedResultsPath, s);
            }
#else
            FileLoader.Load(expectedResultGraph, expectedResultsPath);
#endif
            var resultSet = expectedResultGraph.GetUriNode(new Uri("http://www.w3.org/2001/sw/DataAccess/tests/result-set#ResultSet"));
            if (resultSet != null)
            {
                var rdfParser = new SparqlRdfParser();
                var xmlParser = new SparqlXmlParser();
                var actualResultSet = new SparqlResultSet();
                var expectedResultSet = new SparqlResultSet();
                using (var tr = new StringReader(results))
                {
                    xmlParser.Load(actualResultSet, tr);
                }
#if PORTABLE
                rdfParser.Load(expectedResultSet, new StreamReader(expectedResultsPath));
#else
                rdfParser.Load(expectedResultSet, expectedResultsPath);
#endif
                var bnodeMap = new Dictionary<string, string>();
                CompareSparqlResults(actualResultSet, expectedResultSet, reduced, bnodeMap);
            }
            else
            {
                // This is a constructed graph
                var actualGraph = new Graph();
                actualGraph.LoadFromString(results);
                CompareTripleCollections(actualGraph.Triples, expectedResultGraph.Triples, reduced);
            }
        }

        private void CompareSparqlResults(SparqlResultSet actual, SparqlResultSet expected, bool reduced, Dictionary<string, string> bnodeMap)
        {
            if (expected.ResultsType == SparqlResultsType.Boolean)
            {
                Assert.IsTrue(actual.ResultsType == SparqlResultsType.Boolean);
                Assert.AreEqual(expected.Result, actual.Result);
            }
            else if (expected.ResultsType == SparqlResultsType.VariableBindings)
            {
                if (reduced)
                {
                    Assert.IsTrue(actual.Results.Count <= expected.Results.Count,
                                  "Too many results returned expected <= {0}, got {1}",
                                  expected.Results.Count, actual.Results.Count);
                }
                else
                {
                    Assert.AreEqual(expected.Results.Count, actual.Results.Count,
                                    "Unexpected number of rows in results.");
                }
                foreach (var actualSolution in actual.Results)
                {
                    Assert.IsTrue(expected.Results.Any(x => CompareSolutions(actualSolution, x, bnodeMap)),
                                  "Could not find a match for solution {0} in the expected results set", actualSolution);
                }
            }
            else
            {
                Assert.Fail("Cannot compare results to result set of type {0}", expected.ResultsType);
            }
        }

        private static bool CompareSolutions(SparqlResult x, SparqlResult y, Dictionary<string, string> bnodeMap)
        {
            var boundVarsX = x.Variables.Where(v => x.HasValue(v) && x.Value(v) != null);
            var boundVarsY = y.Variables.Where(v => y.HasValue(v) && y.Value(v) != null);
            if (!boundVarsX.Any() && !boundVarsY.Any()) return true;
            if (x.Variables.Count().Equals(y.Variables.Count()) &&
                x.Variables.All(xv => y.Variables.Contains(xv)))
            {
                foreach (var xv in x.Variables)
                {
                    var xb = x[xv];
                    var yb = y[xv];
                    if (!CompareNodes(xb, yb, bnodeMap)) return false;
                }
                return true;
            }
            return false;
        }

        private static bool CompareNodes(INode actualNode, INode expectedNode, Dictionary<string, string> bnodeMap)
        {
            // If either one is null, they must both be null
            if (actualNode == null || expectedNode == null) return actualNode == null && expectedNode == null;

            // if (!actualNode.NodeType.Equals(expectedNode.NodeType)) return false;
            switch (expectedNode.NodeType)
            {
                case NodeType.Blank:
                    if (actualNode is IBlankNode)
                    {
                        var expectedBNode = expectedNode as IBlankNode;
                        var actualBNode = actualNode as IBlankNode;
                        if (bnodeMap.ContainsKey(expectedBNode.InternalID))
                        {
                            return actualBNode.InternalID.Equals(bnodeMap[expectedBNode.InternalID]);
                        }
                        bnodeMap[expectedBNode.InternalID] = actualBNode.InternalID;
                        return true;
                    }
                    if (actualNode is IUriNode)
                    {
                        var uriNode = actualNode as IUriNode;
                        var bnode = expectedNode as IBlankNode;
                        if (bnodeMap.ContainsKey(bnode.InternalID))
                        {
                            return uriNode.Uri.ToString().Equals(bnodeMap[bnode.InternalID]);
                        }
                        if (uriNode.Uri.ToString().StartsWith(Constants.GeneratedUriPrefix))
                        {
                            bnodeMap[bnode.InternalID] = uriNode.Uri.ToString();
                            return true;
                        }
                    }
                    return false;
                case NodeType.Literal:
                    if (!actualNode.NodeType.Equals(expectedNode.NodeType)) return false;
                    var xl = actualNode as LiteralNode;
                    var yl = expectedNode as LiteralNode;
                    var xd = xl.DataType == null ? RdfDatatypes.PlainLiteral : xl.DataType.ToString();
                    var yd = yl.DataType == null ? RdfDatatypes.PlainLiteral : yl.DataType.ToString();
                    if (!xd.Equals(yd)) return false;
                    if (!CompareValues(yl.Value, xl.Value, yl.DataType == null ? RdfDatatypes.PlainLiteral: yl.DataType.ToString())) return false;
                    var xlang = xl.Language ?? String.Empty;
                    var ylang = yl.Language ?? String.Empty;
#if NETCOREAPP10
                    if (!xlang.Equals(ylang, StringComparison.OrdinalIgnoreCase)) return false;
#else
                    if (!xlang.Equals(ylang, StringComparison.InvariantCultureIgnoreCase)) return false;
#endif
                    break;
                case NodeType.Uri:
                    if (!actualNode.NodeType.Equals(expectedNode.NodeType)) return false;
                    var xu = actualNode as UriNode;
                    var yu = expectedNode as UriNode;
                    if (!xu.Uri.Equals(yu.Uri)) return false;
                    break;
            }
            return true;
        }

        private static bool CompareValues(string expected, string actual, string dataType)
        {
            if (RdfDatatypes.Double.Equals(dataType))
            {
                var dexp = double.Parse(expected);
                var dact = double.Parse(actual);
                return dexp == dact;
            }
            if (RdfDatatypes.Decimal.Equals(dataType))
            {
                var dexp = decimal.Parse(expected);
                var dact = decimal.Parse(actual);
                return dexp == dact;
            }
            return expected.Equals(actual);
        }

        private static void CompareTripleCollections(BaseTripleCollection actualTriples, BaseTripleCollection expectedTriples, bool reduced)
        {
            var actualGraph = new Graph(actualTriples);
            var expectedGraph = new Graph(expectedTriples);
            Assert.IsTrue(expectedGraph.Equals(actualGraph), "Result graphs do not match.r\nExpected:\r\n{0}\r\nActual:\r\n{1}",
                String.Join(",", expectedTriples.Select(t => t.ToString())),
                String.Join(",", actualTriples.Select(t => t.ToString())));
        }

#endregion
    }
}
