using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class NumericComparatorsTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeContext();
        }

        [Test]
        public void TestLessThanIntegerProperty()
        {
            var q = from c in Context.Companies
                    where c.HeadCount < 10
                    select c.Id;
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE { 
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
FILTER (?v0 < '10'^^<http://www.w3.org/2001/XMLSchema#integer>) .
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}");
        }

        [Test]
        public void TestNotLessThanIntegerProperty()
        {
            var q = from c in Context.Companies
                    where !(c.HeadCount < 10)
                    select c.Id;
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE { 
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
FILTER (!((?v0 < '10'^^<http://www.w3.org/2001/XMLSchema#integer>))) .
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}");
        }

    }
}
