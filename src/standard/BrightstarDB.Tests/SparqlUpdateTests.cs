using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class SparqlUpdateTests
    {
        private readonly IBrightstarService _client;


        public SparqlUpdateTests()
        {
            _client = BrightstarService.GetClient("type=embedded;storesDirectory=brightstar");
        }
        
        [Test]
        public void TestInsert()
        {
            var storeName = CreateStore("TestInsert");
            ExecuteUpdate(storeName, @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
INSERT DATA
{ 
  <http://example/book1> dc:title ""A new book"" ;
                         dc:creator ""A.N.Other"" .
}");

            var results = _client.ExecuteQuery(storeName,
    "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?o WHERE { <http://example/book1> dc:title ?o }");
            var resultsDoc = XDocument.Load(results);
            var resultRow = resultsDoc.SparqlResultRows().FirstOrDefault();
            Assert.IsNotNull(resultRow);
            Assert.AreEqual("A new book", resultRow.GetColumnValue("o").ToString());

            ExecuteUpdate(storeName,
                     @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
INSERT { ?a a <http://example.org/book> }
WHERE { ?a dc:title ""A new book"" }");
            results = _client.ExecuteQuery(storeName, "SELECT ?b WHERE {?b a <http://example.org/book>}");
            resultsDoc = XDocument.Load(results);
            resultRow = resultsDoc.SparqlResultRows().FirstOrDefault();
            Assert.IsNotNull(resultRow);
            Assert.AreEqual(new Uri("http://example/book1"), resultRow.GetColumnValue("b"));

        }

        [Test]
        public void TestDeleteData()
        {
            var storeName = CreateStore("TestDelete");
            ExecuteUpdate(storeName,
                          @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
PREFIX ns: <http://example.org/ns#> 
INSERT DATA {
<http://example/book2> ns:price 42 .
<http://example/book2> dc:title ""David Copperfield"" .
<http://example/book2> dc:creator ""Edmund Wells"" .
}");

            ExecuteUpdate(storeName, 
                @"PREFIX dc: <http://purl.org/dc/elements/1.1/>

DELETE DATA
{
  <http://example/book2> dc:title ""David Copperfield"" ;
                         dc:creator ""Edmund Wells"" .
}");
            var results = _client.ExecuteQuery(storeName, "SELECT ?o WHERE { <http://example/book2> ?p ?o . }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            var row = resultsDoc.SparqlResultRows().First();
            Assert.AreEqual(42, row.GetColumnValue("o"));
        }

        [Test]
        public void TestDeleteInsert()
        {
            var storeName = CreateStore("TestDeleteInsert");
            ExecuteUpdate(storeName, 
                @"PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
<http://example/president25> foaf:givenName ""Bill"" .
<http://example/president25> foaf:familyName ""McKinley"" .
<http://example/president27> foaf:givenName ""Bill"" .
<http://example/president27> foaf:familyName ""Taft"" .
<http://example/president42> foaf:givenName ""Bill"" .
<http://example/president42> foaf:familyName ""Clinton"" .
}");
            ExecuteUpdate(storeName, @"PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
DELETE { ?person foaf:givenName 'Bill' }
INSERT { ?person foaf:givenName 'William' }
WHERE
  { ?person foaf:givenName 'Bill'
  } ");
            var results = _client.ExecuteQuery(storeName,
                                               "PREFIX foaf: <http://xmlns.com/foaf/0.1/> SELECT ?fn WHERE { ?x foaf:givenName ?fn }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(3, resultsDoc.SparqlResultRows().Count());
            Assert.IsTrue(resultsDoc.SparqlResultRows().All(r=>r.GetColumnValue("fn").ToString().Equals("William")));
        }

        [Test]
        public void TestDelete()
        {
            var storeName = CreateStore("TestDelete");
            ExecuteUpdate(storeName,
                @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX ns: <http://example.org/ns#>
INSERT DATA {
<http://example/book1> dc:title ""Principles of Compiler Design"" .
<http://example/book1> dc:date ""1977-01-01T00:00:00-02:00""^^xsd:dateTime .

<http://example/book2> ns:price 42 .
<http://example/book2> dc:title ""David Copperfield"" .
<http://example/book2> dc:creator ""Edmund Wells"" .
<http://example/book2> dc:date ""1948-01-01T00:00:00-02:00""^^xsd:dateTime .

<http://example/book3> dc:title ""SPARQL 1.1 Tutorial"" .}");

            ExecuteUpdate(storeName, @"PREFIX dc:  <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

DELETE
 { ?book ?p ?v }
WHERE
 { ?book dc:date ?date .
   FILTER ( ?date > ""1970-01-01T00:00:00-02:00""^^xsd:dateTime )
   ?book ?p ?v
 }");
            var results = _client.ExecuteQuery(storeName,
                                               "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?b ?t WHERE { ?b dc:title ?t}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r=>r.GetColumnValue("b").Equals(new Uri("http://example/book2")) && r.GetColumnValue("t").ToString().Equals("David Copperfield")));
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book3")) && r.GetColumnValue("t").ToString().Equals("SPARQL 1.1 Tutorial")));

        }

        [Test]
        public void TestWildcardSubjetDelete()
        {
            var storeName = CreateStore("TestWildcardSubjectDelete");
            ExecuteUpdate(storeName,
                @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX ns: <http://example.org/ns#>
INSERT DATA {
<http://example/book1> dc:title ""Principles of Compiler Design"" .
<http://example/book1> dc:date ""1977-01-01T00:00:00-02:00""^^xsd:dateTime .

<http://example/book2> ns:price 42 .
<http://example/book2> dc:title ""David Copperfield"" .
<http://example/book2> dc:creator ""Edmund Wells"" .
<http://example/book2> dc:date ""1948-01-01T00:00:00-02:00""^^xsd:dateTime .

<http://example/book3> dc:title ""SPARQL 1.1 Tutorial"" .}");

            ExecuteUpdate(storeName, @"PREFIX dc:  <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
DELETE DATA
 { <http://www.brightstardb.com/.well-known/model/wildcard> dc:title ""David Copperfield"" }");
            var results = _client.ExecuteQuery(storeName,
                                               "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?b ?t WHERE { ?b dc:title ?t}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book1")) && r.GetColumnValue("t").ToString().Equals("Principles of Compiler Design")));
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book3")) && r.GetColumnValue("t").ToString().Equals("SPARQL 1.1 Tutorial")));
        }

        private void LoadBooks(string storeName)
        {
            ExecuteUpdate(storeName,
    @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX ns: <http://example.org/ns#>
INSERT DATA {
<http://example/book1> dc:title ""Principles of Compiler Design"" .
<http://example/book1> dc:date ""1977-01-01T00:00:00-02:00""^^xsd:dateTime .

<http://example/book2> ns:price 42 .
<http://example/book2> dc:title ""David Copperfield"" .
<http://example/book2> dc:creator ""Edmund Wells"" .
<http://example/book2> dc:date ""1948-01-01T00:00:00-02:00""^^xsd:dateTime .

<http://example/book3> dc:title ""SPARQL 1.1 Tutorial"" .}");
        }

        [Test]
        public void TestWildcardPredicateDelete()
        {
            var storeName = CreateStore("TestWildcardSubjectDelete");
            LoadBooks(storeName);

            ExecuteUpdate(storeName, @"PREFIX dc:  <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
DELETE DATA
 { <http://example/book2> <http://www.brightstardb.com/.well-known/model/wildcard> ""David Copperfield"" }");
            var results = _client.ExecuteQuery(storeName,
                                               "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?b ?t WHERE { ?b dc:title ?t}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book1")) && r.GetColumnValue("t").ToString().Equals("Principles of Compiler Design")));
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book3")) && r.GetColumnValue("t").ToString().Equals("SPARQL 1.1 Tutorial")));
        }

        [Test]
        public void TestWildcardObjectDelete()
        {
            var storeName = CreateStore("TestWildcardSubjectDelete");
            LoadBooks(storeName);

            ExecuteUpdate(storeName, @"PREFIX dc:  <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
DELETE DATA
 { <http://example/book2> dc:title <http://www.brightstardb.com/.well-known/model/wildcard> }");
            var results = _client.ExecuteQuery(storeName,
                                               "PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?b ?t WHERE { ?b dc:title ?t}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book1")) && r.GetColumnValue("t").ToString().Equals("Principles of Compiler Design")));
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("b").Equals(new Uri("http://example/book3")) && r.GetColumnValue("t").ToString().Equals("SPARQL 1.1 Tutorial")));
        }

        [Test]
        public void TestDeleteFromGraph()
        {
            var storeName = CreateStore("TestDeleteFromGraph");
            ExecuteUpdate(storeName,
                                  @"PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  GRAPH <http://example/addresses> {
    <http://example/william> a foaf:Person .
    <http://example/william> foaf:givenName ""William"" .
    <http://example/william> foaf:mbox <mailto:bill@example> .

    <http://example/fred> a foaf:Person .
    <http://example/fred> foaf:givenName ""Fred"" .
    <http://example/fred> foaf:mbox  <mailto:fred@example> .
  }
}");
            var results = _client.ExecuteQuery(storeName, "PREFIX foaf:  <http://xmlns.com/foaf/0.1/> SELECT ?p FROM <http://example/addresses> WHERE { ?p a foaf:Person }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());

            results = _client.ExecuteQuery(storeName,
                                           "PREFIX foaf:  <http://xmlns.com/foaf/0.1/> SELECT ?person ?property ?value FROM <http://example/addresses> WHERE { ?person ?property ?value ; foaf:givenName 'Fred' } ");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(3, resultsDoc.SparqlResultRows().Count());

            ExecuteUpdate(storeName,
                                  @"PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
WITH <http://example/addresses>
DELETE { ?person ?property ?value } 
WHERE { ?person ?property ?value ; foaf:givenName 'Fred' } ");

            results = _client.ExecuteQuery(storeName, "PREFIX foaf:  <http://xmlns.com/foaf/0.1/> SELECT ?p FROM <http://example/addresses> WHERE { ?p a foaf:Person }");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            var row = resultsDoc.SparqlResultRows().First();
            Assert.AreEqual(new Uri("http://example/william"), row.GetColumnValue("p"));
        }

        [Test]
        public void TestGraphCopy()
        {
            var storeName = CreateStore("TestGraphCopy");
            ExecuteUpdate(storeName,
                          @"
PREFIX dc: <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX ns: <http://example.org/ns#>
INSERT DATA {
  GRAPH <http://example/bookStore> {
    <http://example/book1> dc:title ""Fundamentals of Compiler Design"" .
    <http://example/book1> dc:date ""1977-01-01T00:00:00-02:00""^^xsd:dateTime .

    <http://example/book2> ns:price 42 .
    <http://example/book2> dc:title ""David Copperfield"" .
    <http://example/book2> dc:creator ""Edmund Wells"" .
    <http://example/book2> dc:date ""1948-01-01T00:00:00-02:00""^^xsd:dateTime .

    <http://example/book3> dc:title ""SPARQL 1.1 Tutorial"" .
  }
}");
            ExecuteUpdate(storeName, @"
PREFIX dc: <http://purl.org/dc/elements/1.1/>
INSERT DATA {
  GRAPH <http://example/bookStore2> {
    <http://example/book4> dc:title ""SPARQL 1.0 Tutorial"" .
  }
}");

            var results = _client.ExecuteQuery(storeName,
                                 @"PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?t FROM <http://example/bookStore2> WHERE { ?b dc:title ?t }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());

            ExecuteUpdate(storeName,
                                  @"PREFIX dc:  <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

INSERT 
  { GRAPH <http://example/bookStore2> { ?book ?p ?v } }
WHERE
  { GRAPH  <http://example/bookStore>
       { ?book dc:date ?date .
         FILTER ( ?date > ""1970-01-01T00:00:00-02:00""^^xsd:dateTime )
         ?book ?p ?v
  } }	");

            results = _client.ExecuteQuery(storeName,
                @"PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?t FROM <http://example/bookStore2> WHERE { ?b dc:title ?t }");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());

            results = _client.ExecuteQuery(storeName,
    @"PREFIX dc: <http://purl.org/dc/elements/1.1/> SELECT ?d FROM <http://example/bookStore2> WHERE { ?b dc:date ?d }");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());

        }

        [Test]
        public void TestGraphManagement()
        {
            var sid = CreateStore("GraphManagement");

            ExecuteUpdate(sid, @"PREFIX dc: <http://purl.org/dc/elements/1.1/>
INSERT DATA
{ 
  <http://example/book1> dc:title ""A new book"" ;
                         dc:creator ""A.N.Other"" .
}");

            ExecuteUpdate(sid, "CREATE GRAPH <http://np.com/g1>");

            ExecuteUpdate(sid, "CREATE SILENT GRAPH <http://np.com/g1>");

            ExecuteUpdate(sid, "DROP GRAPH <" + Constants.DefaultGraphUri + ">");

            var results = _client.ExecuteQuery(sid, "SELECT ?s ?p ?o WHERE { ?s ?p ?o}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(0, resultsDoc.SparqlResultRows().Count());
        }

        [Test]
        public void TestGraphLoad()
        {
#if !WINDOWS_PHONE && !PORTABLE
            var storeName = CreateStore("TestGraphLoad");
            var importFile =
                new FileInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, Configuration.DataLocation,
                    "simple.txt"));
            Assert.IsTrue(importFile.Exists);
            var importUri = new Uri(importFile.FullName).ToString().Replace(" ", "%20");

            // Test import into default graph
            ExecuteUpdate(storeName, String.Format("LOAD <{0}>", importUri));

            var results = _client.ExecuteQuery(storeName,
                                 "SELECT ?p WHERE { <http://example.org/resource15> <http://example.org/property> ?a . ?a <http://example.org/property> ?p . }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            Assert.AreEqual(new Uri("http://example.org/resource2"), resultsDoc.SparqlResultRows().First().GetColumnValue("p"));

            // Test import into named graph
            ExecuteUpdate(storeName, String.Format("LOAD <{0}> INTO GRAPH <http://example.org/graph1>", importUri));
            results = _client.ExecuteQuery(storeName,
                                 "SELECT ?p FROM <http://example.org/graph1> WHERE { <http://example.org/resource15> <http://example.org/property> ?a . ?a <http://example.org/property> ?p . }");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            Assert.AreEqual(new Uri("http://example.org/resource2"), resultsDoc.SparqlResultRows().First().GetColumnValue("p"));
#endif
        }

        [Test]
        public void TestGraphClear()
        {
            var storeName = CreateStore("TestGraphCopy");
            ExecuteUpdate(storeName,
                          @"
PREFIX dc: <http://purl.org/dc/elements/1.1/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
PREFIX ns: <http://example.org/ns#>
INSERT DATA {
  GRAPH <http://example/bookStore> {
    <http://example/book1> dc:title ""Fundamentals of Compiler Design"" .
    <http://example/book1> dc:date ""1977-01-01T00:00:00-02:00""^^xsd:dateTime .

    <http://example/book2> ns:price 42 .
    <http://example/book2> dc:title ""David Copperfield"" .
    <http://example/book2> dc:creator ""Edmund Wells"" .
    <http://example/book2> dc:date ""1948-01-01T00:00:00-02:00""^^xsd:dateTime .

    <http://example/book3> dc:title ""SPARQL 1.1 Tutorial"" .
  }
}");

            var results = _client.ExecuteQuery(storeName,
                                   "SELECT ?s ?p ?o FROM <http://example/bookStore> WHERE { ?s ?p ?o }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(7, resultsDoc.SparqlResultRows().Count());


            ExecuteUpdate(storeName,"CLEAR GRAPH <http://example/bookStore>");

            results = _client.ExecuteQuery(storeName,
                                               "SELECT ?s ?p ?o FROM <http://example/bookStore> WHERE { ?s ?p ?o }");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(0, resultsDoc.SparqlResultRows().Count());
        }

        [Test]
        public void TestGraphCopyCmd()
        {
            var storeName = CreateStore("TestGraphCopyCmd");
            ExecuteUpdate(storeName, @"
PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  <http://example/william> a foaf:Person .
  <http://example/william> foaf:givenName ""William"" .
  <http://example/william> foaf:mbox  <mailto:bill@example> .
}");
            ExecuteUpdate(storeName, @"
PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  GRAPH <http://example.org/named> {
    <http://example/fred> a foaf:Person .
    <http://example/fred> foaf:givenName ""Fred"" .
  }
}
");
            ExecuteUpdate(storeName, "COPY DEFAULT TO <http://example.org/named>");

            var results = _client.ExecuteQuery(storeName,
                                               "SELECT ?s ?p ?o FROM <http://example.org/named> WHERE { ?s ?p ?o }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(3, resultsDoc.SparqlResultRows().Count());
        }


        [Test]
        public void TestGraphMove()
        {
            var storeName = CreateStore("TestGraphCopyCmd");
            ExecuteUpdate(storeName, @"
PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  <http://example/william> a foaf:Person .
  <http://example/william> foaf:givenName ""William"" .
  <http://example/william> foaf:mbox  <mailto:bill@example> .
}");
            ExecuteUpdate(storeName, @"
PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  GRAPH <http://example.org/named> {
    <http://example/fred> a foaf:Person .
    <http://example/fred> foaf:givenName ""Fred"" .
  }
}
");
            ExecuteUpdate(storeName, "MOVE DEFAULT TO <http://example.org/named>");

            var results = _client.ExecuteQuery(storeName,
                                               "SELECT ?s ?p ?o FROM <http://example.org/named> WHERE { ?s ?p ?o }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(3, resultsDoc.SparqlResultRows().Count());
            results = _client.ExecuteQuery(storeName, "SELECT ?s ?p ?o WHERE { ?s ?p ?o }");
            resultsDoc = XDocument.Load(results);
            Assert.AreEqual(0, resultsDoc.SparqlResultRows().Count());
        }

        [Test]
        public void TestGraphAdd()
        {
            var storeName = CreateStore("TestGraphCopyCmd");
            ExecuteUpdate(storeName, @"
PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  <http://example/william> a foaf:Person .
  <http://example/william> foaf:givenName ""William"" .
  <http://example/william> foaf:mbox  <mailto:bill@example> .
}");
            ExecuteUpdate(storeName, @"
PREFIX foaf:  <http://xmlns.com/foaf/0.1/>
INSERT DATA {
  GRAPH <http://example.org/named> {
    <http://example/fred> a foaf:Person .
    <http://example/fred> foaf:givenName ""Fred"" .
  }
}
");
            ExecuteUpdate(storeName, "ADD DEFAULT TO <http://example.org/named>");

            var results = _client.ExecuteQuery(storeName,
                                               "SELECT ?s ?p ?o FROM <http://example.org/named> WHERE { ?s ?p ?o }");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(5, resultsDoc.SparqlResultRows().Count());
        }


        private string CreateStore(string prefix)
        {
            var storeName = prefix + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
            _client.CreateStore(storeName);
            return storeName;
        }

        private void ExecuteUpdate(string storeName, string updateExpression)
        {
            IJobInfo jobInfo = _client.ExecuteUpdate(storeName, updateExpression);
            TestHelper.AssertJobCompletesSuccessfully(_client, storeName, jobInfo);
        }
    }
}
