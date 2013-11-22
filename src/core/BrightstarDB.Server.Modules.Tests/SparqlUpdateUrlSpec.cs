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
    public class SparqlUpdateUrlSpec
    {
        [Test]
        public void TestPostFormWithNoAdditionalParameters()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(j => j.JobCompletedOk).Returns(true);
            brightstar.Setup(s => s.ExecuteUpdate("foo", "update expression", true))
                      .Returns(mockJobInfo.Object)
                      .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/foo/update", with => with.FormValue("update", "update expression"));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            brightstar.Verify();
        }

        [Test]
        public void TestPostSparqlUpdateString()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(j => j.JobCompletedOk).Returns(true);
            brightstar.Setup(s=>s.ExecuteUpdate("foo", "update expression", true))
                .Returns(mockJobInfo.Object)
                .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/foo/update", with =>
                {
                    with.Body("update expression");
                    with.Header("Content-Type", "application/sparql-update");
                });

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            brightstar.Verify();
        }

        [Test]
        public void TestSparqlUpdateFailed()
        {
            var brightstar = new Mock<IBrightstarService>();
            var mockJobInfo = new Mock<IJobInfo>();
            mockJobInfo.Setup(j => j.JobCompletedOk).Returns(false);
            mockJobInfo.Setup(j => j.JobCompletedWithErrors).Returns(true);
            brightstar.Setup(s=>s.ExecuteUpdate("foo", "update expression", true))
                .Returns(mockJobInfo.Object)
                .Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Post("/foo/update", with => with.FormValue("update", "update expression"));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            brightstar.Verify();
        }

        [Test]
        public void TestRequiresSparqlUpdatePermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissions = new Mock<AbstractStorePermissionsProvider>();
            permissions.Setup(s=>s.HasStorePermission(null, "foo", StorePermissions.SparqlUpdate)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissions.Object));

            var response = app.Post("/foo/update", with => with.FormValue("update", "update expression"));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            permissions.Verify();

        }

        [Test]
        public void TestGetReturnsHtmlForm()
        {
            var brightstar = new Mock<IBrightstarService>();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Get("/foo/update", with=>with.Accept(MediaRange.FromString("text/html")));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Body.AsString(), Contains.Substring("<title>BrightstarDB: SPARQL Update</title>"));
        }
    }
}
