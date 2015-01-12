using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using BrightstarDB.Client;
using BrightstarDB.Rdf;
using NUnit.Framework;

namespace BrightstarDB.Server.IntegrationTests
{
    [TestFixture]
    public class GraphStoreProtocolTests : ClientTestBase
    {
        private readonly string _storeNameSuffix = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        private string _noNamedGraphStore;
        private string _twoNamedGraphStore;

        private const string InsertTriples = @"<http://example.org/s> <http://example.org/p> <http://example.org/o> .";
        private const string NewTriples = @"<http://example.org/s> <http://example.org/p> ""o"" .";
        private const string InvalidTriples = @"<http://example.org/s> <http://example.org/p> ""o"""; // No final period

        private static IBrightstarService GetClient()
        {
            return BrightstarService.GetClient("type=rest;endpoint=http://localhost:8090/brightstar");
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            StartService();
            var client = GetClient();
            _noNamedGraphStore = "GraphStoreProtocolTests_NoNamedGraphs_" + _storeNameSuffix; 
            client.CreateStore(_noNamedGraphStore);
            var jobInfo = client.ExecuteTransaction(_noNamedGraphStore, new UpdateTransactionData {InsertData = InsertTriples});
            Assert.That(jobInfo.JobCompletedOk);
            _twoNamedGraphStore = "GraphStoreProtocolTests_TwoNamedGraphs_" + _storeNameSuffix;
            client.CreateStore(_twoNamedGraphStore);
            jobInfo = client.ExecuteTransaction(_twoNamedGraphStore,
                new UpdateTransactionData {DefaultGraphUri = "http://example.org/g1", InsertData = InsertTriples});
            Assert.That(jobInfo.JobCompletedOk);
            jobInfo = client.ExecuteTransaction(_twoNamedGraphStore,
                new UpdateTransactionData { DefaultGraphUri = "http://example.org/g2", InsertData = InsertTriples });
            Assert.That(jobInfo.JobCompletedOk);

        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            CloseService();
        }

        [Test]
        public void TestListGraphs()
        {
            var request = WebRequest.CreateHttp(new Uri(GetStoreUri(_twoNamedGraphStore) + "/graphs"));
            request.Accept = SparqlResultsFormat.Xml.MediaTypes[0];
            var response = request.GetResponse() as HttpWebResponse;
            Assert.NotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var results = XDocument.Load(GetResponseReader(response));
            CollectionAssert.Contains(results.GetVariableNames(), "graphUri");
            var rows = results.SparqlResultRows().ToList();
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows.Select(x=>x.GetColumnValue("graphUri").ToString()).All(x=>x.Equals("http://example.org/g1") || x.Equals("http://example.org/g2")));
        }

        [Test]
        public void TestReplaceGraphContent()
        {
            var request = WebRequest.CreateHttp(new Uri(GetStoreUri(_twoNamedGraphStore) + "/graphs?graph=http://example.org/g1"));
            request.Method = "PUT";
            request.ContentType = "text/ntriples; charset=utf-8";
            SetRequestBody(request, NewTriples);
            var response = request.GetResponse() as HttpWebResponse;
            Assert.NotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            var resultStream = GetClient()
                .ExecuteQuery(_twoNamedGraphStore, "SELECT ?o WHERE { GRAPH <http://example.org/g1> { ?s ?p ?o } }");
            var resultDoc = XDocument.Load(resultStream);
            var rows = resultDoc.SparqlResultRows().ToList();
            Assert.That(rows.Count, Is.EqualTo(1));
            var v = rows[0].GetColumnValue("o");
            Assert.That(v, Is.Not.Null);
            Assert.That(v, Is.InstanceOf<PlainLiteral>());
            Assert.That(v, Has.Property("Value").EqualTo("o"));
        }

        [Test]
        public void TestPutWithInvalidContentReturns400()
        {
            var request = WebRequest.CreateHttp(new Uri(GetStoreUri(_twoNamedGraphStore) + "/graphs?graph=http://example.org/g1"));
            request.Method = "PUT";
            request.ContentType = "text/ntriples; charset=utf-8";
            SetRequestBody(request, InvalidTriples);
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                Assert.Fail("Expected a WebException to be raised");
            }
            catch (WebException wex)
            {
                var webResponse = wex.Response as HttpWebResponse;
                Assert.NotNull(webResponse);
                Assert.That(webResponse.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public void TestPutNewGraph()
        {
            var client = GetClient();
            var storeName = "GraphStoreProtocolTests_TestPutNewGraph_" + _storeNameSuffix;
            client.CreateStore(storeName);

            var request = WebRequest.CreateHttp(new Uri(GetStoreUri(storeName) + "/graphs?graph=http://example.org/g1"));
            request.Method = "PUT";
            request.ContentType = "text/ntriples; charset=utf-8";
            SetRequestBody(request, NewTriples);
            var response = request.GetResponse() as HttpWebResponse;
            Assert.NotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var resultStream = GetClient()
                .ExecuteQuery(storeName, "SELECT ?o WHERE { GRAPH <http://example.org/g1> { ?s ?p ?o } }");
            var resultDoc = XDocument.Load(resultStream);
            var rows = resultDoc.SparqlResultRows().ToList();
            Assert.That(rows.Count, Is.EqualTo(1));
            var v = rows[0].GetColumnValue("o");
            Assert.That(v, Is.Not.Null);
            Assert.That(v, Is.InstanceOf<PlainLiteral>());
            Assert.That(v, Has.Property("Value").EqualTo("o"));
        }

        [Test]
        public void TestDeleteGraph()
        {
            var client = GetClient();
            var storeName = "GraphStoreProtocolTests_TestDeleteGraph_" + _storeNameSuffix;
            client.CreateStore(storeName);
            var jobInfo = client.ExecuteTransaction(storeName,
                new UpdateTransactionData {DefaultGraphUri = "http://example.org/g1", InsertData = InsertTriples});
            Assert.That(jobInfo.JobCompletedOk);

            var request = WebRequest.CreateHttp(GetStoreUri(storeName) + "/graphs?graph=http://example.org/g1");
            request.Method = "DELETE";
            var response = request.GetResponse() as HttpWebResponse;
            Assert.NotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            var graphList = client.ListNamedGraphs(storeName).ToList();
            Assert.That(graphList.Count, Is.EqualTo(0));

        }

        [Test]
        public void TestDeleteNoExistentGraphReturns404()
        {
            var client = GetClient();
            var storeName = "GraphStoreProtocolTests_TestDeleteNonExistentGraph_" + _storeNameSuffix;
            client.CreateStore(storeName);

            var request = WebRequest.CreateHttp(GetStoreUri(storeName) + "/graphs?graph=http://example.org/g1");
            request.Method = "DELETE";
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
            }
            catch (WebException wex)
            {
                var response = wex.Response as HttpWebResponse;
                Assert.NotNull(response);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            }
        }

        [Test]
        public void TestPostMergesGraphContent()
        {
            var client = GetClient();
            var storeName = "GraphStoreProtocolTests_TestPostMergesGraphContent_" + _storeNameSuffix;
            client.CreateStore(storeName);
            client.ExecuteTransaction(storeName,
                new UpdateTransactionData {DefaultGraphUri = "http://example.org/g1", InsertData = InsertTriples});

            var request = WebRequest.CreateHttp(GetStoreUri(storeName) + "/graphs?graph=http://example.org/g1");
            request.Method = "POST";
            request.ContentType = "text/ntriples; charset=utf-8";
            SetRequestBody(request, NewTriples);

            var response = request.GetResponse() as HttpWebResponse;
            Assert.NotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            AssertTriplePatternInGraph(client, storeName, InsertTriples, "http://example.org/g1");
            AssertTriplePatternInGraph(client, storeName, NewTriples, "http://example.org/g1");
        }

        [Test]
        public void TestPostMergesDefaultGraphContent()
        {
            var client = GetClient();
            var storeName = "GraphStoreProtocolTests_TestPostMergesDefaultGraphContent_" + _storeNameSuffix;
            client.CreateStore(storeName);
            client.ExecuteTransaction(storeName,
                new UpdateTransactionData { InsertData = InsertTriples });

            var request = WebRequest.CreateHttp(GetStoreUri(storeName) + "/graphs?default");
            request.Method = "POST";
            request.ContentType = "text/ntriples; charset=utf-8";
            SetRequestBody(request, NewTriples);

            var response = request.GetResponse() as HttpWebResponse;
            Assert.NotNull(response);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            AssertTriplePatternInDefaultGraph(client, storeName, InsertTriples);
            AssertTriplePatternInDefaultGraph(client, storeName, NewTriples);
        }


        private static TextReader GetResponseReader(HttpWebResponse response)
        {
            var responseStream = response.GetResponseStream();
            if (responseStream == null) return null;
            var charset = response.CharacterSet;
            var encoding = String.IsNullOrEmpty(charset) ? Encoding.GetEncoding("iso-8859-1") : Encoding.GetEncoding(charset);
            return new StreamReader(responseStream, encoding);
        }

        private static void SetRequestBody(WebRequest request, string requestBody)
        {
            var requestEncoding = Encoding.GetEncoding("utf-8");
            using (var writer = new StreamWriter(request.GetRequestStream(), requestEncoding))
            {
                writer.Write(requestBody);
                writer.Flush();
            }
        }
    }
}
