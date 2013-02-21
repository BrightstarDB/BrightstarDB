using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace SparqlTestTasks
{
    internal class TestManifest
    {
        public string ManifestFilePath { get; private set; }
        public Uri ManifestDirectory { get; private set; }
        public string OutputFilePath { get; private set; }
        public string ClassName { get; private set; }
        public string Namespace { get; private set; }
        private int _unamedTestCount = 0;

        public IEnumerable<SparqlTestCase> TestCases
        {
            get { return _testCases; }
        }

        private static readonly Uri RdfFirst = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#first");
        private static readonly Uri RdfRest = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#rest");
        private static readonly Uri RdfType = new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
        private static readonly Uri NegativeSyntaxTest = new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#NegativeSyntaxTest");
        private static readonly Uri PositiveSyntaxTest = new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#PositiveSyntaxTest");
        private static readonly Uri QueryEvaluationTest = new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#QueryEvaluationTest");
        private static readonly Uri GraphData = new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-query#graphData");
        private static readonly Uri UtData = new Uri("http://www.w3.org/2009/sparql/tests/test-update#data");
        private static readonly Uri UtGraphData = new Uri("http://www.w3.org/2009/sparql/tests/test-update#graphData");
        private static readonly Uri UtGraph = new Uri("http://www.w3.org/2009/sparql/tests/test-update#graph");
        private static readonly Uri RdfsLabel = new Uri("http://www.w3.org/2000/01/rdf-schema#label");
        private List<SparqlTestCase> _testCases;

        public TestManifest(ITaskItem manifestTaskItem, DirectoryInfo outputDirectory, string defaultNamespace, bool nameForParentDirectory)
        {
            ManifestFilePath = manifestTaskItem.ItemSpec;
            ManifestDirectory = new Uri(Path.GetFullPath(Path.GetDirectoryName(ManifestFilePath)));
            var className = manifestTaskItem.GetMetadata("TestClass");
            if (String.IsNullOrEmpty(className))
            {
                if (nameForParentDirectory)
                {
                    FileInfo f = new FileInfo(ManifestFilePath);
                    className = f.Directory.Name;
                }
                else 
                {
                    className = Path.GetFileNameWithoutExtension(ManifestFilePath);
                }
            }
            ClassName = CleanupClassName(className);
            OutputFilePath = outputDirectory.FullName + Path.DirectorySeparatorChar + ClassName + ".cs";
            Namespace = manifestTaskItem.GetMetadata("TestNamespace");
            if (String.IsNullOrEmpty(Namespace)) Namespace = defaultNamespace;
            _testCases = new List<SparqlTestCase>();
            ProcessManifest();
        }

        public bool IsUpToDate()
        {
#if DEBUG
            return false;
#endif
            return File.Exists(OutputFilePath) &&
                   File.GetCreationTime(OutputFilePath) > File.GetCreationTime(ManifestFilePath);
        }

        private void ProcessManifest()
        {
            var baseGraph = new Graph();
            FileLoader.Load(baseGraph, ManifestFilePath);
            ProcessIncludes(baseGraph);
            ProcessEntries(baseGraph);
        }

        private static void ProcessIncludes(Graph g)
        {
            var includeTriple =
                g.GetTriplesWithPredicate(new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#include")).
                    FirstOrDefault();
            while(includeTriple != null) {
                if (includeTriple.Object.NodeType == NodeType.Blank)
                {
                    ProcessList(includeTriple.Object, g, ProcessInclude);
                }
                else
                {
                    ProcessInclude(includeTriple.Object, g);
                }
                g.Retract(includeTriple);
                includeTriple = g.GetTriplesWithPredicate(new Uri("http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#include")).
                    FirstOrDefault();
            }
        }

        private static void ProcessInclude(INode n, Graph g)
        {
            if (n is UriNode)
            {

                Console.WriteLine("Loading manifest file: {0}", (n as UriNode).Uri.LocalPath);
                var includeGraph = new Graph();
                FileLoader.Load(includeGraph, (n as UriNode).Uri.LocalPath);
                g.Merge(includeGraph);
            }
        }

        private void ProcessEntries(BaseGraph g)
        {
            var sparqlResults = g.ExecuteQuery(
                @"PREFIX mf: <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#>
PREFIX qt: <http://www.w3.org/2001/sw/DataAccess/tests/test-query#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
SELECT ?testCase ?name ?comment ?query ?data ?result ?action WHERE {
?testCase a mf:QueryEvaluationTest .
?testCase mf:name ?name .
OPTIONAL { ?testCase rdfs:comment ?comment } .
?testCase mf:action ?action .
?action qt:query ?query .
?action qt:data  ?data .
?testCase mf:result ?result . }")
                               as SparqlResultSet;
            foreach(var result in sparqlResults.Results)
            {
                var qe = 
                new QueryEvaluationTestCase
                                   {
                                       Definition = (result["testCase"] as UriNode).Uri,
                                       Name = result["name"].ToString(),
                                       Comment = result.HasValue("comment") ? result["comment"].ToString() : null,
                                       Query = (result["query"] as UriNode).Uri,
                                       Data = (result["data"] as UriNode).Uri,
                                       Result = (result["result"] as UriNode).Uri,
                                       GraphData = new List<Uri>()
                                   };
                if (String.IsNullOrEmpty(qe.Name))
                {
                    _unamedTestCount++;
                    qe.Name = "UnamedTest_" + _unamedTestCount;
                }
                var action = result["action"] as INode;
                foreach(var t in g.GetTriplesWithSubjectPredicate(action, g.CreateUriNode(GraphData)))
                {
                    qe.GraphData.Add((t.Object as UriNode).Uri);
                }
                _testCases.Add(qe);
            }

            var updateEvaluationTests =
                g.ExecuteQuery(
                    @"
PREFIX mf: <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#>
PREFIX qt: <http://www.w3.org/2001/sw/DataAccess/tests/test-query#>
PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>
PREFIX ut:      <http://www.w3.org/2009/sparql/tests/test-update#>
SELECT ?test ?name ?comment ?action ?result ?request WHERE {
    ?test a ut:UpdateEvaluationTest .
    ?test mf:name ?name .
    OPTIONAL { ?test rdfs:comment ?comment }
    ?test mf:action ?action .
    ?test mf:result ?result .
    ?action ut:request ?request .
}")
                as SparqlResultSet;
            foreach(var updateEvaluationTest in updateEvaluationTests.Results)
            {
                var uet = new UpdateEvaluationTestCase
                              {
                                  Definition = (updateEvaluationTest["test"] as IUriNode).Uri,
                                  Name = updateEvaluationTest["name"].ToString(),
                                  Comment =
                                      updateEvaluationTest.HasValue("comment")
                                          ? updateEvaluationTest["comment"].ToString()
                                          : null,
                                  Request = (updateEvaluationTest["request"] as IUriNode).Uri
                              };
                var actionNode = updateEvaluationTest["action"];
                var resultNode = updateEvaluationTest["result"];
                foreach(var t in g.GetTriplesWithSubjectPredicate(actionNode, g.CreateUriNode(UtData)))
                {
                    uet.PreData = (t.Object as IUriNode).Uri;
                }
                foreach (var t in g.GetTriplesWithSubjectPredicate(actionNode, g.CreateUriNode(UtGraphData)))
                {
                    uet.PreGraphData.Add(GetUpdateGraph(t.Object, g));
                }
                
                foreach(var t in g.GetTriplesWithSubjectPredicate(resultNode, g.CreateUriNode(UtData)))
                {
                    uet.PostData = (t.Object as IUriNode).Uri;
                }
                foreach (var t in g.GetTriplesWithSubjectPredicate(resultNode, g.CreateUriNode(UtGraphData)))
                {
                    uet.PostGraphData.Add(GetUpdateGraph(t.Object, g));
                }
                _testCases.Add(uet);
            }
        }

        private UpdateGraph GetUpdateGraph(INode graphData, IGraph g)
        {
            var dataUri = g.GetTriplesWithSubjectPredicate(graphData, g.CreateUriNode(UtGraph)).FirstOrDefault();
            if (dataUri == null)
            {
                dataUri = g.GetTriplesWithSubjectPredicate(graphData, g.CreateUriNode(UtData)).FirstOrDefault();
            }
            if (dataUri == null)
            {
                var allTriples = g.GetTriplesWithSubject(graphData).ToList();
                throw new Exception("No data for graph");
            }
            var graphLabel = g.GetTriplesWithSubjectPredicate(graphData, g.CreateUriNode(RdfsLabel)).First();
            return new UpdateGraph{
                Data = (dataUri.Object as IUriNode).Uri,
                Uri = new Uri(graphLabel.Object.ToString())
            };
        }

        private static void ProcessList(INode listNode, Graph g, Action<INode, Graph> processAction)
        {
            var first =
                g.GetTriplesWithSubject(listNode).Where(
                    t => (t.Predicate as UriNode).Uri.ToString().Equals(RdfFirst.ToString())).Select(t => t.Object).
                    FirstOrDefault();
            var rest =
                g.GetTriplesWithSubject(listNode).Where(
                    t => (t.Predicate as UriNode).Uri.ToString().Equals(RdfRest.ToString())).Select(t => t.Object).
                    FirstOrDefault();
            if (first != null)
            {
                processAction(first, g);
            }
            if (rest != null)
            {
                ProcessList(rest, g, processAction);
            }
        }

        static string CleanupClassName(string input)
        {
            var classNameBuilder = new StringBuilder();
            string[] parts = input.Split(new[]{'-',' '});
            foreach (var part in parts)
            {
              if (part.Length > 0)
              {
                  classNameBuilder.Append(Char.ToUpper(part[0]));
                  classNameBuilder.Append(part.Substring(1));
              }  
            }
            return classNameBuilder.ToString();
        }
    }
}