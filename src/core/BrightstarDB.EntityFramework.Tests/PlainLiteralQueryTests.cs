using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Rdf;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class PlainLiteralQueryTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            base.InitializeContext();
        }

        [Test]
        public void TestPlainLiteralMatching()
        {
            var q = Context.Concepts.Where(c => c.PrefLabel.Equals(new PlainLiteral("Test", "en"))).Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v0 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Concept> .
{ ?c <http://www.networkedplanet.com/schemas/test/prefLabel> 'Test'@en . }
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v0)
}");
        }

        [Test]
        public void TestPlainLiteralCollectionMatching()
        {
            var q =
                Context.Concepts.Where(
                    c => c.AltLabels.Any(alt => alt.Equals(new PlainLiteral("Another Test", "en-gb"))))
                       .Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Concept> .
FILTER EXISTS {
    ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?alt .
    FILTER (?alt = 'Another Test'@en-gb) . 
} 
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}");
        }

        [Test]
        public void TestSelectPlainLiteralByLanguage()
        {
            var q = Context.Concepts.Where(c => c.AltLabels.Any(alt => alt.Language.Equals("en-gb")))
                           .Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Concept> .
FILTER EXISTS {
    ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?alt .
    FILTER ((LANG(?alt)) = 'en-gb') .
} 
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}");
        }

        [Test]
        public void TestByPlainLiteralRegex()
        {
            var q =
                Context.Concepts.Where(
                    c => c.AltLabels.Any(alt => alt.Value.StartsWith("Networked") && alt.Language.Equals("en-gb")))
                       .Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
    ?c a <http://www.networkedplanet.com/schemas/test/Concept> .
    FILTER EXISTS {
        ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?alt .
        FILTER ((STRSTARTS((STR(?alt)), 'Networked')) && ((LANG(?alt)) = 'en-gb')) .
    } 
    BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}");
        }

        [Test]
        public void TestSelectLiterals()
        {
            var q = Context.Concepts.SelectMany(c => c.AltLabels.Where(alt => alt.Language.Equals("en-gb")));
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?x003Cgeneratedx003Ex005Fx0030 WHERE {
    ?c a <http://www.networkedplanet.com/schemas/test/Concept> .
    ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?x003Cgeneratedx003Ex005Fx0030 .
    FILTER ((LANG(?x003Cgeneratedx003Ex005Fx0030)) = 'en-gb') .
}");
        }
    }
}
