using System.Collections.Generic;
using System.IO;
using BrightstarDB.Rdf;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BTreeStore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using System.Linq;

namespace BrightstarDB.Tests.SparqlTestSuite {
    [TestClass]
	public partial class ManifestSyntax {

		private IStoreManager _storeManager;
        private string _storeLocation;
        private IStore _store;

        public ManifestSyntax()
        {
            _storeManager = StoreManagerFactory.GetStoreManager();
        }

		[TestInitialize]
		public void SetUp()
		{
		    _storeLocation = "brightstar\\" + Guid.NewGuid();
		    _store = _storeManager.CreateStore(_storeLocation);
		}

        [TestCleanup]
        public void TearDown()
        {
            _storeManager.DeleteStore(_storeLocation);
        }

		#region Test Methods

		#endregion

		#region Support Methods
		
		private void ImportData(string dataPath, string defaultGraphUri = null)
        {
            var g = new Graph();
            FileLoader.Load(g, dataPath);
            foreach (var t in g.Triples)
            {
                if (t.Object.NodeType == NodeType.Literal)
                {
                    var litNode = t.Object as LiteralNode;
                    _store.InsertTriple(
                        GetNodeString(t.Subject),
                        GetNodeString(t.Predicate),
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
                        GetNodeString(t.Subject),
                        GetNodeString(t.Predicate),
                        GetNodeString(t.Object),
                        false,
                        null,
                        null,
                        defaultGraphUri ?? Constants.DefaultGraphUri
                        );
                }
            }
            _store.Commit(Guid.Empty);
        }

        private static string GetNodeString(INode node)
        {
            if (node.NodeType == NodeType.Uri)
            {
                return (node as UriNode).Uri.ToString();
            }
            if (node.NodeType == NodeType.Blank)
            {
                return (node as BlankNode).InternalID;
            }
            Assert.Fail("Unexpected node type in GetNodeString: {0}", node.NodeType);
            return null;
        }


		private void ImportGraph(string dataPath, Uri graphUri) {
            ImportData(dataPath, graphUri.ToString());
		}

		private string ExecuteQuery(string queryPath)
		{
		    var queryExp = File.ReadAllText(queryPath);
            return _store.ExecuteSparqlQuery(queryExp, SparqlResultsFormat.Xml);
		}

		private void CheckResult(string results, string expectedResultPath, bool laxCardinality) 
        {
            Assert.IsNotNull(results);
		    var resultExtension = Path.GetExtension(expectedResultPath).ToLower();
            if (resultExtension.Equals(".srx"))
            {
                CompareSparqlResults(results, expectedResultPath, laxCardinality);
            } 
            else if (resultExtension.Equals(".ttl"))
            {
                CompareResultGraphs(results, expectedResultPath, laxCardinality);
            }
            else {
				Assert.Fail("Don't know how to compare results to results file {0}", expectedResultPath);              
			}
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
            CompareSparqlResults(actualResultSet, expectedResultSet, reduced);
        }

        private void CompareResultGraphs(string results, string expectedResultsPath, bool reduced)
        {
            var rdfParser = new SparqlRdfParser();
            var xmlParser = new SparqlXmlParser();
            var actualResultSet = new SparqlResultSet();
            var expectedResultSet = new SparqlResultSet();
            using(var tr = new StringReader(results))
            {
                xmlParser.Load(actualResultSet, tr);
            }
            rdfParser.Load(expectedResultSet, expectedResultsPath);
            CompareSparqlResults(actualResultSet, expectedResultSet, reduced);
        }

        private void CompareSparqlResults(SparqlResultSet actual, SparqlResultSet expected, bool reduced)
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
                                    "Unexpected number of rows in results. Expected {0}, got {1}",
                                    expected.Results.Count, actual.Results.Count);
                }
                foreach (var actualSolution in actual.Results)
                {
                    Assert.IsTrue(expected.Results.Any(x => CompareSolutions(actualSolution, x)),
                                  "Could not find a match for solution {0} in the expected results set", actualSolution);
                }
            }
            else
            {
                Assert.Fail("Cannot compare results to result set of type {0}", expected.ResultsType);
            }
        }

        private static bool CompareSolutions(SparqlResult x, SparqlResult y)
        {
            if (x.Variables.Count().Equals(y.Variables.Count()) &&
                x.Variables.All(xv=>y.Variables.Contains(xv)))
            {
                foreach (var xv in x.Variables)
                {
                    var xb = x[xv];
                    var yb = y[xv];
                    if (!CompareNodes(xb, yb)) return false;
                }
                return true;
            }
            return false;
        }

        private static bool CompareNodes(INode xb, INode yb)
        {
            if (!xb.NodeType.Equals(yb.NodeType)) return false;
            switch(xb.NodeType)
            {
                case NodeType.Literal:
                    var xl = xb as LiteralNode;
                    var yl = yb as LiteralNode;
                    if (!xl.Value.Equals(yl.Value)) return false;
                    var xd = xl.DataType == null ? RdfDatatypes.PlainLiteral : xl.DataType.ToString();
                    var yd = yl.DataType == null ? RdfDatatypes.PlainLiteral : yl.DataType.ToString();
                    if (!xd.Equals(yd)) return false;
                    var xlang = xl.Language ?? String.Empty;
                    var ylang = yl.Language ?? String.Empty;
                    if (!xlang.Equals(ylang)) return false;
                    break;
                case NodeType.Uri:
                    var xu = xb as UriNode;
                    var yu = yb as UriNode;
                    if (!xu.Uri.Equals(yu.Uri)) return false;
                    break;
            }
            return true;
        }

        private static void CompareTripleCollections(BaseTripleCollection actualTriples, BaseTripleCollection expectedTriples, bool reduced) 
		{
            var alreadySeen = new HashSet<Triple>();
            foreach (var expectedTriple in expectedTriples)
            {
                if (reduced && alreadySeen.Contains(expectedTriple)) continue;
                Assert.IsTrue(actualTriples.Contains(expectedTriple),
                              "Could not find expected triple '{0}' in results set.", expectedTriple);
            }
            foreach(var actualTriple in actualTriples)
            {
                Assert.IsTrue(expectedTriples.Contains(actualTriple),
                    "Unexpected result set triple '{0}'", actualTriple);
            }
		}

		#endregion
	}
}