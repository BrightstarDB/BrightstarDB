using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestClass]
    public class NumericComparatorsTests : LinqToSparqlTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            InitializeContext();
        }

        [TestMethod]
        public void TestLessThanIntegerProperty()
        {
            var q = from c in Context.Companies
                    where c.HeadCount < 10
                    select c;
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?c WHERE { 
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
FILTER (?v0 < '10'^^<http://www.w3.org/2001/XMLSchema#integer>) .}");
        }

        [TestMethod]
        public void TestNotLessThanIntegerProperty()
        {
            var q = from c in Context.Companies
                    where !(c.HeadCount < 10)
                    select c;
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?c WHERE { 
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
FILTER (!((?v0 < '10'^^<http://www.w3.org/2001/XMLSchema#integer>))) .}");
        }

    }
}
