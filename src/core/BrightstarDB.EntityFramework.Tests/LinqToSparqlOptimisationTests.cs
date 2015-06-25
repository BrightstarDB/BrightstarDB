using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace BrightstarDB.EntityFramework.Tests
{
    [TestFixture]
    public class LinqToSparqlOptimisationTests : LinqToSparqlTestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeContext();
        }


        [Test]
        public void TestGetDinnerByStringProperty()
        {
            var q = from x in Context.Dinners where x.Title == "Test" select x;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.AreEqual(
                NormalizeSparql(@"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" .} 
WHERE {
  ?x ?x_p ?x_o . {
    SELECT ?x WHERE {
      ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
      { ?x <http://purl.org/dc/terms/title> 'Test' . } 
    }
  }
}"),
                 NormalizeSparql(lastSparql));
        }

        [Test]
        public void TestOptimisationOfMultiHopPropertyFilter()
        {
            var q = from x in Context.Dinners
                from r in x.Rsvps
                where r.AttendeeEmail == "kal@networkedplanet.com"
                select x;
            var results = q.ToList();
            var lastSparql = Context.LastSparqlQuery;
            Assert.AreEqual(
                NormalizeSparql(
                @"CONSTRUCT { ?x ?x_p ?x_o. ?x <http://www.brightstardb.com/.well-known/model/selectVariable> ""x"" .}
WHERE {
  ?x ?x_p ?x_o . {
    SELECT ?x WHERE {
      ?x a <http://www.networkedplanet.com/schemas/test/Dinner> .
      ?r a <http://www.networkedplanet.com/schemas/test/Rsvp> .
      ?x <http://www.networkedplanet.com/schemas/test/attendees> ?r .
      { ?r <http://www.networkedplanet.com/schemas/test/email> 'kal@networkedplanet.com' . }
    } } }"),
                NormalizeSparql(lastSparql));
        }


    }
}
