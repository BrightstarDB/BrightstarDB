using System;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Storage;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class JobsUrlSpec
    {
        [Test]
        public void TestPostConsolidateJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("1234");
            brightstar.Setup(s => s.ConsolidateStore("foo"))
                      .Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateConsolidateJob();

            // Execute
            var response = app.Post("/foo/jobs", with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("1234"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostExportJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("2345");
            brightstar.Setup(s=>s.StartExport("foo", "export.nt", null)).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateExportJob("export.nt");

            // Execute
            var response = app.Post("foo/jobs", with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("2345"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostExportGraphJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("2345");
            brightstar.Setup(s => s.StartExport("foo", "export.nt", "http://some/graph/uri")).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateExportJob("export.nt", "http://some/graph/uri");

            // Execute
            var response = app.Post("foo/jobs", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("2345"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostImportJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("3456");
            brightstar.Setup(s=>s.StartImport("foo", "import.nt", null)).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateImportJob("import.nt");

            // Execute
            var response = app.Post("/foo/jobs", with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("3456"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostImportGraphJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("3456");
            brightstar.Setup(s => s.StartImport("foo", "import.nt", "http://import/graph/uri")).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateImportJob("import.nt", "http://import/graph/uri");

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("3456"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostSparqlUpdateJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("4567");
            brightstar.Setup(s => s.ExecuteUpdate("foo", "update expression", false)).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateSparqlUpdateJob("update expression");

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("4567"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostSparqlUpdateWithNoExpression()
        {
            TestBadPost("/foo/jobs", "{ JobType: \"SparqlUpdate\", JobParameters: { } }");
        }

        [Test]
        public void TestPostSparqlUpdateWithEmptyExpression()
        {
            TestBadPost("/foo/jobs", "{ JobType: \"SparqlUpdate\", JobParameters: { UpdateExpression: \"\" } }");
        }

        [Test]
        public void TestPostTransactionJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("5678");
            brightstar.Setup(
                s =>
                s.ExecuteTransaction("foo", "preconditions string", "delete patterns string", "insert triples string",
                                     null, false))
                                     .Returns(mockJobInfo.Object)
                                     .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateTransactionJob("preconditions string", "delete patterns string",
                                                                      "insert triples string");

            // Execute
            var response = app.Post("/foo/jobs", with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("5678"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostTransactionGraphJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("5678");
            brightstar.Setup(
                s =>
                s.ExecuteTransaction("foo", "preconditions string", "delete patterns string", "insert triples string",
                                     "http://update/graph/uri", false))
                                     .Returns(mockJobInfo.Object)
                                     .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateTransactionJob("preconditions string", "delete patterns string",
                                                                      "insert triples string", "http://update/graph/uri");

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(MediaRange.FromString("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("5678"));
            brightstar.Verify();            
        }

        [Test]
        public void TestPostTransactionAllowsParametersToBeOmitted()
        {
            // Can omit preconditions
            TestValidPost("/foo/jobs",
                          "{ JobType: \"Transaction\", JobParameters: { Deletes: \"delete\", Inserts: \"insert\", DefaultGraphUri:\"graph\"}}",
                          (b, m) =>
                          b.Setup(s => s.ExecuteTransaction("foo", null, "delete", "insert", "graph", false))
                           .Returns(m.Object)
                           .Verifiable());

            // Can omit delete patterns
            TestValidPost("/foo/jobs",
                          "{ JobType: \"Transaction\", JobParameters: { Inserts: \"insert\", DefaultGraphUri:\"graph\"}}",
                          (b, m) =>
                          b.Setup(s => s.ExecuteTransaction("foo", null, null, "insert", "graph", false))
                           .Returns(m.Object)
                           .Verifiable());

            // Can omit inserts
            TestValidPost("/foo/jobs", "{ JobType: \"Transaction\", JobParameters: { DefaultGraphUri:\"graph\"}}",
                          (b, m) =>
                          b.Setup(s => s.ExecuteTransaction("foo", null, null, null, "graph", false))
                           .Returns(m.Object)
                           .Verifiable());
        }

        [Test]
        public void TestPostUpdateStatsJob()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateUpdateStatsJob(),
                          (b, m) => b.Setup(s => s.UpdateStatistics("foo")).Returns(m.Object).Verifiable());
        }

        
        [Test]
        public void TestPostRepeatTransactionJob()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateRepeatTransactionJob(Guid.NewGuid()),
                          (b, m) =>
                              {
                                  var mockTransaction = new Mock<ITransactionInfo>();
                                  b.Setup(s => s.GetTransaction("foo", It.IsAny<Guid>()))
                                   .Returns(mockTransaction.Object)
                                   .Verifiable();
                                  b.Setup(s => s.ReExecuteTransaction("foo", mockTransaction.Object))
                                   .Returns(m.Object)
                                   .Verifiable();
                              });
        }

        [Test]
        public void TestPostSnapshotJob()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateSnapshotJob("bar", "AppendOnly", 123),
                (b, m) =>
                    {
                        var mockCommitPoint = new Mock<ICommitPointInfo>();
                        b.Setup(s => s.GetCommitPoint("foo", 123)).Returns(mockCommitPoint.Object);
                        b.Setup(s=>s.CreateSnapshot("foo", "bar", PersistenceType.AppendOnly, It.IsAny<ICommitPointInfo>()))
                            .Returns(m.Object)
                            .Verifiable();
                    });
        }

        [Test]
        public void TestPostSnapshotJobWithDefaultCommitPoint()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateSnapshotJob("bar", "Rewrite"),
                (b, m) => b.Setup(s => s.CreateSnapshot("foo", "bar", PersistenceType.Rewrite, null))
                           .Returns(m.Object)
                           .Verifiable());
            
        }

        private static void TestBadPost(string toUrl, string jsonString)
        {
            var brightstar = new Mock<IBrightstarService>();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Post(toUrl, with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.Body(jsonString);
                    with.Header("Content-Type", "application/json");
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        private static void TestValidPost(string toUrl, JobRequestObject requestObject, Action<Mock<IBrightstarService>, Mock<IJobInfo>> brightstarSetup)
        {
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("ABCD");
            var brightstar = new Mock<IBrightstarService>();
            brightstarSetup(brightstar, mockJobInfo);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post(toUrl, with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Headers["Content-Location"], Is.EqualTo("ABCD"));
            brightstar.Verify();
        }

        private static void TestValidPost(string toUrl, string jsonString, Action<Mock<IBrightstarService>, Mock<IJobInfo>> brightstarSetup)
        {
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("ABCD");
            var brightstar = new Mock<IBrightstarService>();
            brightstarSetup(brightstar, mockJobInfo);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            
            // Execute
            var response = app.Post(toUrl, with =>
                {
                    with.Accept(MediaRange.FromString("application/json"));
                    with.Body(jsonString);
                    with.Header("Content-Type", "application/json");
                });
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            brightstar.Verify();
        }
    }
}
