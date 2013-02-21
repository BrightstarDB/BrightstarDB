using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SparqlTestTasks
{
    internal class SparqlTestCase
    {
        public string Name { get; set; }
        public string Comment { get; set; }
        public Uri Definition { get; set; }

        public string GetTestMethodName()
        {
            return MakeCSharpIdentifier(Name);
        }

        private static string MakeCSharpIdentifier(string input)
        {
            var identifierBuilder = new StringBuilder();
            foreach(var part in input.Split(new char[] {' ', '-', ',', '/', '\'', ':', '(',')','{','}'}, StringSplitOptions.RemoveEmptyEntries))
            {
                identifierBuilder.Append(Char.ToUpper(part[0]));
                if (part.Length > 1)
                {
                    identifierBuilder.Append(part.Substring(1).ToLower());
                }
            }
            return
                identifierBuilder.ToString()
                    .Replace("+", "_Plus_")
                    .Replace("*", "_Asterix_")
                    .Replace("?", "_QuestionMark_")
                    .Replace("||", "OrOperator")
                    .Replace("&&", "AndOperator");
        }
    }

    internal class QueryEvaluationTestCase : SparqlTestCase
    {
        public Uri Data { get; set; }
        public Uri Query { get; set; }
        public Uri Result { get; set; }
        public List<Uri> GraphData { get; set; }
        public bool LaxCardinality { get; set; }
    }
}
