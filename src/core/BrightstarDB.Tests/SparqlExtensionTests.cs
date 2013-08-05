using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using BrightstarDB.Client;
using NUnit.Framework;

namespace BrightstarDB.Tests
{
    [TestFixture]
    public class SparqlExtensionTests
    {
        private IBrightstarService _client;

        private string _testData =
            @"<http://example.org/x> <http://example.org/p1> ""1""^^<http://www.w3.org/2001/XMLSchema#int> .
<http://example.org/x> <http://example.org/p2> ""2""^^<http://www.w3.org/2001/XMLSchema#int> .
<http://example.org/y> <http://example.org/p1> ""1""^^<http://www.w3.org/2001/XMLSchema#int> .
<http://example.org/y> <http://example.org/p2> ""3""^^<http://www.w3.org/2001/XMLSchema#int> .";

        public SparqlExtensionTests()
        {
#if WINDOWS_PHONE
            _client = BrightstarService.GetEmbeddedClient("brightstar");
#else
            _client = BrightstarService.GetClient("type=embedded;storesDirectory=c:\\brightstar");
#endif
        }

        [Test]
        public void TestBitwiseAnd()
        {
            var storeName = "TestBitwiseAnd_" + DateTime.Now.Ticks;
            _client.CreateStore(storeName);
            var job = _client.ExecuteTransaction(storeName, null, null, _testData);
            TestHelper.AssertJobCompletesSuccessfully(_client, storeName, job);

            var results = _client.ExecuteQuery(storeName,
                                 @"PREFIX bsfunc: <http://brightstardb.com/.well-known/sparql/functions/>
SELECT ?s WHERE { 
  ?s <http://example.org/p1> ?p1 ;
     <http://example.org/p2> ?p2 . 
  FILTER (bsfunc:bit_and(?p1, ?p2) = 1)
}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(1, resultsDoc.SparqlResultRows().Count());
            var row = resultsDoc.SparqlResultRows().First();
            Assert.AreEqual("http://example.org/y", row.GetColumnValue("s").ToString());
        }

        [Test]
        public void TestBitwiseOr()
        {
            var storeName = "TestBitwiseOr_" + DateTime.Now.Ticks;
            _client.CreateStore(storeName);
            var job = _client.ExecuteTransaction(storeName, null, null, _testData);
            TestHelper.AssertJobCompletesSuccessfully(_client, storeName, job);

            var results = _client.ExecuteQuery(storeName,
                                               @"PREFIX bsfunc: <http://brightstardb.com/.well-known/sparql/functions/>
SELECT ?s WHERE {
    ?s <http://example.org/p1> ?p1 ;
       <http://example.org/p2> ?p2 .
    FILTER (bsfunc:bit_or(?p1, ?p2) = 3)
}");
            var resultsDoc = XDocument.Load(results);
            Assert.AreEqual(2, resultsDoc.SparqlResultRows().Count());
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r=>r.GetColumnValue("s").ToString().Equals("http://example.org/x")));
            Assert.IsTrue(resultsDoc.SparqlResultRows().Any(r => r.GetColumnValue("s").ToString().Equals("http://example.org/y")));
        }
    }
}
