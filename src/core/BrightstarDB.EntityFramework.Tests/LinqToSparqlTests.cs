using System.Linq;
using BrightstarDB.EntityFramework.Tests.ContextObjects;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class LinqToSparqlTests : LinqToSparqlTestBase
    {

        [SetUp]
        public void SetUp()
        {
            InitializeContext();
        }

        [Test]
        public void CheckContextTypeMappings()
        {
            Assert.AreEqual("http://www.networkedplanet.com/schemas/test/Dinner", Context.MapTypeToUri(typeof (IDinner)));
            Assert.AreEqual("http://www.networkedplanet.com/schemas/test/Rsvp", Context.MapTypeToUri(typeof(IRsvp)));
        }

        [Test]
        public void TestDinnerPropertyMappings()
        {
            var dinnerType = typeof (IDinner);
            var id = dinnerType.GetProperty("Id");
            var hint = Context.GetPropertyHint(id);
            Assert.IsNotNull(hint);
            Assert.AreEqual(PropertyMappingType.Id, hint.MappingType);
            var title = dinnerType.GetProperty("Title");
            hint = Context.GetPropertyHint(title);
            Assert.IsNotNull(hint);
            Assert.AreEqual(PropertyMappingType.Property, hint.MappingType);
            Assert.AreEqual("http://purl.org/dc/terms/title", hint.SchemaTypeUri);

            var rsvps = dinnerType.GetProperty("Rsvps");
            hint = Context.GetPropertyHint(rsvps);
            Assert.IsNotNull(hint);
            Assert.AreEqual(PropertyMappingType.Arc, hint.MappingType);
            Assert.AreEqual("http://www.networkedplanet.com/schemas/test/attendees", hint.SchemaTypeUri);
        }

        [Test]
        public void TestGetAllDinners()
        {
            var q = from p in Context.Dinners select p;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(@"CONSTRUCT { ?p ?p_p ?p_o. ?p <http://www.brightstardb.com/.well-known/model/selectVariable> ""p"" .} WHERE { ?p ?p_p ?p_o . {SELECT ?p WHERE {?p a <http://www.networkedplanet.com/schemas/test/Dinner> .} } }"),
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestGetDinnerById()
        {
            var q = from p in Context.Dinners where p.Id.Equals("1") select p;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql("ASK { <id:1> a <http://www.networkedplanet.com/schemas/test/Dinner> . }"),
                NormalizeSparql(lastSparql));

            var q2 = Context.Dinners.Where(x => x.Id == "1").FirstOrDefault();
            lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql("ASK { <id:1> a <http://www.networkedplanet.com/schemas/test/Dinner> . }"),
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestIdEscaping(){
            var q = Context.Dinners.FirstOrDefault(x => x.Id == "foo bar");
            AssertQuerySparql("ASK { <id:foo%20bar> a <http://www.networkedplanet.com/schemas/test/Dinner> . }");
        }

        [Test]
        public void TestGetRsvpByDinnerId()
        {
            var q = from x in Context.Rsvps where x.Dinner.Id.Equals("1") select x;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" .} WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE { 
    ?x a <http://www.networkedplanet.com/schemas/test/Rsvp> .
    <id:1> <http://www.networkedplanet.com/schemas/test/attendees> ?x.} } }");
        }

        [Test]
        public void TestGetDinnersProperty()
        {
            var q = from p in Context.Dinners select p.Title;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(NormalizeSparql(@"SELECT ?v0 WHERE {
                ?p a <http://www.networkedplanet.com/schemas/test/Dinner> .
                ?p <http://purl.org/dc/terms/title> ?v0 .}"),
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestGetAllDinnerRsvps()
        {
            var q = from p in Context.Dinners
                    where p.Id.Equals("address")
                    select p.Rsvps;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(
                    @"SELECT ?v0 WHERE {
                    <id:address> a <http://www.networkedplanet.com/schemas/test/Dinner> .
                    <id:address> <http://www.networkedplanet.com/schemas/test/attendees> ?v0 . }"),
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestGetAllDinnerRsvps2()
        {
            // This query uses == instead of .Equals but should result in the same SPARQL
            var q = from p in Context.Dinners
                    where p.Id == "address"
                    select p.Rsvps;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(
                    @"SELECT ?v0 WHERE {
                    <id:address> a <http://www.networkedplanet.com/schemas/test/Dinner> .
                    <id:address> <http://www.networkedplanet.com/schemas/test/attendees> ?v0 . }"),
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestGetAllClashesWithSpecificDinner()
        {
            var q = from x in Context.Dinners
                    where x.Id == "address"
                    from y in Context.Dinners
                    where y.EventDate.Equals(x.EventDate) && (x.Id != y.Id)
                    select y;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(
                    @"CONSTRUCT { ?y ?y_p ?y_o. ?y <http://www.brightstardb.com/.well-known/model/selectVariable> ""y"" .} WHERE {
?y ?y_p ?y_o . {
    SELECT ?y WHERE {
    <id:address> a <http://www.networkedplanet.com/schemas/test/Dinner> .
    ?y a <http://www.networkedplanet.com/schemas/test/Dinner> .
    ?y <http://www.networkedplanet.com/schemas/test/date> ?v0 .
    <id:address> <http://www.networkedplanet.com/schemas/test/date> ?v1 .
    FILTER ((?v0=?v1) && (!sameTerm(<id:address>, ?y)))
    .} } }"),
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestOneHopWithoutFilter()
        {
            var q = from x in Context.Dinners
                    from r in x.Rsvps
                    select r.AttendeeEmail;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(
                    @"SELECT ?v1 WHERE {
?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
?r a <http://www.networkedplanet.com/schemas/test/Rsvp> .
?x <http://www.networkedplanet.com/schemas/test/attendees> ?r .
?r <http://www.networkedplanet.com/schemas/test/email> ?v1 .}"), // NOTE: v1 not v0 becuase v0 gets used in processing the additional FROM clause
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestOneHopSelect()
        {
            var q = Context.People.Where(p => p.Father.Id == "address");
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(NormalizeSparql(
                @"CONSTRUCT { ?p ?p_p ?p_o. ?p <http://www.brightstardb.com/.well-known/model/selectVariable> ""p"" .} WHERE {
?p ?p_p ?p_o . {
    SELECT ?p WHERE { 
    ?p a <http://www.networkedplanet.com/schemas/test/Person> .
    ?p <http://www.networkedplanet.com/schemas/test/father> <id:address> .} } }"), NormalizeSparql(lastSparql));
        }


        [Test]
        public void TestDinnerByAttendee()
        {
            var q = from x in Context.Dinners
                    from r in x.Rsvps
                    where r.AttendeeEmail == "kal@networkedplanet.com"
                    select x;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.IsNotNull(lastSparql);
            Assert.AreEqual(
                NormalizeSparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
    ?r a <http://www.networkedplanet.com/schemas/test/Rsvp> .
    ?x <http://www.networkedplanet.com/schemas/test/attendees> ?r .
    ?r <http://www.networkedplanet.com/schemas/test/email> ?v1 .
    FILTER(?v1 = 'kal@networkedplanet.com') . } } }"), 
                NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestLinqJoin()
        {
            var q = from x in Context.Dinners
                    join r in Context.Rsvps on x.Host equals r.AttendeeEmail
                    select r;
            var results = q.ToList();
            AssertQuerySparql(
                              @"CONSTRUCT { ?r ?r_p ?r_o. ?r <http://www.brightstardb.com/.well-known/model/selectVariable> ""r"" .} WHERE {
?r ?r_p ?r_o . {
    SELECT ?r WHERE { 
    ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
    ?r a <http://www.networkedplanet.com/schemas/test/Rsvp> .
    ?r <http://www.networkedplanet.com/schemas/test/email> ?v0 .
    ?x <http://www.networkedplanet.com/schemas/test/host> ?v1 .
    FILTER (?v0=?v1) . } } }");
        }

        [Test]
        public void TestDecimalComparators()
        {
            var q = from x in Context.Companies
                    where x.CurrentSharePrice < 1.0m
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/price> ?v0 .
    FILTER (?v0 < '1.00'^^<http://www.w3.org/2001/XMLSchema#decimal>) . } } }");

            q = from x in Context.Companies
                where x.CurrentSharePrice <= 1.1m
                select x;
            q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/price> ?v0 .
    FILTER (?v0 <= '1.10'^^<http://www.w3.org/2001/XMLSchema#decimal>) . } } }");

            q = from x in Context.Companies
                where x.CurrentSharePrice > 1.2m
                select x;
            q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/price> ?v0 .
    FILTER (?v0 > '1.20'^^<http://www.w3.org/2001/XMLSchema#decimal>) . } } }");

            q = from x in Context.Companies
                where x.CurrentSharePrice >= 1.3m
                select x;
            q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/price> ?v0 .
    FILTER (?v0 >= '1.30'^^<http://www.w3.org/2001/XMLSchema#decimal>) . } } }");


            q = from x in Context.Companies
                where x.CurrentSharePrice >= 1.0m && x.CurrentSharePrice <= 2.0m
                select x;
            q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/price> ?v0 .
    FILTER ((?v0 >= '1.00'^^<http://www.w3.org/2001/XMLSchema#decimal>) && (?v0 <= '2.00'^^<http://www.w3.org/2001/XMLSchema#decimal>)) . } } }");

        }

        [Test]
        public void TestDoubleConstant()
        {
            var q = from x in Context.Companies
                    where x.CurrentMarketCap < 1.0e06
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/marketCap> ?v0 .
    FILTER (?v0 < '1.000000E+006'^^<http://www.w3.org/2001/XMLSchema#double>) . } } }");

        }

        [Test]
        public void TestIntegerConstant()
        {
            var q = from x in Context.Companies
                    where x.HeadCount < 100
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" .} WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
    FILTER (?v0 < '100'^^<http://www.w3.org/2001/XMLSchema#integer>) . } } }");
        }

        [Test]
        public void TestBooleanConstants()
        {
            var q = from x in Context.Companies
                    where x.IsListed == true
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    FILTER (?v0 = true) . } } }");

            q = from x in Context.Companies
                    where x.IsListed == false
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    OPTIONAL { ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 . }
    FILTER (!bound(?v0) || (?v0 = false)) . } } }");
        }

        [Test]
        public void TestBooleanAsserts()
        {

            var q = from x in Context.Companies
                    where x.IsListed
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o .  {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    FILTER (?v0 = true) . } } }");

            q = from x in Context.Companies
                    where !x.IsListed
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT {?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    FILTER (!(?v0)) . } } }");

            q = from x in Context.Companies
                where x.IsListed && x.IsBlueChip
                select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    ?x <http://www.networkedplanet.com/schemas/test/isBlueChip> ?v1 .
    FILTER (?v0 && ?v1) . } } }");

            q = from x in Context.Companies
                where x.IsListed && !x.IsBlueChip
                select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    ?x <http://www.networkedplanet.com/schemas/test/isBlueChip> ?v1 .
    FILTER (?v0 && (!(?v1))) . } } }");

            q = from x in Context.Companies
                where !x.IsListed && x.IsBlueChip
                select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    ?x <http://www.networkedplanet.com/schemas/test/isBlueChip> ?v1 .
    FILTER ((!(?v0)) && ?v1) . } } }");

            q = from x in Context.Companies
                where !x.IsListed && !x.IsBlueChip
                select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" . } WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/isListed> ?v0 .
    ?x <http://www.networkedplanet.com/schemas/test/isBlueChip> ?v1 .
    FILTER ((!(?v0)) && (!(?v1))) . } } }");
        }

        [Test]
        public void TestLiteralInCollection()
        {
            var tickers = new string[] {"AAA", "AAB", "AAC", "AAD"};
            var q = from x in Context.Companies
                    where tickers.Contains(x.TickerSymbol)
                    select x;
            q.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" .} WHERE {
?x ?x_p ?x_o . {
    SELECT ?x WHERE {
    ?x a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x <http://www.networkedplanet.com/schemas/test/ticker> ?v0 .
    FILTER (?v0 IN ('AAA', 'AAB', 'AAC', 'AAD')) . } } }");
        }

        [Test]
        public void TestSelectProperty()
        {
            var q = from x in Context.Companies select x.Name;
            q.ToList();
            AssertQuerySparql(
                @"SELECT ?v0 WHERE { ?x a <http://www.networkedplanet.com/schemas/test/Company> .
?x <http://purl.org/dc/terms/title> ?v0 . }"
                );
        }

        [Test]
        public void TestCreateAnonymous()
        {
            var q = from x in Context.Companies select new {x.Name, x.TickerSymbol};
            q.ToList();
            AssertQuerySparql(
                @"SELECT ?v0 ?v1 WHERE {  ?x a <http://www.networkedplanet.com/schemas/test/Company> .
OPTIONAL { ?x <http://purl.org/dc/terms/title> ?v0 . }
OPTIONAL { ?x <http://www.networkedplanet.com/schemas/test/ticker> ?v1 . } }");

            var p = from x in Context.Companies select new {x.Name, x.TickerSymbol, Market=x.ListedOn.Name};
            p.ToList();
            AssertQuerySparql(
                @"SELECT ?v0 ?v1 ?v3 WHERE {  ?x a <http://www.networkedplanet.com/schemas/test/Company> .
OPTIONAL { ?x <http://purl.org/dc/terms/title> ?v0 . }
OPTIONAL { ?x <http://www.networkedplanet.com/schemas/test/ticker> ?v1 . }
OPTIONAL { ?v2 <http://www.networkedplanet.com/schemas/test/listing> ?x .
           ?v2 <http://purl.org/dc/terms/title> ?v3 . } }");
            Assert.AreEqual("v3", Context.LastSparqlLinqQueryContext.AnonymousMembersMap.Where(x=>x.Item1.Equals("Market")).Select(x=>x.Item2).FirstOrDefault());

            var r = from x in Context.Companies select new { x.Name, x.TickerSymbol, Market = x.ListedOn };
            r.ToList();
            AssertQuerySparql(
                @"SELECT ?v0 ?v1 ?v2 WHERE {  ?x a <http://www.networkedplanet.com/schemas/test/Company> .
OPTIONAL { ?x <http://purl.org/dc/terms/title> ?v0 . }
OPTIONAL { ?x <http://www.networkedplanet.com/schemas/test/ticker> ?v1 . }
OPTIONAL { ?v2 <http://www.networkedplanet.com/schemas/test/listing> ?x . } }");

        }

        [Test]
        public void TestAggregates()
        {
            var q = Context.Companies.Average(x => x.HeadCount);
            AssertQuerySparql(
                @"SELECT (AVG(?v0) AS ?v1) WHERE { 
?x a <http://www.networkedplanet.com/schemas/test/Company> .
?x <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
}");

            var count = Context.Companies.Count();
            AssertQuerySparql(
                @"SELECT (COUNT(?x003Cgeneratedx003Ex005Fx0031) AS ?v0) WHERE { ?x003Cgeneratedx003Ex005Fx0031 a <http://www.networkedplanet.com/schemas/test/Company> . }");

            var largeCompanyCount = Context.Companies.Where(x => x.HeadCount > 100).Count();
            AssertQuerySparql(
                @"SELECT (COUNT(?x) AS ?v1) WHERE { ?x a <http://www.networkedplanet.com/schemas/test/Company> . 
?x <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
FILTER(?v0 > '100'^^<http://www.w3.org/2001/XMLSchema#integer>). }");

            var largeCompanyHeadcount = Context.Companies.Where(x => x.HeadCount > 100).Average(x => x.HeadCount);
            AssertQuerySparql(
                @"SELECT (AVG(?v0) AS ?v1) WHERE {
?x a <http://www.networkedplanet.com/schemas/test/Company> .
?x <http://www.networkedplanet.com/schemas/test/headCount> ?v0 .
FILTER(?v0 > '100'^^<http://www.w3.org/2001/XMLSchema#integer>).
}");
        }

        [Test]
        public void TestSelectMany()
        {
            var q = Context.Dinners.SelectMany(d => d.Rsvps);
            q.ToList();
            AssertQuerySparql(
                              @"CONSTRUCT { ?x003Cgeneratedx003Ex005Fx0030 ?x003Cgeneratedx003Ex005Fx0030_p ?x003Cgeneratedx003Ex005Fx0030_o. 
?x003Cgeneratedx003Ex005Fx0030 <http://www.brightstardb.com/.well-known/model/selectVariable> ""x003Cgeneratedx003Ex005Fx0030"" .} WHERE {
?x003Cgeneratedx003Ex005Fx0030 ?x003Cgeneratedx003Ex005Fx0030_p ?x003Cgeneratedx003Ex005Fx0030_o . {
    SELECT ?x003Cgeneratedx003Ex005Fx0030 WHERE { 
    ?d a <http://www.networkedplanet.com/schemas/test/Dinner> . 
    ?x003Cgeneratedx003Ex005Fx0030 a <http://www.networkedplanet.com/schemas/test/Rsvp> . 
    ?d <http://www.networkedplanet.com/schemas/test/attendees> ?x003Cgeneratedx003Ex005Fx0030 . } } }");
        }

        [Test]
        public void TestSelectManyWithSubquery()
        {
        var q2 =
                Context.Dinners.SelectMany(d => d.Rsvps.Where(x => x.AttendeeEmail.Equals("kal@networkedplanet.com")));
            q2.ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x003Cgeneratedx003Ex005Fx0030 ?x003Cgeneratedx003Ex005Fx0030_p ?x003Cgeneratedx003Ex005Fx0030_o.
?x003Cgeneratedx003Ex005Fx0030 <http://www.brightstardb.com/.well-known/model/selectVariable> ""x003Cgeneratedx003Ex005Fx0030"" .} WHERE {
?x003Cgeneratedx003Ex005Fx0030 ?x003Cgeneratedx003Ex005Fx0030_p ?x003Cgeneratedx003Ex005Fx0030_o . {
    SELECT ?x003Cgeneratedx003Ex005Fx0030 WHERE { 
    ?d a <http://www.networkedplanet.com/schemas/test/Dinner> . 
    ?x003Cgeneratedx003Ex005Fx0030 a <http://www.networkedplanet.com/schemas/test/Rsvp> . 
    ?d <http://www.networkedplanet.com/schemas/test/attendees> ?x003Cgeneratedx003Ex005Fx0030 . 
    ?x003Cgeneratedx003Ex005Fx0030 <http://www.networkedplanet.com/schemas/test/email> ?v1 .
    FILTER (?v1 = 'kal@networkedplanet.com') .} } }");
        }

        [Test]
        public void TestOfType()
        {
            var q = Context.Companies.OfType<ContextObjects.IPerson>().ToList();
            AssertQuerySparql(
                @"CONSTRUCT { ?x003Cgeneratedx003Ex005Fx0031 ?x003Cgeneratedx003Ex005Fx0031_p ?x003Cgeneratedx003Ex005Fx0031_o.
?x003Cgeneratedx003Ex005Fx0031 <http://www.brightstardb.com/.well-known/model/selectVariable> ""x003Cgeneratedx003Ex005Fx0031"" .} WHERE {
?x003Cgeneratedx003Ex005Fx0031 ?x003Cgeneratedx003Ex005Fx0031_p ?x003Cgeneratedx003Ex005Fx0031_o . {
    SELECT ?x003Cgeneratedx003Ex005Fx0031 WHERE {
    ?x003Cgeneratedx003Ex005Fx0031 a <http://www.networkedplanet.com/schemas/test/Company> .
    ?x003Cgeneratedx003Ex005Fx0031 a <http://www.networkedplanet.com/schemas/test/Person> .
    } } }");
        }

        [Test]
        public void TestDistinct()
        {
            var q = Context.Dinners.Select(x => x.Host).Distinct();
            q.ToList();
            AssertQuerySparql(
                @"SELECT DISTINCT ?v0 WHERE { 
?x a <http://www.networkedplanet.com/schemas/test/Dinner> . 
?x <http://www.networkedplanet.com/schemas/test/host> ?v0 .
}");
        }

        [Test]
        public void TestGroupBy()
        {
            var q = Context.Dinners.GroupBy(x => x.Host);
            q.ToList();
            AssertQuerySparql(
                @"SELECT ?x WHERE {
?x a <http://www.networkedplanet.com/schemas/test/Dinner> . 
?x <http://www.networkedplanet.com/schemas/test/host> ?v0 .
} GROUP BY ?v0");
            var p = Context.Rsvps.GroupBy(x => x.Dinner.Host);
            p.ToList();
            AssertQuerySparql(

                              @"SELECT ?x WHERE {
?x a <http://www.networkedplanet.com/schemas/test/Rsvp> .
?v0 <http://www.networkedplanet.com/schemas/test/attendees> ?x .
?v0 <http://www.networkedplanet.com/schemas/test/host> ?v1 .
} GROUP BY ?v1");
        }

        [Test]
        public void TestCast()
        {
            var q = Context.Companies.Cast<IDinner>();
            q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x003Cgeneratedx003Ex005Fx0031 ?x003Cgeneratedx003Ex005Fx0031_p ?x003Cgeneratedx003Ex005Fx0031_o. 
?x003Cgeneratedx003Ex005Fx0031 <http://www.brightstardb.com/.well-known/model/selectVariable> ""x003Cgeneratedx003Ex005Fx0031"" . 
} WHERE {
?x003Cgeneratedx003Ex005Fx0031 ?x003Cgeneratedx003Ex005Fx0031_p ?x003Cgeneratedx003Ex005Fx0031_o . {
    SELECT ?x003Cgeneratedx003Ex005Fx0031 WHERE {
    ?x003Cgeneratedx003Ex005Fx0031 a <http://www.networkedplanet.com/schemas/test/Company> . } } }");

            try
            {
                var p = Context.Companies.Cast<string>();
                p.ToList();
                Assert.Fail("Expected EntityFrameworkException when attempting to cast to a non-EF type");
            }
            catch(EntityFrameworkException)
            {
                // Expected
            }
        }

        [Test]
        public void TestJoinWithAverage()
        {
            var q =
                (from c in Context.Companies
                 join m in Context.Markets on c.ListedOn.Id equals m.Id
                 select c)
                    .Average(c => c.CurrentMarketCap);
            AssertQuerySparql(
                @"SELECT (AVG(?v1) AS ?v2) WHERE {
?c a <http://www.networkedplanet.com/schemas/test/Company> .
?m a <http://www.networkedplanet.com/schemas/test/Market> .
?v0 <http://www.networkedplanet.com/schemas/test/listing> ?c .
FILTER(sameTerm(?m,?v0)) .
?c <http://www.networkedplanet.com/schemas/test/marketCap> ?v1 .
}");
        }

        [Test]
        public void TestAny()
        {
            var q = from d in Context.Dinners
                    where d.Rsvps.Any(r => r.AttendeeEmail.Equals("kal@networkedplanet.com"))
                    select d.Id;
            var result = q.ToList();
            AssertQuerySparql(@"SELECT ?v2 WHERE {
    ?d a <http://www.networkedplanet.com/schemas/test/Dinner> .
    FILTER EXISTS {
        ?d <http://www.networkedplanet.com/schemas/test/attendees> ?r .
        ?r <http://www.networkedplanet.com/schemas/test/email> ?v1 .
        FILTER (?v1 = 'kal@networkedplanet.com') .
    }
    BIND(STRAFTER(STR(?d), 'http://www.brightstardb.com/.well-known/genid/') AS ?v2)
}");

            var q2 = from m in Context.Markets
                     where m.ListedCompanies.Any(c => c.CurrentSharePrice > 10.0m)
                     select m.Id;
            var r2 = q2.ToList();
            AssertQuerySparql(@"SELECT ?v2 WHERE {
    ?m a <http://www.networkedplanet.com/schemas/test/Market> .
    FILTER EXISTS {
        ?m <http://www.networkedplanet.com/schemas/test/listing> ?c .
        ?c <http://www.networkedplanet.com/schemas/test/price> ?v1 .
        FILTER (?v1 > '10.00'^^<http://www.w3.org/2001/XMLSchema#decimal>) .
    } 
    BIND(STRAFTER(STR(?m), 'http://www.brightstardb.com/.well-known/genid/') AS ?v2)
}");
        }

        [Test]
        public void TestEagerLoadOrdered()
        {
            var q = Context.Dinners.OrderBy(x => x.EventDate);
            var result = q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. 
?x <http://www.brightstardb.com/.well-known/model/sortValue0> ?x_sort0 . 
?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"".
} WHERE {
    ?x ?x_p ?x_o .
    {
        SELECT ?x ?x_sort0 WHERE {
            ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
            ?x <http://www.networkedplanet.com/schemas/test/date> ?v0 .
            BIND (?v0 AS ?x_sort0).
        } ORDER BY ASC(?v0)
    } 
}");
        }

        [Test]
        public void TestEagerLoadDistinct()
        {
            var q = Context.Dinners.Distinct();
            var result = q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x003Cgeneratedx003Ex005Fx0031 ?x003Cgeneratedx003Ex005Fx0031_p ?x003Cgeneratedx003Ex005Fx0031_o.
?x003Cgeneratedx003Ex005Fx0031 <http://www.brightstardb.com/.well-known/model/selectVariable> ""x003Cgeneratedx003Ex005Fx0031"".
} WHERE {
  ?x003Cgeneratedx003Ex005Fx0031 ?x003Cgeneratedx003Ex005Fx0031_p ?x003Cgeneratedx003Ex005Fx0031_o .
  {
    SELECT DISTINCT ?x003Cgeneratedx003Ex005Fx0031 WHERE {
      ?x003Cgeneratedx003Ex005Fx0031 a <http://www.networkedplanet.com/schemas/test/Dinner> .
    }
  }
}");
        }

        [Test]
        public void TestEagerLoadOrderedDistinct()
        {
            var q = Context.Dinners.OrderBy(x => x.EventDate).Distinct();
            var result = q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o.
?x <http://www.brightstardb.com/.well-known/model/sortValue0> ?x_dsort0.
?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"".
} WHERE {
  ?x ?x_p ?x_o .
  {
    SELECT DISTINCT ?x (MAX(?x_sort0) AS ?x_dsort0) WHERE {
      ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
      ?x <http://www.networkedplanet.com/schemas/test/date> ?v0 .
      BIND (?v0 AS ?x_sort0).
    } 
    GROUP BY ?x 
    ORDER BY ASC(MAX(?x_sort0))
  }
}");
        }

        [Test]
        public void TestEagerLoadOrderedPaged()
        {
            var q = Context.Dinners.OrderBy(x => x.EventDate).Skip(10).Take(5);
            var result = q.ToList();
            AssertQuerySparql(@"CONSTRUCT { ?x ?x_p ?x_o. 
?x <http://www.brightstardb.com/.well-known/model/sortValue0> ?x_sort0 . 
?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"".
} WHERE {
    ?x ?x_p ?x_o .
    {
        SELECT ?x ?x_sort0 WHERE {
            ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
            ?x <http://www.networkedplanet.com/schemas/test/date> ?v0 .
            BIND (?v0 AS ?x_sort0).
        } ORDER BY ASC(?v0) LIMIT 5 OFFSET 10
    } 
}");
        }


    }

}
