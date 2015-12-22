using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using BrightstarDB.Client;
using Moq;
using Nancy;
using Nancy.ModelBinding.DefaultBodyDeserializers;
using Nancy.Responses.Negotiation;
using NUnit.Framework;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class GraphsUrlSpec
    {
        private static readonly MediaRange RdfXml = new MediaRange(RdfFormat.RdfXml.MediaTypes[0]);
        private static readonly MediaRange SparqlXml = new MediaRange(SparqlResultsFormat.Xml.MediaTypes[0]);

        [Test]
        public void TestListGraphs()
        {
            var brightstar = new Mock<IBrightstarService>();
            IEnumerable<string> graphs = new[] {"http://example.org/g1", "http://example.org/g2"};
            brightstar.Setup(s=>s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s => s.ListNamedGraphs("foo")).Returns(graphs).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with => with.Accept(SparqlXml));

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseBody = response.Body.AsString();
            var xmlDoc = XDocument.Parse(responseBody);
            CollectionAssert.Contains(xmlDoc.GetVariableNames(), "graphUri");
            var rows = xmlDoc.SparqlResultRows().ToList();
            Assert.That(rows[0].GetColumnValue("graphUri"), Is.EqualTo(new Uri("http://example.org/g1")));
            Assert.That(rows[1].GetColumnValue("graphUri"), Is.EqualTo(new Uri("http://example.org/g2")));
        }

        [Test]
        public void TestListGraphsAsJson()
        {
            var brightstar = new Mock<IBrightstarService>();
            IEnumerable<string> graphs = new[] { "http://example.org/g1", "http://example.org/g2" };
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s => s.ListNamedGraphs("foo")).Returns(graphs).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with => with.Accept("application/json"));

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var jobs = response.Body.DeserializeJson<List<string>>();
            Assert.That(jobs, Is.Not.Null);
            Assert.That(jobs.Count, Is.EqualTo(2));
            Assert.That(jobs, Contains.Item("http://example.org/g1"));
            Assert.That(jobs, Contains.Item("http://example.org/g2"));

        }

        [Test]
        public void TestGetNamedGraph()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockResultsStream = new MemoryStream(Encoding.UTF8.GetBytes("Mock Results"));
            ISerializationFormat expectedFormat = RdfFormat.RdfXml;
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(
                s =>
                    s.ExecuteQuery("foo", "CONSTRUCT { ?s ?p ?o } WHERE { GRAPH <http://example.org/g> { ?s ?p ?o } }",
                        It.IsAny<IEnumerable<string>>(), null, It.IsAny<SparqlResultsFormat>(), RdfFormat.RdfXml, out expectedFormat)).Returns(mockResultsStream).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with =>
            {
                with.Query("graph", "http://example.org/g");
                with.Accept(RdfXml);
            });

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseBody = response.Body.AsString();
            Assert.That(responseBody, Is.EqualTo("Mock Results"));
            brightstar.Verify();
        }

        [Test]
        public void TestInvalidStoreNameReturnsNotFound()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false).Verifiable();

            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with =>
            {
                with.Query("graph", "http://example.org/g");
                with.Accept(RdfXml);
            });

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

        }

        [Test]
        public void TestRelativeGraphUriReturnsBadRequest()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();

            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with =>
            {
                with.Query("graph", "g");
                with.Accept(RdfXml);
            });

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void TestRetrieveDefaultGraph()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockResultsStream = new MemoryStream(Encoding.UTF8.GetBytes("Mock Results"));
            ISerializationFormat expectedFormat = RdfFormat.RdfXml;
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(
                s =>
                    s.ExecuteQuery("foo", "CONSTRUCT { ?s ?p ?o } WHERE { ?s ?p ?o }",
                        It.Is<IEnumerable<string>>(x=>x.First().Equals(Constants.DefaultGraphUri)), null, It.IsAny<SparqlResultsFormat>(), RdfFormat.RdfXml, out expectedFormat)).Returns(mockResultsStream).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with =>
            {
                with.Query("default", "");
                with.Accept(RdfXml);
            });

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseBody = response.Body.AsString();
            Assert.That(responseBody, Is.EqualTo("Mock Results"));
            brightstar.Verify();
        }

        [Test]
        public void TestIfModifiedSinceHeaderRespected()
        {
            ISerializationFormat expectedFormat = RdfFormat.RdfXml;
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(
                s =>
                    s.ExecuteQuery("foo", "CONSTRUCT { ?s ?p ?o } WHERE { ?s ?p ?o }",
                        It.Is<IEnumerable<string>>(x => x.First().Equals(Constants.DefaultGraphUri)),
                        It.IsNotNull<DateTime?>(), It.IsAny<SparqlResultsFormat>(), RdfFormat.RdfXml, out expectedFormat))
                .Throws<BrightstarStoreNotModifiedException>()
                .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Get("/foo/graphs", with =>
            {
                with.Query("default", "");
                with.Accept(RdfXml);
                with.Header("If-Modified-Since", DateTime.UtcNow.ToString("r"));
            });

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
        }

        private bool SpaceNormalizedStringsAreEqual(string expected, string actual)
        {
            var wsregex = new Regex(@"\s+");
            var check = wsregex.Replace(expected, " ").Equals(wsregex.Replace(actual, " "));
            if (!check)
            {
                // Output for debugging purposes
                Console.WriteLine("Strings do not match when space-normalized. Expected: '{0}'. Actual: '{1}'.", expected, actual);
            }
            return check;
        }

        [Test]
        public void TestPutUpdatesNamedGraph()
        {
            IEnumerable<string> existingGraphs = new[] {"http://example.org/g"};
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(m => m.JobCompletedOk).Returns(true);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            brightstar.Setup(s => s.ListNamedGraphs("foo")).Returns(existingGraphs);
            brightstar.Setup(s => s.ExecuteUpdate("foo",
                It.Is<string>(p=>SpaceNormalizedStringsAreEqual(@"DROP SILENT GRAPH <http://example.org/g>; INSERT DATA { GRAPH <http://example.org/g> { <http://example.org/s> <http://example.org/p> <http://example.org/o> . } }",p)),
                true, It.IsAny<string>())).Returns(mockJobInfo.Object).Verifiable();
                  
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Put("/foo/graphs", with =>
            {
                with.Query("graph", "http://example.org/g");
                with.Header("Content-Type", RdfFormat.NTriples.MediaTypes.First());
                with.Body("<http://example.org/s> <http://example.org/p> <http://example.org/o> .");
            });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            brightstar.Verify();
        }

        [Test]
        public void TestPutCreatesNewNamedGraph()
        {
            IEnumerable<string> existingGraphs = new String [] { };
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(m => m.JobCompletedOk).Returns(true);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            brightstar.Setup(s => s.ListNamedGraphs("foo")).Returns(existingGraphs);
            brightstar.Setup(s => s.ExecuteUpdate("foo",
                It.Is<string>(p=>SpaceNormalizedStringsAreEqual(@"DROP SILENT GRAPH <http://example.org/g>; INSERT DATA { GRAPH <http://example.org/g> { <http://example.org/s> <http://example.org/p> <http://example.org/o> . } }", p)),
                true, It.IsAny<string>())).Returns(mockJobInfo.Object).Verifiable();

            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Put("/foo/graphs", with =>
            {
                with.Query("graph", "http://example.org/g");
                with.Header("Content-Type", RdfFormat.NTriples.MediaTypes.First());
                with.Body("<http://example.org/s> <http://example.org/p> <http://example.org/o> .");
            });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            brightstar.Verify();
        }

        [Test]
        public void TestPutUpdatesDefaultGraph()
        {
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(m => m.JobCompletedOk).Returns(true);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            brightstar.Setup(s => s.ExecuteUpdate("foo",
                It.Is<string>(p=>SpaceNormalizedStringsAreEqual(@"DROP SILENT DEFAULT; INSERT DATA { <http://example.org/s> <http://example.org/p> <http://example.org/o> . }", p)),
                true, It.IsAny<string>())).Returns(mockJobInfo.Object).Verifiable();

            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Put("/foo/graphs", with =>
            {
                with.Query("default", "");
                with.Header("Content-Type", RdfFormat.NTriples.MediaTypes.First());
                with.Body("<http://example.org/s> <http://example.org/p> <http://example.org/o> .");
            });
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            brightstar.Verify();
        }

        [Test]
        public void TestDeleteUnknownStoreReturnsNotFound()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s=>s.DoesStoreExist("foo")).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Delete("foo/graphs", with => with.Query("graph", "http://example.org/g"));
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            brightstar.Verify();
        }

        [Test]
        public void TestDeleteUnknownNamedGraphReturnsNotFound()
        {
            IEnumerable<string> graphs = new string[] {"http://example.org/g1", "http://example.org/g2"};
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s => s.ListNamedGraphs("foo")).Returns(graphs).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Delete("foo/graphs", with => with.Query("graph", "http://example.org/g"));
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            brightstar.Verify();
        }

        [Test]
        public void TestDeleteNamedGraph()
        {
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(m => m.JobCompletedOk).Returns(true);
            IEnumerable<string> graphs = new string[] { "http://example.org/g1", "http://example.org/g2" };
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s => s.ListNamedGraphs("foo")).Returns(graphs).Verifiable();
            brightstar.Setup(s=>s.ExecuteUpdate("foo", "DROP GRAPH <http://example.org/g1>", true, "Drop Graph http://example.org/g1"))
                .Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Delete("foo/graphs", with => with.Query("graph", "http://example.org/g1"));
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            brightstar.Verify();
            
        }

        [Test]
        public void TestDeleteDefaultGraph()
        {
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(m => m.JobCompletedOk).Returns(true);
            IEnumerable<string> graphs = new string[] { "http://example.org/g1", "http://example.org/g2" };
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s => s.ExecuteUpdate("foo", "DROP DEFAULT", true, "Drop Default Graph"))
                .Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var response = app.Delete("foo/graphs", with => with.Query("default", ""));
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            brightstar.Verify();
        }
    }
}
