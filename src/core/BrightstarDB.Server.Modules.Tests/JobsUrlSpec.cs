using System;
using System.Collections.Generic;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Server.Modules.Permissions;
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
        private static readonly MediaRange Json = new MediaRange("application/json");

        #region Consolidate Job
        [Test]
        public void TestPostConsolidateJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("1234");
            brightstar.Setup(s => s.ConsolidateStore("foo", "Consolidate Job"))
                      .Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateConsolidateJob("Consolidate Job");

            // Execute
            var response = app.Post("/foo/jobs", with =>
                {
                    with.Accept(new MediaRange("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/1234"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostConsolidateJobRequiresAdminPermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateConsolidateJob(), StorePermissions.Admin);
        }
        #endregion

        #region Export Job
        [Test]
        public void TestPostExportJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("2345");
            brightstar.Setup(s=>s.StartExport("foo", "export.nt", null, It.Is<RdfFormat>(r=>r.DefaultExtension.Equals(RdfFormat.NQuads.DefaultExtension)), "ExportJob")).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateExportJob("export.nt", label:"ExportJob");

            // Execute
            var response = app.Post("foo/jobs", with =>
                {
                    with.Accept(new MediaRange("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/2345"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostExportGraphJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("2345");
            brightstar.Setup(s => s.StartExport("foo", "export.nt", "http://some/graph/uri", It.Is<RdfFormat>(r=>r.DefaultExtension.Equals(RdfFormat.NTriples.DefaultExtension)), "ExportJob")).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateExportJob("export.nt", "http://some/graph/uri", RdfFormat.NTriples, "ExportJob");

            // Execute
            var response = app.Post("foo/jobs", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/2345"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostExportJobRequiresExportPermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateExportJob("export.nt"), StorePermissions.Export);
            AssertPermissionRequired(JobRequestObject.CreateExportJob("export.nt", "http://some/graph/uri"),
                                     StorePermissions.Export);
        }
        #endregion

        #region
        [Test]
        public void TestPostImportJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("3456");
            brightstar.Setup(s=>s.StartImport("foo", "import.nt", null, "ImportJob", null)).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateImportJob("import.nt", label:"ImportJob");

            // Execute
            var response = app.Post("/foo/jobs", with =>
                {
                    with.Accept(new MediaRange("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/3456"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostImportGraphJob()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("3456");
            brightstar.Setup(s => s.StartImport("foo", "import.nt", "http://import/graph/uri", "ImportGraphJob", null)).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateImportJob("import.nt", "http://import/graph/uri", "ImportGraphJob");

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/3456"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostImportWithFormatOverride()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("3456");
            brightstar.Setup(s => s.StartImport("foo", "import.nt", null, "ImportJob", It.Is<RdfFormat>(f=>f.MatchesMediaType(RdfFormat.RdfXml)))).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateImportJob("import.nt", label: "ImportJob", importFormat:RdfFormat.RdfXml);

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/3456"));
            brightstar.Verify();
        }

        [Test]
        public void TestPostImportJobRequiresUpdatePermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateImportJob("import.nt"), StorePermissions.TransactionUpdate);
            AssertPermissionRequired(JobRequestObject.CreateImportJob("import.nt", "http://some/graph/uri"), StorePermissions.TransactionUpdate);
        }

        #endregion

        #region SPARQL Update
        [Test]
        public void TestPostSparqlUpdateJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("4567");
            brightstar.Setup(s => s.ExecuteUpdate("foo", "update expression", false,"SparqlUpdateJob")).Returns(mockJobInfo.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateSparqlUpdateJob("update expression", "SparqlUpdateJob");

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/4567"));
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
        public void TestPostSparqlUpdateJobRequiresSparqlUpdatePermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateSparqlUpdateJob("update expression"), StorePermissions.SparqlUpdate);
        }

        #endregion

        #region Update Transaction
        [Test]
        public void TestPostTransactionJob()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(s => s.JobId).Returns("5678");
            brightstar.Setup(
                s =>
                s.ExecuteTransaction("foo", It.Is<UpdateTransactionData>(txn =>
                                                                         txn.ExistencePreconditions ==
                                                                         "preconditions string" &&
                                                                         txn.NonexistencePreconditions ==
                                                                         "nonexistence preconditions string" &&
                                                                         txn.DeletePatterns == "delete patterns string" &&
                                                                         txn.InsertData == "insert triples string" &&
                                                                         txn.DefaultGraphUri == null), false,
                                     "TransactionJob"))
                      .Returns(mockJobInfo.Object)
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateTransactionJob(
                new UpdateTransactionData
                    {
                        ExistencePreconditions = "preconditions string",
                        NonexistencePreconditions = "nonexistence preconditions string",
                        DeletePatterns = "delete patterns string",
                        InsertData = "insert triples string",
                        DefaultGraphUri = null
                    }, "TransactionJob");
            

            // Execute
            var response = app.Post("/foo/jobs", with =>
                {
                    with.Accept(new MediaRange("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/5678"));
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
                s.ExecuteTransaction("foo",
                                     It.Is<UpdateTransactionData>(
                                         x => x.ExistencePreconditions == "preconditions string" &&
                                              x.NonexistencePreconditions == "nonexistence preconditions string" &&
                                              x.DeletePatterns == "delete patterns string" &&
                                              x.InsertData == "insert triples string" &&
                                              x.DefaultGraphUri == "http://update/graph/uri"), false,
                                     "TransactionGraphJob"))
                      .Returns(mockJobInfo.Object)
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));
            var requestObject = JobRequestObject.CreateTransactionJob(
                new UpdateTransactionData
                    {
                        ExistencePreconditions = "preconditions string",
                        NonexistencePreconditions = "nonexistence preconditions string",
                        DeletePatterns = "delete patterns string",
                        InsertData = "insert triples string",
                        DefaultGraphUri = "http://update/graph/uri"
                    }, "TransactionGraphJob");

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(requestObject);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/5678"));
            brightstar.Verify();            
        }

        [Test]
        public void TestPostTransactionAllowsParametersToBeOmitted()
        {
            // Can omit preconditions
            TestValidPost("/foo/jobs",
                          "{ JobType: \"Transaction\", JobParameters: { Deletes: \"delete\", Inserts: \"insert\", DefaultGraphUri:\"graph\"}}",
                          (b, m) =>
                          b.Setup(
                              s =>
                              s.ExecuteTransaction("foo",
                                                   It.Is<UpdateTransactionData>(x => x.ExistencePreconditions==null &&
                                                                                     x.NonexistencePreconditions==null &&
                                                                                     x.DeletePatterns.Equals("delete") &&
                                                                                     x.InsertData.Equals("insert") &&
                                                                                     x.DefaultGraphUri.Equals("graph")
                                                       )
                                                   , false, It.IsAny<string>()))
                           .Returns(m.Object)
                           .Verifiable());

            // Can omit delete patterns
            TestValidPost("/foo/jobs",
                          "{ JobType: \"Transaction\", JobParameters: { Inserts: \"insert\", DefaultGraphUri:\"graph\"}}",
                          (b, m) =>
                          b.Setup(s => s.ExecuteTransaction("foo",
                                                            It.Is<UpdateTransactionData>(
                                                                x => x.ExistencePreconditions == null &&
                                                                     x.NonexistencePreconditions == null &&
                                                                     x.DeletePatterns == null &&
                                                                     x.InsertData == "insert" &&
                                                                     x.DefaultGraphUri == "graph"), false,
                                                            It.IsAny<string>()))
                           .Returns(m.Object)
                           .Verifiable());

            // Can omit inserts
            TestValidPost("/foo/jobs", "{ JobType: \"Transaction\", JobParameters: { DefaultGraphUri:\"graph\"}}",
                          (b, m) =>
                          b.Setup(s => s.ExecuteTransaction("foo",
                                                            It.Is<UpdateTransactionData>(
                                                                x => x.ExistencePreconditions == null &&
                                                                     x.NonexistencePreconditions == null &&
                                                                     x.DeletePatterns == null &&
                                                                     x.InsertData == null &&
                                                                     x.DefaultGraphUri == "graph"), false,
                                                            It.IsAny<string>()))
                           .Returns(m.Object)
                           .Verifiable());
        }

        [Test]
        public void TestPostTransactionRequiresTransactionalUpdatePermission()
        {
            AssertPermissionRequired(
                JobRequestObject.CreateTransactionJob(new UpdateTransactionData
                    {
                        ExistencePreconditions = "pre",
                        NonexistencePreconditions = "nexist",
                        DeletePatterns = "del",
                        InsertData = "ins",
                        DefaultGraphUri = null
                    }, null), StorePermissions.TransactionUpdate);
        }

        [Test]
        public void TestPostGraphTransactionRequiresTransactionalUpdatePermission()
        {
            AssertPermissionRequired(
                JobRequestObject.CreateTransactionJob(
                    new UpdateTransactionData
                        {
                            ExistencePreconditions = "pre",
                            NonexistencePreconditions = "nexist",
                            DeletePatterns = "del",
                            InsertData = "ins",
                            DefaultGraphUri = "http://some/graph/uri"
                        }, null),
                StorePermissions.TransactionUpdate);
        }

        #endregion

        #region Stats Update
        [Test]
        public void TestPostUpdateStatsJob()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateUpdateStatsJob(label:"UpdateStatsJob"),
                          (b, m) => b.Setup(s => s.UpdateStatistics("foo", "UpdateStatsJob")).Returns(m.Object).Verifiable());
        }

        [Test]
        public void TestPostUpdateStatsJobRequiresAdminPermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateUpdateStatsJob(), StorePermissions.Admin);
        }
        #endregion

        #region Re-execute Transaction
        [Test]
        public void TestPostRepeatTransactionJob()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateRepeatTransactionJob(Guid.NewGuid(), "ReExecuteTransaction"),
                          (b, m) =>
                              {
                                  var mockTransaction = new Mock<ITransactionInfo>();
                                  b.Setup(s => s.GetTransaction("foo", It.IsAny<Guid>()))
                                   .Returns(mockTransaction.Object)
                                   .Verifiable();
                                  b.Setup(s => s.ReExecuteTransaction("foo", mockTransaction.Object, "ReExecuteTransaction"))
                                   .Returns(m.Object)
                                   .Verifiable();
                              });
        }

        [Test]
        public void TestPostRepeatTransactionJobRequiresAdminPermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateRepeatTransactionJob(Guid.Empty), StorePermissions.Admin);
        }
        #endregion

        #region Snapshot Job
        [Test]
        public void TestPostSnapshotJob()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateSnapshotJob("bar", PersistenceType.AppendOnly, 123, "SnapshotJob"),
                (b, m) =>
                    {
                        var mockCommitPoint = new Mock<ICommitPointInfo>();
                        b.Setup(s => s.GetCommitPoint("foo", 123)).Returns(mockCommitPoint.Object);
                        b.Setup(s=>s.CreateSnapshot("foo", "bar", PersistenceType.AppendOnly, It.IsAny<ICommitPointInfo>(), "SnapshotJob"))
                            .Returns(m.Object)
                            .Verifiable();
                    });
        }

        [Test]
        public void TestPostSnapshotJobWithDefaultCommitPoint()
        {
            TestValidPost("/foo/jobs", JobRequestObject.CreateSnapshotJob("bar", PersistenceType.Rewrite, label:"SnapshotJob"),
                (b, m) => b.Setup(s => s.CreateSnapshot("foo", "bar", PersistenceType.Rewrite, null, "SnapshotJob"))
                           .Returns(m.Object)
                           .Verifiable());
            
        }

        [Test]
        public void TestPostSnapshotJobRequiresAdminPermissions()
        {
            AssertPermissionRequired(JobRequestObject.CreateSnapshotJob("bar", PersistenceType.AppendOnly, 123), StorePermissions.Admin);
            AssertPermissionRequired(JobRequestObject.CreateSnapshotJob("bletch", PersistenceType.Rewrite), StorePermissions.Admin);
        }
        #endregion

        #region List Jobs
        [Test]
        public void TestListJobs()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetJobInfo("foo", 0, 11)).Returns(MockJobInfo(11)).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Get("/foo/jobs", with => with.Accept(Json));
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var jobs = response.Body.DeserializeJson<List<JobResponseModel>>();
            Assert.That(jobs, Is.Not.Null);
            Assert.That(jobs.Count, Is.EqualTo(10));
            brightstar.Verify();
        }

        private static IEnumerable<IJobInfo> MockJobInfo(int num)
        {
            for (int i = 0; i < num; i++)
            {
                var mock = new Mock<IJobInfo>();
                mock.Setup(j => j.JobId).Returns(Guid.NewGuid().ToString);
                mock.Setup(j => j.StatusMessage).Returns("Mock job #" + i);
                mock.Setup(j => j.JobCompletedOk).Returns(true);
                yield return mock.Object;
            }
        }

        #endregion

        [Test]
        public void TestGetJob()
        {
            var mockJob = new Mock<IJobInfo>();
            mockJob.Setup(j => j.JobId).Returns("EDBB1735-426B-4A57-B8E2-91C581D54075");
            mockJob.Setup(j=> j.Label).Returns("MockJobLabel");
            mockJob.Setup(j => j.StatusMessage).Returns("Mock Job");
            mockJob.Setup(j => j.JobCompletedOk).Returns(true);
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetJobInfo("foo", "EDBB1735-426B-4A57-B8E2-91C581D54075")).Returns(
                mockJob.Object).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Get("/foo/jobs/EDBB1735-426B-4A57-B8E2-91C581D54075", with => with.Accept(Json));
            
            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var job = response.Body.DeserializeJson<JobResponseModel>();
            Assert.That(job, Is.Not.Null);
            Assert.That(job.JobId, Is.EqualTo("EDBB1735-426B-4A57-B8E2-91C581D54075"));
            Assert.That(job.StatusMessage, Is.EqualTo("Mock Job"));
            Assert.That(job.JobCompletedOk, Is.EqualTo(true));
            Assert.That(job.JobCompletedWithErrors, Is.EqualTo(false));
            Assert.That(job.Label, Is.EqualTo("MockJobLabel"));
            brightstar.Verify();
        }

        [Test]
        public void TestGetJobWithInvalidJobIdReturnsNotFound()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.GetJobInfo("foo", "EDBB1735-426B-4A57-B8E2-91C581D54075"))
                      .Returns(()=>null)
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Get("/foo/jobs/EDBB1735-426B-4A57-B8E2-91C581D54075");

            Assert.That(response, Is.Not.Null);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        private void AssertPermissionRequired(JobRequestObject jobRequest, StorePermissions witheldPermission)
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissionsService = new Mock<AbstractStorePermissionsProvider>();
            permissionsService.Setup(s => s.GetStorePermissions(null, "foo"))
                              .Returns(StorePermissions.All ^ witheldPermission)
                              .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissionsService.Object));

            // Execute
            var response = app.Post("/foo/jobs", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(jobRequest);
            });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            permissionsService.Verify();
        }

        private static void TestBadPost(string toUrl, string jsonString)
        {
            var brightstar = new Mock<IBrightstarService>();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Post(toUrl, with =>
                {
                    with.Accept(new MediaRange("application/json"));
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
                    with.Accept(new MediaRange("application/json"));
                    with.JsonBody(requestObject);
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Headers["Location"], Is.EqualTo("foo/jobs/ABCD"));
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
                    with.Accept(new MediaRange("application/json"));
                    with.Body(jsonString);
                    with.Header("Content-Type", "application/json");
                });
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            brightstar.Verify();
        }
    }
}
