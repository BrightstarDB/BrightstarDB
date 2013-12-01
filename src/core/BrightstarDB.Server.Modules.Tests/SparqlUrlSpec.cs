using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Permissions;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class SparqlUrlSpec
    {
        private static readonly MediaRange SparqlXml = MediaRange.FromString("application/sparql-results+xml");

        [Test]
        public void TestGetQueryAsSparqlXmlWithoutAdditionalParameters()
        {
            const string mockResponse = "<results></results>";
            ISerializationFormat format = SparqlResultsFormat.Xml;
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.ExecuteQuery("foo", "sparql query string", null, null,
                                                 SparqlResultsFormat.Xml, It.IsAny<RdfFormat>(), out format))
                                   .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "foo", "sparql query string");
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsSparqlJsonWithoutAdditionalParameters()
        {
            const string mockResponse = "{ \"head\": { }, \"results\": { } }";
            ISerializationFormat format = SparqlResultsFormat.Json;
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(
                s => s.ExecuteQuery("foo", "sparql query string", null, null,
                                    SparqlResultsFormat.Json, It.IsAny<RdfFormat>(), out format))
                    .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "foo", "sparql query string",
                                           accept: MediaRange.FromString("application/sparql-results+json"));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsSparqlCsvWithoutAdditionalParameters()
        {
            const string mockResponse = "x,y";
            ISerializationFormat format = SparqlResultsFormat.Csv;
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(
                s => s.ExecuteQuery("foo", "sparql query string", null, null,
                                    SparqlResultsFormat.Csv, It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "foo", "sparql query string",
                                           accept: MediaRange.FromString("text/csv"));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsSparqlTsvWithoutAdditionalParameters()
        {
            const string mockResponse = "x\ty";
            ISerializationFormat format = SparqlResultsFormat.Tsv;
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.ExecuteQuery("foo", "sparql query string", null, null,
                                                 SparqlResultsFormat.Tsv, It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar,
                "foo", "sparql query string", accept:MediaRange.FromString("text/tab-separated-values"));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsRdfXmlWithoutAdditionalParameters()
        {
            const string mockResponse = "<rdf:RDF/>";
            ISerializationFormat format = RdfFormat.RdfXml.WithEncoding(Encoding.UTF8);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.ExecuteQuery("foo", "sparql query string", null, null,
                                                 It.IsAny<SparqlResultsFormat>(), RdfFormat.RdfXml, out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "foo", "sparql query string",
                                           accept: MediaRange.FromString("application/rdf+xml"));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryWithSingleDefaultGraphUri()
        {
            const string mockResponse = "yello";
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Xml;
            brightstar.Setup(
                s =>
                s.ExecuteQuery("bar", "another query", new[] {"http://some/graph/uri"}, null,
                               SparqlResultsFormat.Xml, It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "bar", "another query",
                                           defaultGraphUris: new[] {"http://some/graph/uri"},
                                           accept: MediaRange.FromString("application/sparql-results+xml"));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryWithMultipleDefaultGraphUri()
        {
            const string mockResponse = "yello";
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Xml;
            brightstar.Setup(
                s =>
                s.ExecuteQuery("bar", "another query", new[] {"http://some/graph/uri", "http://some/other/graph"}, null,
                               SparqlResultsFormat.Xml, It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "bar", "another query",
                                           defaultGraphUris: new[] {"http://some/graph/uri", "http://some/other/graph"},
                                           accept: MediaRange.FromString("application/sparql-results+xml"));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryWithFormatOverride()
        {
            const string mockResponse ="OK";
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Json;
            brightstar.Setup(
                s =>
                s.ExecuteQuery("foo", "query", null, null, It.IsAny<SparqlResultsFormat>(), It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse)));
            var response = TestGetSucceeds(brightstar, "foo", "query",
                                           formats: new string[] {format.MediaTypes[0]},
                                           accept: "application/xml");
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
            Assert.That(MediaRange.FromString(format.MediaTypes[0]).Matches(MediaRange.FromString(response.ContentType)),
                "Expected content type: {0}. Got: {1}", format.MediaTypes[0], response.ContentType);
        }

        [Test]
        public void TestFormPostQuery()
        {
            TestFormPostSucceeds("foo", "query", null, null, SparqlXml,
                                 SparqlResultsFormat.Xml, null);
            TestFormPostSucceeds("foo", "query", new[] {"http://some/graph/uri"}, null,
                                 MediaRange.FromString("application/sparql-results+xml"), SparqlResultsFormat.Xml, null);
            TestFormPostSucceeds("foo", "query", new[] { "http://some/graph/uri", "http://some/other/uri" }, null,
                                 MediaRange.FromString("application/sparql-results+xml"), SparqlResultsFormat.Xml, null);
        }

        [Test]
        public void TestPostSparql()
        {
            TestSparqlPostSucceeds("foo", "query", null, null, SparqlXml,
                                 SparqlResultsFormat.Xml, null);
            TestSparqlPostSucceeds("foo", "query", new[] { "http://some/graph/uri" }, null,
                                 MediaRange.FromString("application/sparql-results+xml"), SparqlResultsFormat.Xml, null);
            TestSparqlPostSucceeds("foo", "query", new[] { "http://some/graph/uri", "http://some/other/uri" }, null,
                                 MediaRange.FromString("application/sparql-results+xml"), SparqlResultsFormat.Xml, null);
        }

        [Test]
        public void TestGetQueryWithIfModifiedSince()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Xml;
            brightstar.Setup(
                s => s.ExecuteQuery("foo", "query", (IEnumerable<string>)null, It.IsNotNull<DateTime?>(), SparqlResultsFormat.Xml,  It.IsAny<RdfFormat>(), out format))
                      .Throws<BrightstarStoreNotModifiedException>()
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/sparql", with =>
                {
                    with.Query("query", "query");
                    with.Accept(SparqlXml);
                    with.Header("If-Modified-Since", DateTime.Now.ToUniversalTime().ToString("r"));
                });
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
            brightstar.Verify();
        }

        [Test]
        public void TestPostQueryWithIfModifiedSince()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Xml;
            brightstar.Setup(
                s => s.ExecuteQuery("foo", "query", (IEnumerable<string>)null, It.IsNotNull<DateTime?>(), SparqlResultsFormat.Xml,  It.IsAny<RdfFormat>(), out format))
                      .Throws<BrightstarStoreNotModifiedException>()
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/foo/sparql", with =>
            {
                with.FormValue("query", "query");
                with.Accept(SparqlXml);
                with.Header("If-Modified-Since", DateTime.Now.ToUniversalTime().ToString("r"));
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotModified));
        }


        [Test]
        public void TestGetQueryOnValidCommitPoint()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Xml;
            brightstar.Setup(
                s =>
                s.ExecuteQuery(It.Is<ICommitPointInfo>(c => c.Id.Equals(123)), "query", null,
                               SparqlResultsFormat.Xml,  It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes("mock results")))
                      .Verifiable();
            var commitPoint = new Mock<ICommitPointInfo>();
            commitPoint.Setup(c => c.Id).Returns(123);
            
            brightstar.Setup(s => s.GetCommitPoint("foo", 123L)).Returns(commitPoint.Object);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            
            // Execute
            var response = app.Get("/foo/commits/123/sparql", with =>
                {
                    with.Query("query", "query");
                    with.Accept(SparqlXml);
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Body.AsString(), Is.EqualTo("mock results"));
            brightstar.Verify();
        }

        [Test]
        public void TestGetQueryOnInvalidCommitPoint()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetCommitPoint("foo", 123L)).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/commits/123/sparql", with =>
            {
                with.Query("query", "query");
                with.Accept(SparqlXml);
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            brightstar.Verify();
        }

        [Test]
        public void TestPostQueryOnValidCommitPoint()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = SparqlResultsFormat.Xml;
            brightstar.Setup(
                s =>
                s.ExecuteQuery(It.Is<ICommitPointInfo>(c => c.Id.Equals(123)), "query", (IEnumerable<string>)null,
                               SparqlResultsFormat.Xml,  It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes("mock results")))
                      .Verifiable();
            var commitPoint = new Mock<ICommitPointInfo>();
            commitPoint.Setup(c => c.Id).Returns(123);

            brightstar.Setup(s => s.GetCommitPoint("foo", 123L)).Returns(commitPoint.Object);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/foo/commits/123/sparql", with =>
            {
                with.FormValue("query", "query");
                with.Accept(SparqlXml);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Body.AsString(), Is.EqualTo("mock results"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostQueryOnInvalidCommitPoint()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetCommitPoint("foo", 123L)).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/foo/commits/123/sparql", with =>
            {
                with.FormValue("query", "query");
                with.Accept(SparqlXml);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            brightstar.Verify();
        }

        [Test]
        public void TestGetRequiresReadPermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissions = new Mock<AbstractStorePermissionsProvider>();
            permissions.Setup(s=>s.HasStorePermission(null, "foo", StorePermissions.Read)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissions.Object));

            // Execute
            var response = app.Get("/foo/sparql", with =>
            {
                with.Query("query", "query");
                with.Accept(SparqlXml);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void TestPostRequiresReadPermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissions = new Mock<AbstractStorePermissionsProvider>();
            permissions.Setup(s => s.HasStorePermission(null, "foo", StorePermissions.Read)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissions.Object));

            // Execute
            var response = app.Post("/foo/sparql", with =>
            {
                with.FormValue("query", "query");
                with.Accept(SparqlXml);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        #region Helper Methods
        private static BrowserResponse TestGetSucceeds(
            Mock<IBrightstarService> brightstar,
            string storeName, string query, 
            IEnumerable<string> defaultGraphUris = null,
            IEnumerable<string> namedGraphUris = null, 
            IEnumerable<string> formats = null,
            MediaRange accept = null)
        {
            // Setup
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            if (accept == null) accept = MediaRange.FromString("application/sparql-results+xml");
            // Execute
            var response = app.Get("/" + storeName + "/sparql", with =>
                {
                    with.Query("query", query);
                    if (defaultGraphUris != null)
                    {
                        foreach (var defaultGraph in defaultGraphUris)
                        {
                            with.Query("default-graph-uri", defaultGraph);
                        }
                    }
                    if (namedGraphUris != null)
                    {
                        foreach (var namedGraph in namedGraphUris)
                        {
                            with.Query("named-graph-uri", namedGraph);
                        }
                    }
                    if (formats != null)
                    {
                        foreach (var format in formats)
                        {
                            with.Query("format", format);
                        }
                    }
                    with.Accept(accept);
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            if (formats == null)
            {
                Assert.That(accept.Matches(MediaRange.FromString(response.ContentType)));
            }
            brightstar.Verify();
            return response;
        }

        private static void TestFormPostSucceeds(string storeName, string query, IEnumerable<string> defaultGraphUris, IEnumerable<string> namedGraphUris, MediaRange accept, SparqlResultsFormat expectedQueryFormat, Action<Mock<IBrightstarService>> brightstarSetup)
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = expectedQueryFormat;
            brightstar.Setup(s => s.ExecuteQuery(storeName, query, defaultGraphUris, null, expectedQueryFormat,  It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes("Mock Results")))
                      .Verifiable();
            if (brightstarSetup != null) brightstarSetup(brightstar);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            
            // Execute
            var response = app.Post("/" + storeName + "/sparql", with =>
                {
                    with.FormValue("query", query);
                    if (defaultGraphUris != null)
                    {
                        foreach (var defaultGraphUri in defaultGraphUris)
                        {
                            with.FormValue("default-graph-uri", defaultGraphUri);
                        }
                    }
                    if (namedGraphUris != null)
                    {
                        foreach (var namedGraphUri in namedGraphUris)
                        {
                            with.FormValue("named-graph-uri", namedGraphUri);
                        }
                    }
                    with.Accept(accept);
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(accept.Matches(MediaRange.FromString(response.ContentType)));
            Assert.That(response.Body.AsString(), Is.EqualTo("Mock Results"));
            brightstar.Verify();
        }

        private static void TestSparqlPostSucceeds(string storeName, string query, IEnumerable<string> defaultGraphUris, IEnumerable<string> namedGraphUris, MediaRange accept, SparqlResultsFormat expectedQueryFormat, Action<Mock<IBrightstarService>> brightstarSetup)
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            ISerializationFormat format = expectedQueryFormat;
            brightstar.Setup(s => s.ExecuteQuery(storeName, query, defaultGraphUris, null, expectedQueryFormat,  It.IsAny<RdfFormat>(), out format))
                      .Returns(new MemoryStream(Encoding.UTF8.GetBytes("Mock Results")))
                      .Verifiable();
            if (brightstarSetup != null) brightstarSetup(brightstar);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/" + storeName + "/sparql", with =>
            {
                with.Body(query);
                with.Header("Content-Type", "application/sparql-query");
                if (defaultGraphUris != null)
                {
                    foreach (var defaultGraphUri in defaultGraphUris)
                    {
                        with.Query("default-graph-uri", defaultGraphUri);
                    }
                }
                if (namedGraphUris != null)
                {
                    foreach (var namedGraphUri in namedGraphUris)
                    {
                        with.Query("named-graph-uri", namedGraphUri);
                    }
                }
                with.Accept(accept);
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(accept.Matches(MediaRange.FromString(response.ContentType)));
            Assert.That(response.Body.AsString(), Is.EqualTo("Mock Results"));
            brightstar.Verify();
        }

        #endregion
    }
}
