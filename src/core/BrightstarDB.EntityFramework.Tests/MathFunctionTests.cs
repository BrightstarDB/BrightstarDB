using System;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class MathFunctionTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            base.InitializeContext();
        }

        [Test]
        public void TestRound()
        {
            var q = Context.Companies.Where(c => Math.Round(c.CurrentSharePrice) > 10).Select(c=>c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/price> ?v0 .
FILTER((ROUND(?v0)) > '10.00'^^<http://www.w3.org/2001/XMLSchema#decimal>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}"
                );

            q = Context.Companies.Where(c => Math.Round(c.CurrentMarketCap) > 100).Select(c=>c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/marketCap> ?v0 .
FILTER((ROUND(?v0)) > '1.000000E+002'^^<http://www.w3.org/2001/XMLSchema#double>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}"
                );
        }

        [Test]
        public void TestFloor()
        {
            var q = Context.Companies.Where(c => Math.Floor(c.CurrentSharePrice) > 10).Select(c=>c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/price> ?v0 .
FILTER((FLOOR(?v0)) > '10.00'^^<http://www.w3.org/2001/XMLSchema#decimal>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}"
                );

            q = Context.Companies.Where(c => Math.Floor(c.CurrentMarketCap) > 100).Select(c=>c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/marketCap> ?v0 .
FILTER((FLOOR(?v0)) > '1.000000E+002'^^<http://www.w3.org/2001/XMLSchema#double>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}"
                );
        }

        [Test]
        public void TestCeiling()
        {
            var q = Context.Companies.Where(c => Math.Ceiling(c.CurrentSharePrice) > 10).Select(c=>c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/price> ?v0 .
FILTER((CEIL(?v0)) > '10.00'^^<http://www.w3.org/2001/XMLSchema#decimal>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );

            q = Context.Companies.Where(c => Math.Ceiling(c.CurrentMarketCap) > 100).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://www.networkedplanet.com/schemas/test/marketCap> ?v0 .
FILTER((CEIL(?v0)) > '1.000000E+002'^^<http://www.w3.org/2001/XMLSchema#double>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}"
                );
        }
    }
}
