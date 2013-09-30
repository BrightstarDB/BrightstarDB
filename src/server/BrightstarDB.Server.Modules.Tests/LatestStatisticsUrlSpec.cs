using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class LatestStatisticsUrlSpec : StatisticsUrlSpecBase
    {
        [Test]
        public void TestGetLatestStatistics()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s=>s.GetStatistics("foo")).Returns(MockStatistics(1).First()).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo/statistics/latest", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var result = response.Body.DeserializeJson<StatisticsResponseObject>();
            Console.WriteLine(response.Headers["Link"]);
            Assert.That(result, Is.Not.Null);
            brightstar.Verify();
        }

        [Test]
        public void TestGetLatestStatisticsRequiresQueryPermisions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var permissions = new Mock<IStorePermissionsProvider>();
            permissions.Setup(s=>s.HasStorePermission(null, "foo", StorePermissions.Query)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, permissions.Object));

            // Execute
            var response = app.Get("/foo/statistics/latest", with => with.Accept(Json));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}