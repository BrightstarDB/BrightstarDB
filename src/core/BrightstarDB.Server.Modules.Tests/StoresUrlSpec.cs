using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Server.Modules.Permissions;
using BrightstarDB.Storage;
using NUnit.Framework;
using Nancy;
using Nancy.Responses.Negotiation;
using Nancy.Testing;
using Moq;
using CreateStoreRequestObject = BrightstarDB.Server.Modules.Model.CreateStoreRequestObject;
using StoreResponseModel = BrightstarDB.Server.Modules.Model.StoreResponseModel;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class StoresUrlSpec
    {
        
        [Test]
        public void TestGetReturnsOk()
        {
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[0]);
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object,
                                          new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                          new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.ListStores)));

            var response = app.Get("/", c=>c.Accept(new MediaRange("application/json")));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void TestGetHtmlReturnsOk()
        {
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[0]);
            var app =
                new Browser(new FakeNancyBootstrapper(mockBrightstar.Object, new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.ListStores)));
            var response = app.Get("/");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Body.AsString(), Contains.Substring("<title>BrightstarDB: Stores</title>"));
        }

        [Test]
        public void TestGetRequiresListStoresPermissions()
        {
            // Setup
            var brightstar = new Mock<IBrightstarService>();
            var systemPermissions = new Mock<AbstractSystemPermissionsProvider>();
            systemPermissions.Setup(s=>s.HasPermissions(null, SystemPermissions.ListStores)).Returns(false).Verifiable();
            var app = new Browser(new FakeNancyBootstrapper(brightstar.Object, new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All), systemPermissions.Object));

            // Execute
            var response = app.Get("/", c => c.Accept(new MediaRange("application/json")));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            systemPermissions.Verify();
        }

        [Test]
        public void TestGetReturnsJsonArray()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new[] {"store1", "store2", "store3"});
            var app =
                new Browser(new FakeNancyBootstrapper(mockBrightstar.Object,
                                                      new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.ListStores)));

            // Execute
            var response = app.Get("/", c => c.Accept(new MediaRange("application/json")));

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.ContentType, Contains.Substring("application/json"));
            Assert.That(response.Body, Is.Not.Null);
            var responseContent = response.Body.DeserializeJson <StoresResponseModel>();
            Assert.That(responseContent, Is.Not.Null);
            Assert.That(responseContent.Stores, Is.Not.Null);
            Assert.That(responseContent.Stores.Count, Is.EqualTo(3));
            Assert.That(responseContent.Stores.Any(s => s.Equals("store1") ));
            Assert.That(responseContent.Stores.Any(s => s.Equals("store2") ));
            Assert.That(responseContent.Stores.Any(s => s.Equals("store3") ));
        }

        [Test]
        public void TestPostToCreateStore()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false);
            mockBrightstar.Setup(s => s.CreateStore("foo", PersistenceType.AppendOnly)).Verifiable("Expected CreateStore to be called");
            var app = new Browser(new FakeNancyBootstrapper(mockBrightstar.Object, new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All), new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.CreateStore)));

            // Execute
            var response = app.Post("/", with =>
                {
                    with.Accept(new MediaRange("application/json"));
                    with.JsonBody(new CreateStoreRequestObject("foo", PersistenceType.AppendOnly));
                    with.AjaxRequest();
                });

            // Assert
            mockBrightstar.Verify();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.ContentType, Contains.Substring("application/json"));
            Assert.That(response.Body, Is.Not.Null);
            var responseContent = response.Body.DeserializeJson<StoreResponseModel>();
            Assert.That(responseContent, Is.Not.Null);
            Assert.That(responseContent, Has.Property("Name").EqualTo("foo"));
            Assert.That(responseContent, Has.Property("Jobs").EqualTo("foo/jobs"));
        }

        [Test]
        public void TestCannotDuplicateStoreName()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.DoesStoreExist("foo")).Returns(true);
            var app =
                new Browser(new FakeNancyBootstrapper(mockBrightstar.Object,
                                                      new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.CreateStore)));
            
            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(new CreateStoreRequestObject("foo", PersistenceType.AppendOnly));
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        }

        [Test]
        public void TestCannotProvideNullStoreName()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            var app =
                new Browser(new FakeNancyBootstrapper(mockBrightstar.Object,
                                                      new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.CreateStore)));

            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(new CreateStoreRequestObject());
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void TestInvalidStoreNameGivesBadRequest()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.CreateStore("/invalid/store_name")).Throws<ArgumentException>();
            var app =
                new Browser(new FakeNancyBootstrapper(mockBrightstar.Object,
                                                      new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.CreateStore)));

            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(new CreateStoreRequestObject("/invalid/store_name"));
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public void TestPersistenceTypeIsOptional()
        {
            // Setup
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.DoesStoreExist("foo")).Returns(false);
            mockBrightstar.Setup(s => s.CreateStore("foo")).Verifiable();
            var app =
                new Browser(new FakeNancyBootstrapper(mockBrightstar.Object,
                                                      new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.CreateStore)));

            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(new CreateStoreRequestObject("foo"));
                with.AjaxRequest();
            });

            mockBrightstar.Verify();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.ContentType, Contains.Substring("application/json"));
            Assert.That(response.Body, Is.Not.Null);
            var responseContent = response.Body.DeserializeJson<StoreResponseModel>();
            Assert.That(responseContent, Is.Not.Null);
            Assert.That(responseContent, Has.Property("Name").EqualTo("foo"));
            Assert.That(responseContent, Has.Property("Jobs").EqualTo("foo/jobs"));            
        }

        [Test]
        public void TestPostRequiresCreateStorePermission()
        {
            var brightstar = new Mock<IBrightstarService>();
            var systemPermissions = new Mock<AbstractSystemPermissionsProvider>();
            systemPermissions.Setup(s=>s.HasPermissions(null, SystemPermissions.CreateStore)).Returns(false).Verifiable();
            var app =
                new Browser(new FakeNancyBootstrapper(brightstar.Object, new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                      systemPermissions.Object));
            // Execute
            var response = app.Post("/", with =>
            {
                with.Accept(new MediaRange("application/json"));
                with.JsonBody(new CreateStoreRequestObject("foo"));
                with.AjaxRequest();
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            systemPermissions.Verify();
        }
    }
}
