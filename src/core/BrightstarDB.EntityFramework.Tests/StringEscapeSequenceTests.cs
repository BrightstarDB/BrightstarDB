using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Remotion.Linq.Clauses;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class StringEscapeSequenceTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeContext();
            Context.FilterOptimizationEnabled = true;
        }

        [TestCase("\t", "\\t")]
        [TestCase("\n", "\\n")]
        [TestCase("\r", "\\r")]
        [TestCase("\b", "\\b")]
        [TestCase("\f", "\\f")]
        [TestCase("\"", "\"")] // Don't need to escape double quotes inside single-quoted string and DNR complains if we do.
        [TestCase("\'", "\\'")]
        [TestCase("\\", "\\\\")]
        public void TestSparqlStringEscape(string sep, string escaped)
        {
            var linqString = "Foo" + sep + "Bar";
            var expectSparqlString = "Foo" + escaped + "Bar";
            var q = from p in Context.Dinners where p.Title == linqString select p;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(@"CONSTRUCT { ?p ?p_p ?p_o. ?p <http://www.brightstardb.com/.well-known/model/selectVariable> ""p"" .} WHERE { ?p ?p_p ?p_o . {SELECT ?p WHERE {?p a <http://www.networkedplanet.com/schemas/test/Dinner> . { ?p <http://purl.org/dc/terms/title> '" + expectSparqlString + @"' . } } } }"),
                NormalizeSparql(lastSparql));
        }
    }
}
