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
            var q = Context.Concepts.Where(c => c.PrefLabel.Equals(new PlainLiteral("Test", "en")));
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?c WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Concept> .
?c <http://www.networkedplanet.com/schemas/test/prefLabel> ?v0 .
FILTER(?v0 = 'Test'@en) . }");
        }

        [Test]
        public void TestPlainLiteralCollectionMatching()
        {
            var q =
                Context.Concepts.Where(
                    c => c.AltLabels.Any(alt => alt.Equals(new PlainLiteral("Another Test", "en-gb"))));
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?c WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Concept> .
FILTER EXISTS {
    ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?alt .
    FILTER (?alt = 'Another Test'@en-gb) . 
} }");
        }

        [Test]
        public void TestSelectPlainLiteralByLanguage()
        {
            var q = Context.Concepts.Where(c => c.AltLabels.Any(alt => alt.Language.Equals("en-gb")));
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?c WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Concept> .
FILTER EXISTS {
    ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?alt .
    FILTER ((LANG(?alt)) = 'en-gb') .
} }");
        }

        [Test]
        public void TestByPlainLiteralRegex()
        {
            var q =
                Context.Concepts.Where(
                    c => c.AltLabels.Any(alt => alt.Value.StartsWith("Networked") && alt.Language.Equals("en-gb")));
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?c WHERE {
    ?c a <http://www.networkedplanet.com/schemas/test/Concept> .
    FILTER EXISTS {
        ?c <http://www.networkedplanet.com/schemas/test/altLabel> ?alt .
        FILTER ((STRSTARTS((STR(?alt)), 'Networked')) && ((LANG(?alt)) = 'en-gb')) .
    } }");
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
