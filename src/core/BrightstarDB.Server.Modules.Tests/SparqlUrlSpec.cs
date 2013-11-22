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
            var response = TestGetSucceeds("foo", "sparql query string", null, null, null,
                            bs =>
                            bs.Setup(s => s.ExecuteQuery("foo", "sparql query string", (IEnumerable<string>)null, null, SparqlResultsFormat.Xml))
                              .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse))));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsSparqlJsonWithoutAdditionalParameters()
        {
            const string mockResponse = "{ \"head\": { }, \"results\": { } }";
            var response = TestGetSucceeds(
                "foo", "sparql query string", null, null,
                MediaRange.FromString("application/sparql-results+json"),
                bs => bs.Setup(
                    s => s.ExecuteQuery("foo", "sparql query string", (IEnumerable<string>) null,null, SparqlResultsFormat.Json))
                        .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse))));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsSparqlCsvWithoutAdditionalParameters()
        {
            const string mockResponse = "x,y";
            var response = TestGetSucceeds(
                "foo", "sparql query string", null, null,
                MediaRange.FromString("text/csv"),
                bs=>bs.Setup(s=>s.ExecuteQuery("foo", "sparql query string", (IEnumerable<string>)null, null, SparqlResultsFormat.Csv))
                    .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse))));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryAsSparqlTsvWithoutAdditionalParameters()
        {
            const string mockResponse = "x\ty";
            var response = TestGetSucceeds(
                "foo", "sparql query string", null, null,
                MediaRange.FromString("text/tab-separated-values"),
                bs => bs.Setup(s => s.ExecuteQuery("foo", "sparql query string", (IEnumerable<string>)null, null, SparqlResultsFormat.Tsv))
                    .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse))));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryWithSingleDefaultGraphUri()
        {
            const string mockResponse = "yello";
            var response = TestGetSucceeds(
                "bar", "another query", new[] {"http://some/graph/uri"}, null,
                MediaRange.FromString("application/sparql-results+xml"),
                bs =>
                bs.Setup(
                    s =>
                    s.ExecuteQuery("bar", "another query", new[] {"http://some/graph/uri"}, null,
                                   SparqlResultsFormat.Xml))
                  .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse))));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
        }

        [Test]
        public void TestGetQueryWithMultipleDefaultGraphUri()
        {
            const string mockResponse = "yello";
            var response = TestGetSucceeds(
                "bar", "another query", new[] { "http://some/graph/uri", "http://some/other/graph" }, null,
                MediaRange.FromString("application/sparql-results+xml"),
                bs =>
                bs.Setup(
                    s =>
                    s.ExecuteQuery("bar", "another query", new[] { "http://some/graph/uri", "http://some/other/graph" }, null,
                                   SparqlResultsFormat.Xml))
                  .Returns(new MemoryStream(Encoding.UTF8.GetBytes(mockResponse))));
            Assert.That(response.Body.AsString(), Is.EqualTo(mockResponse));
            
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
            brightstar.Setup(
                s => s.ExecuteQuery("foo", "query", (IEnumerable<string>) null, It.IsNotNull<DateTime?>(), SparqlResultsFormat.Xml))
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
            brightstar.Setup(
                s => s.ExecuteQuery("foo", "query", (IEnumerable<string>)null, It.IsNotNull<DateTime?>(), SparqlResultsFormat.Xml))
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
            brightstar.Setup(
                s =>
                s.ExecuteQuery(It.Is<ICommitPointInfo>(c => c.Id.Equals(123)), "query", (IEnumerable<string>) null,
                               SparqlResultsFormat.Xml))
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
            brightstar.Setup(
                s =>
                s.ExecuteQuery(It.Is<ICommitPointInfo>(c => c.Id.Equals(123)), "query", (IEnumerable<string>)null,
                               SparqlResultsFormat.Xml))
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
            string storeName, string query, IEnumerable<string> defaultGraphUris, 
            IEnumerable<string> namedGraphUris, 
            MediaRange accept,
            Action<Mock<IBrightstarService>> brightstarSetup)
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstarSetup(brightstar);
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
                    with.Accept(accept);
                });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.ContentType, Is.EqualTo(accept.ToString()));
            brightstar.Verify();
            return response;
        }

        private static void TestFormPostSucceeds(string storeName, string query, IEnumerable<string> defaultGraphUris, IEnumerable<string> namedGraphUris, MediaRange accept, SparqlResultsFormat expectedQueryFormat, Action<Mock<IBrightstarService>> brightstarSetup)
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.ExecuteQuery(storeName, query, defaultGraphUris, null, expectedQueryFormat))
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
            Assert.That(response.ContentType, Is.EqualTo(accept.ToString()));
            Assert.That(response.Body.AsString(), Is.EqualTo("Mock Results"));
            brightstar.Verify();
        }

        private static void TestSparqlPostSucceeds(string storeName, string query, IEnumerable<string> defaultGraphUris, IEnumerable<string> namedGraphUris, MediaRange accept, SparqlResultsFormat expectedQueryFormat, Action<Mock<IBrightstarService>> brightstarSetup)
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.ExecuteQuery(storeName, query, defaultGraphUris, null, expectedQueryFormat))
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
            Assert.That(response.ContentType, Is.EqualTo(accept.ToString()));
            Assert.That(response.Body.AsString(), Is.EqualTo("Mock Results"));
            brightstar.Verify();
        }

        #endregion
    }
}
