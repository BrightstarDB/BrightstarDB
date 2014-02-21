using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Permissions;
using Moq;
using NUnit.Framework;
using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Responses.Negotiation;
using Nancy.Security;
using Nancy.Testing;

namespace BrightstarDB.Server.Modules.Tests.Authentication
{
    [TestFixture]
    public class BasicAuthProviderSpec
    {
        [Test]
        public void TestAccessRequiresBasicAuthentication()
        {
            var permissions = new Mock<AbstractSystemPermissionsProvider>();
            permissions.Setup(p => p.HasPermissions(null, SystemPermissions.ListStores)).Returns(false);
            var userValidator = new Mock<IUserValidator>();
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[0]);
            var bootstrapper = new FakeNancyBootstrapper(mockBrightstar.Object, new BasicAuthAuthenticationProvider(new BasicAuthenticationConfiguration(userValidator.Object, "test")),
                                                new FallbackStorePermissionsProvider(StorePermissions.All),
                                                permissions.Object);
            var app = new Browser(bootstrapper);

            var response = app.Get("/", c => c.Accept(MediaRange.FromString("application/json")));
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

            permissions.VerifyAll();
            userValidator.VerifyAll();
            mockBrightstar.Verify(x=>x.ListStores(), Times.Never());
        }

        [Test]
        public void TestAccessUsingBasicAuthentication()
        {
            var permissions = new Mock<AbstractSystemPermissionsProvider>();
            permissions.Setup(
                p =>
                p.HasPermissions(It.Is<IUserIdentity>(x => x != null && x.UserName.Equals("alice")), SystemPermissions.ListStores))
                       .Returns(true);
            var userValidator = new Mock<IUserValidator>();
            userValidator.Setup(v => v.Validate("alice", "password"))
                         .Returns(new MockUserIdentity("alice", new string[0]));
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[0]);
            var bootstrapper = new FakeNancyBootstrapper(mockBrightstar.Object, new BasicAuthAuthenticationProvider(new BasicAuthenticationConfiguration(userValidator.Object, "test")),
                                                new FallbackStorePermissionsProvider(StorePermissions.All),
                                                permissions.Object);
            var app = new Browser(bootstrapper);

            var response = app.Get("/", c =>
            {
                c.BasicAuth("alice", "password");
                c.Accept(MediaRange.FromString("application/json"));
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            permissions.VerifyAll();
            userValidator.VerifyAll();
            mockBrightstar.VerifyAll();
        }

        [Test]
        public void TestAccessRequiresValidatedPassword()
        {
            var permissions = new Mock<AbstractSystemPermissionsProvider>();
            permissions.Setup(
                p =>
                p.HasPermissions(It.Is<IUserIdentity>(x => x != null && x.UserName.Equals("alice")), SystemPermissions.ListStores))
                       .Returns(true);
            var userValidator = new Mock<IUserValidator>();
            userValidator.Setup(v => v.Validate("alice", "invalidpassword"))
                         .Returns((IUserIdentity)null);
            var mockBrightstar = new Mock<IBrightstarService>();
            mockBrightstar.Setup(s => s.ListStores()).Returns(new string[0]);
            var bootstrapper = new FakeNancyBootstrapper(mockBrightstar.Object, new BasicAuthAuthenticationProvider(new BasicAuthenticationConfiguration(userValidator.Object, "test")),
                                                new FallbackStorePermissionsProvider(StorePermissions.All),
                                                permissions.Object);
            var app = new Browser(bootstrapper);

            var response = app.Get("/", c =>
            {
                c.BasicAuth("alice", "invalidpassword");
                c.Accept(MediaRange.FromString("application/json"));
            });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));

            userValidator.VerifyAll();
            permissions.Verify(x=>x.HasPermissions(null, SystemPermissions.ListStores), Times.Once());
            permissions.Verify(x=>x.HasPermissions(It.IsNotNull<IUserIdentity>(), SystemPermissions.ListStores), Times.Never());
            mockBrightstar.Verify(x => x.ListStores(), Times.Never());
            
        }
    }
}
