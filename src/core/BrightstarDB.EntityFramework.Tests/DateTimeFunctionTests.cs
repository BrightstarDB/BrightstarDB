using System.Linq;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class DateTimeFunctionTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            base.InitializeContext();
        }

        [Test]
        public void TestDay()
        {
            var q = Context.Dinners.Where(d => d.EventDate.Day == 1).Select(d=>d.Id);
            var result = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
?d <http://www.networkedplanet.com/schemas/test/date> ?v0 .
FILTER((DAY(?v0)) = '1'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );
        }

        [Test]
        public void TestHour()
        {
            var q = Context.Dinners.Where(d => d.EventDate.Hour == 1).Select(d=>d.Id);
            var result = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
?d <http://www.networkedplanet.com/schemas/test/date> ?v0 .
FILTER((HOURS(?v0)) = '1'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );
        }

        [Test]
        public void TestMinute()
        {
            var q = Context.Dinners.Where(d => d.EventDate.Minute == 1).Select(d=>d.Id);
            var result = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
?d <http://www.networkedplanet.com/schemas/test/date> ?v0 .
FILTER((MINUTES(?v0)) = '1'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );
        }

        [Test]
        public void TestMonth()
        {
            var q = Context.Dinners.Where(d => d.EventDate.Month == 1).Select(d=>d.Id);
            var result = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
?d <http://www.networkedplanet.com/schemas/test/date> ?v0 .
FILTER((MONTH(?v0)) = '1'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );
        }

        [Test]
        public void TestSecond()
        {
            var q = Context.Dinners.Where(d => d.EventDate.Second == 1).Select(d=>d.Id);
            var result = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
?d <http://www.networkedplanet.com/schemas/test/date> ?v0 .
FILTER((SECONDS(?v0)) = '1'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );
        }

        [Test]
        public void TestYear()
        {
            var q = Context.Dinners.Where(d => d.EventDate.Year == 2011).Select(d=>d.Id);
            var result = q.ToList();
            AssertQuerySparql(
                @"SELECT ?v1 WHERE {
?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
?d <http://www.networkedplanet.com/schemas/test/date> ?v0 .
FILTER((YEAR(?v0)) = '2011'^^<http://www.w3.org/2001/XMLSchema#integer>).
BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v1)
}"
                );
        }
    }
}
