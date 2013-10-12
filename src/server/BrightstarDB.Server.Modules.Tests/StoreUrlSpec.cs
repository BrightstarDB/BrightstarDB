using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class StoreUrlSpec
    {
        [Test]
        public void TestGetExistingStoreReturnsOk()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo", with => with.Accept(MediaRange.FromString("application/json")));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var storeInfo = response.Body.DeserializeJson<StoreResponseModel>();
            Assert.That(storeInfo, Is.Not.Null);
            Assert.That(storeInfo, Has.Property("Name").EqualTo("foo"));
            Assert.That(storeInfo, Has.Property("Jobs").EqualTo("foo/jobs"));
            Assert.That(storeInfo, Has.Property("Commits").EqualTo("foo/commits"));
            Assert.That(storeInfo, Has.Property("Transactions").EqualTo("foo/transactions"));
            Assert.That(storeInfo, Has.Property("Statistics").EqualTo("foo/statistics"));
        }

        [Test]
        public void TestGetNonExistantStoreReturnsNotFound()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Get("/foo", with => with.Accept(MediaRange.FromString("application/json")));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void TestHeadExistingStoreReturnsOk()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Head("/foo");
            
            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void TestHeadNonExistantStoreReturnsNotFound()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false);
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            // Execute
            var response = app.Head("/foo");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public void TestGetRequiresReadPermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var storePermissions = new Mock<AbstractStorePermissionsProvider>();
            storePermissions.Setup(s=>s.HasStorePermission(null, "foo", StorePermissions.Read)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, storePermissions.Object));

            // Execute
            var response = app.Get("/foo");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void TestHeadRequiresReadPermissions()
        {
            var brightstar = new Mock<IBrightstarService>();
            var storePermissions = new Mock<AbstractStorePermissionsProvider>();
            storePermissions.Setup(s => s.HasStorePermission(null, "foo", StorePermissions.Read)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, storePermissions.Object));

            // Execute
            var response = app.Head("/foo");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public void TestDeleteReturnsOk()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s=>s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s=>s.DeleteStore("foo")).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Delete("/foo", c=>c.Accept(MediaRange.FromString("application/json")));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            brightstar.Verify();
        }

        [Test]
        public void TestDeleteHtmlResponse()
        {
            var brightstar = new Mock<IBrightstarService>();
            brightstar.Setup(s=>s.DoesStoreExist("foo")).Returns(true).Verifiable();
            brightstar.Setup(s => s.DeleteStore("foo")).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object));

            var response = app.Delete("/foo", c => c.Accept(MediaRange.FromString("text/html")));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Body.AsString(), Contains.Substring("Store 'foo' deleted successfully."));
            brightstar.Verify();
            
        }

        [Test]
        public void TestDeleteRequiresStoreAdminPermission()
        {
            var brightstar = new Mock<IBrightstarService>();
            var storePermissions = new Mock<AbstractStorePermissionsProvider>();

            storePermissions.Setup(s => s.HasStorePermission(null, "foo", StorePermissions.Admin)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, storePermissions.Object));

            // Execute
            var response = app.Delete("/foo");

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
