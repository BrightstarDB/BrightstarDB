using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkedPlanet.Brightstar.Storage;
using NetworkedPlanet.Rdf;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace NetworkedPlanet.Brightstar.Tests.Sparql11TestSuite
{
    public class SparqlTest
    {
        private readonly IStoreManager _storeManager;
        private string _storeLocation;
        private IStore _store;

        public SparqlTest()
        {
            _storeManager = StoreManagerFactory.GetStoreManager();
        }

        public void CreateStore()
        {
            _storeLocation = "brightstar\\" + Guid.NewGuid();
            _store = _storeManager.CreateStore(_storeLocation);
        }

        public void DeleteStore()
        {
            _storeManager.DeleteStore(_storeLocation);
        }

        #region Support Methods

        private Dictionary<string, string> _bnodeMappings;

        protected void ImportData(string dataPath, string defaultGraphUri = null)
        {
            var g = new Graph();
            var importId = Guid.NewGuid();
            FileLoader.Load(g, dataPath);

            _bnodeMappings = new Dictionary<string, string>();

            foreach (var t in g.Triples)
            {
                if (t.Object.NodeType == NodeType.Literal)
                {
                    var litNode = t.Object as LiteralNode;
                    _store.InsertTriple(
                        GetNodeString(t.Subject, importId),
                        GetNodeString(t.Predicate, importId),
                        litNode.Value,
                        true,
                        litNode.DataType == null ? null : litNode.DataType.ToString(),
                        litNode.Language,
                        defaultGraphUri ?? Constants.DefaultGraphUri
                        );
                }
                else
                {
                    _store.InsertTriple(
                        GetNodeString(t.Subject, importId),
                        GetNodeString(t.Predicate, importId),
                        GetNodeString(t.Object, importId),
                        false,
                        null,
                        null,
                        defaultGraphUri ?? Constants.DefaultGraphUri
                        );
                }
            }
            _store.Commit(Guid.NewGuid());
        }

        private string GetNodeString(INode node, Guid importId)
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

                //return "_:bnode-" + importId + "-" + (node as BlankNode).InternalID;                
            }
            Assert.Fail("Unexpected node type in GetNodeString: {0}", node.NodeType);
            return null;
        }


        protected void ImportGraph(string dataPath, Uri graphUri)
        {
            ImportData(dataPath, graphUri.ToString());
        }

        protected string ExecuteQuery(string queryPath)
        {
            var queryExp = File.ReadAllText(queryPath);
            return _store.ExecuteSparqlQuery(queryExp);
        }

        protected void CheckResult(string results, string expectedResultPath, bool laxCardinality)
        {
            Assert.IsNotNull(results);
            var resultExtension = Path.GetExtension(expectedResultPath).ToLower();
            if (resultExtension.Equals(".srx"))
            {
                CompareSparqlResults(results, expectedResultPath, laxCardinality);
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

        protected void ValidateUnamedGraph(string dataPath)
        {
        }

        protected void ValidateGraph(string dataPath, Uri graphUri)
        {
        }

        protected void ExecuteUpdate(string requestPath)
        {
        }

        private void CompareSparqlResults(string results, string expectedResultsPath, bool reduced)
        {
            var p = new SparqlXmlParser();
            var actualResultSet = new SparqlResultSet();
            using (var tr = new StringReader(results))
            {
                p.Load(actualResultSet, tr);
            }
            var expectedResultSet = new SparqlResultSet();
            p.Load(expectedResultSet, expectedResultsPath);
            var bnodeMap = new Dictionary<string, string>();
            CompareSparqlResults(actualResultSet, expectedResultSet, reduced, bnodeMap);
        }

        private void CompareResultGraphs(string results, string expectedResultsPath, bool reduced)
        {
            var expectedResultGraph = new Graph();
            FileLoader.Load(expectedResultGraph, expectedResultsPath);
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
                rdfParser.Load(expectedResultSet, expectedResultsPath);
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
                    var uriNode = actualNode as IUriNode;
                    if (uriNode == null) return false;
                    var bnode = expectedNode as IBlankNode;
                    if (bnodeMap.ContainsKey(bnode.InternalID))
                    {
                        return uriNode.Uri.ToString().Equals(bnodeMap[bnode.InternalID]);
                    }
                    else if (uriNode.Uri.ToString().StartsWith(Constants.GeneratedUriPrefix))
                    {
                        bnodeMap[bnode.InternalID] = uriNode.Uri.ToString();
                        return true;
                    }
                    return false;
                case NodeType.Literal:
                    if (!actualNode.NodeType.Equals(expectedNode.NodeType)) return false;
                    var xl = actualNode as LiteralNode;
                    var yl = expectedNode as LiteralNode;
                    if (!xl.Value.Equals(yl.Value)) return false;
                    var xd = xl.DataType == null ? RdfDatatypes.PlainLiteral : xl.DataType.ToString();
                    var yd = yl.DataType == null ? RdfDatatypes.PlainLiteral : yl.DataType.ToString();
                    if (!xd.Equals(yd)) return false;
                    var xlang = xl.Language ?? String.Empty;
                    var ylang = yl.Language ?? String.Empty;
                    if (!xlang.Equals(ylang, StringComparison.InvariantCultureIgnoreCase)) return false;
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

        private static void CompareTripleCollections(BaseTripleCollection actualTriples, BaseTripleCollection expectedTriples, bool reduced)
        {
            var actualTripleList = new List<Triple>(actualTriples);
            var expectedTripleList = new List<Triple>(expectedTriples);
            var alreadySeen = new HashSet<Triple>();
            var bnodeMap = new Dictionary<string, string>();

            while (actualTripleList.Count > 0)
            {
                var at = actualTripleList[0];
                actualTripleList.Remove(at);
                if (alreadySeen.Contains(at)) continue;
                var match = expectedTripleList.Find(x => TripleMatch(x, at, bnodeMap));
                if (match == null)
                {
                    Assert.Fail("No match found for actual triple {0} in expected triples set", at);
                }

                expectedTripleList.Remove(match);
                alreadySeen.Add(at);
            }
            Assert.IsTrue(actualTripleList.Count == 0);
            Assert.IsTrue(expectedTripleList.Count == 0, "Left with some unmatched triples in the expected triples set: {0}",
                String.Join(",", expectedTripleList.Select(t => t.ToString())));
        }

        private static bool TripleMatch(Triple expectedTriple, Triple actualTriple, Dictionary<string, string> bnodeMap)
        {
            var subjMatch = NodeMatch(expectedTriple.Subject, actualTriple.Subject, bnodeMap);
            var predMatch = NodeMatch(expectedTriple.Predicate, actualTriple.Predicate, bnodeMap);
            var objMatch = NodeMatch(expectedTriple.Object, actualTriple.Object, bnodeMap);

            if (subjMatch && predMatch && objMatch)
            {
                if (actualTriple.Subject is IUriNode &&
                    expectedTriple.Subject is IBlankNode &&
                    !bnodeMap.ContainsKey((actualTriple.Subject as IUriNode).Uri.ToString()))
                {
                    bnodeMap[(actualTriple.Subject as IUriNode).Uri.ToString()] = (expectedTriple.Subject as IBlankNode).InternalID;
                }
                if (actualTriple.Predicate is IUriNode &&
                    expectedTriple.Predicate is IBlankNode &&
                    !bnodeMap.ContainsKey((actualTriple.Predicate as IUriNode).Uri.ToString()))
                {
                    bnodeMap[(actualTriple.Predicate as IUriNode).Uri.ToString()] = (expectedTriple.Predicate as IBlankNode).InternalID;

                    //bnodeMap[(actualTriple.Predicate as IBlankNode).InternalID] = (expectedTriple.Predicate as IBlankNode).InternalID;
                }
                if (actualTriple.Object is IUriNode &&
                    expectedTriple.Object is IBlankNode &&
                    !bnodeMap.ContainsKey((actualTriple.Object as IUriNode).Uri.ToString()))
                {
                    bnodeMap[(actualTriple.Object as IUriNode).Uri.ToString()] = (expectedTriple.Object as IBlankNode).InternalID;

                    //bnodeMap[(actualTriple.Object as IBlankNode).InternalID] = (expectedTriple.Object as IBlankNode).InternalID;
                }

                // both blanks

                if (actualTriple.Subject is IBlankNode &&
                    expectedTriple.Subject is IBlankNode &&
                    !bnodeMap.ContainsKey((actualTriple.Subject as IBlankNode).InternalID))
                {
                    bnodeMap[(actualTriple.Subject as IBlankNode).InternalID] = (expectedTriple.Subject as IBlankNode).InternalID;
                }
                if (actualTriple.Predicate is IBlankNode &&
                    expectedTriple.Predicate is IBlankNode &&
                    !bnodeMap.ContainsKey((actualTriple.Predicate as IBlankNode).InternalID))
                {
                    bnodeMap[(actualTriple.Predicate as IBlankNode).InternalID] = (expectedTriple.Predicate as IBlankNode).InternalID;

                    //bnodeMap[(actualTriple.Predicate as IBlankNode).InternalID] = (expectedTriple.Predicate as IBlankNode).InternalID;
                }
                if (actualTriple.Object is IBlankNode &&
                    expectedTriple.Object is IBlankNode &&
                    !bnodeMap.ContainsKey((actualTriple.Object as IBlankNode).InternalID))
                {
                    bnodeMap[(actualTriple.Object as IBlankNode).InternalID] = (expectedTriple.Object as IBlankNode).InternalID;

                    //bnodeMap[(actualTriple.Object as IBlankNode).InternalID] = (expectedTriple.Object as IBlankNode).InternalID;
                }
            }
            return subjMatch && objMatch && predMatch;
            /*
            return NodeMatch(expectedTriple.Subject, actualTriple.Subject, bnodeMap) &&
                   NodeMatch(expectedTriple.Predicate, actualTriple.Predicate, bnodeMap) &&
                   NodeMatch(expectedTriple.Object, actualTriple.Object, bnodeMap);
             */
        }

        private static bool NodeMatch(INode expectedNode, INode actualNode, Dictionary<string, string> bnodeMap)
        {
            if (expectedNode is IBlankNode && actualNode is IUriNode)
            {
                var actualUriNode = actualNode as IUriNode;
                var expectedBNode = expectedNode as IBlankNode;
                if (bnodeMap.ContainsKey(actualUriNode.Uri.ToString()))
                {
                    return expectedBNode.InternalID.Equals(bnodeMap[actualUriNode.Uri.ToString()]);
                }
                return true;
            }
            if (expectedNode is IBlankNode && actualNode is IBlankNode)
            {
                var ebNode = expectedNode as IBlankNode;
                var abNode = actualNode as IBlankNode;
                if (bnodeMap.ContainsKey(abNode.InternalID))
                {
                    return ebNode.InternalID.Equals(bnodeMap[abNode.InternalID]);
                }
                return true; // ebNode.InternalID.Equals(abNode.InternalID);
            }
            if (!expectedNode.NodeType.Equals(actualNode.NodeType)) return false;
            return expectedNode.Equals(actualNode);
        }

        //private static Triple MakeMatchTriple(Triple actualTriple, Dictionary<string, string> bnodeMap)
        //{
        //    return new Triple(
        //        MakeMatchNode(actualTriple.Subject, bnodeMap),
        //        MakeMatchNode(actualTriple.Predicate, bnodeMap),
        //        MakeMatchNode(actualTriple.Object, bnodeMap),
        //        actualTriple.GraphUri);
        //}

        //private static INode MakeMatchNode(INode n, Dictionary<string, string> bnodeMap)
        //{
        //    if (n is IBlankNode)
        //    {
        //        var b = n as IBlankNode;
        //        if (bnodeMap.ContainsKey(b.InternalID))
        //        {
        //            return n.Graph.CreateBlankNode(bnodeMap[b.InternalID]);
        //        }
        //    }
        //    return n;
        //}

        #endregion
    }
}
