using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class StringFunctionTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            base.InitializeContext();
        }

        [Test]
        public void TestStartsWith()
        {
            var q = Context.Companies.Where(c => c.Name.StartsWith("Netw")).Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (STRSTARTS(?v0, 'Netw')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.StartsWith("Netw", true, CultureInfo.InvariantCulture)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, '^Netw', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.StartsWith("Netw", StringComparison.CurrentCultureIgnoreCase)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, '^Netw', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.StartsWith("Netw", StringComparison.InvariantCultureIgnoreCase)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, '^Netw', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.StartsWith("Netw", StringComparison.OrdinalIgnoreCase)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, '^Netw', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

        }

        [Test]
        public void TestEndsWith()
        {
            var q = Context.Companies.Where(c => c.Name.EndsWith("net")).Select(c=>c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (STRENDS(?v0, 'net')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.EndsWith("net", true, CultureInfo.CurrentCulture)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.EndsWith("net", StringComparison.CurrentCulture)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.EndsWith("net", StringComparison.InvariantCulture)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.EndsWith("net", StringComparison.Ordinal)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");


            q = Context.Companies.Where(c => c.Name.EndsWith("net", StringComparison.CurrentCultureIgnoreCase)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.EndsWith("net", StringComparison.InvariantCultureIgnoreCase)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

            q = Context.Companies.Where(c => c.Name.EndsWith("net", StringComparison.OrdinalIgnoreCase)).Select(c => c.Id);
            results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (regex(?v0, 'net$', 'i')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");

        }

        [Test]
        public void TestContains()
        {
            var q = Context.Companies.Where(c => c.Name.Contains("work")).Select(c=>c.Id);
            var results = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER (CONTAINS(?v0, 'work')).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");
        }

        [Test]
        public void TestLength()
        {
            var q = Context.Companies.Where(c => c.Name.Length > 10).Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER ((STRLEN(?v0)) > '10'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");
        }

        [Test]
        public void TestSubstring()
        {
            var q = Context.Companies.Where(c => c.Name.Substring(5).Equals("rkedPlanet")).Select(c=>c.Id);
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER ((SUBSTR(?v0, '6'^^<http://www.w3.org/2001/XMLSchema#integer>)) = 'rkedPlanet').
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}"); // 6 in SPARQL becuase their string indexing is 1-based

            q = Context.Companies.Where(c => c.Name.Substring(5, 3).Equals("rke")).Select(c=>c.Id);
            results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER ((SUBSTR(?v0, '6'^^<http://www.w3.org/2001/XMLSchema#integer>, '3'^^<http://www.w3.org/2001/XMLSchema#integer>)) = 'rke').
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");
        }

        [Test]
        public void TestToUpper()
        {
            var q = Context.Companies.Where(c => c.Name.ToUpper().Equals("NETWORKEDPLANET")).Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER ((UCASE(?v0)) = 'NETWORKEDPLANET').
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");
        }

        [Test]
        public void TestToLower()
        {
            var q = Context.Companies.Where(c => c.Name.ToLower().Equals("networkedplanet")).Select(c => c.Id);
            var results = q.ToList();
            AssertQuerySparql(@"SELECT ?v1 WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?c <http://purl.org/dc/terms/title> ?v0 .
FILTER ((LCASE(?v0)) = 'networkedplanet').
BIND(STRAFTER(STR(?c), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)}");
        }

    }
}
