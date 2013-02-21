using System;
using System.Collections.Generic;

namespace SparqlTestTasks
{
    internal class UpdateEvaluationTestCase : SparqlTestCase
    {
        public UpdateEvaluationTestCase()
        {
            PreGraphData = new List<UpdateGraph>();
            PostGraphData = new List<UpdateGraph>();
        }

        public Uri Request { get; set; }
        public Uri PreData { get; set; }
        public List<UpdateGraph> PreGraphData { get; set; }

        public Uri PostData { get; set; }
        public List<UpdateGraph> PostGraphData { get; set; }
    }

    internal class UpdateGraph
    {
        public Uri Data { get; set; }
        public Uri Uri { get; set; }
    }
}