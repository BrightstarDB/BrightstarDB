using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
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
            var storeInfo = response.Body.DeserializeJson<StoreResponseObject>();
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
    }
}
