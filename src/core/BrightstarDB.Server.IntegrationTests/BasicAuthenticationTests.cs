#if !PORTABLE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Configuration;
using BrightstarDB.Server.Modules.Permissions;
using NUnit.Framework;
using Nancy.Authentication.Basic;
using Nancy.Security;

namespace BrightstarDB.Server.IntegrationTests
{
    [TestFixture]
    public class BasicAuthenticationTests : ClientTestBase
    {
        public BasicAuthenticationTests()
            : base(new BrightstarBootstrapper(
                BrightstarService.GetClient(),
                new IAuthenticationProvider[]
                {
                    new BasicAuthAuthenticationProvider(
                        new BasicAuthenticationConfiguration(
                            new StaticUserValidator(new Dictionary<string, string>
                            {
                                {"alice", "password"},
                                {"bob", "bob123"}
                            }), "test")),
                },
                new StaticStorePermissionsProvider(
                    new Dictionary<string, Dictionary<string, StorePermissions>>
                    {
                        {
                            "foo",
                            new Dictionary<string, StorePermissions>
                            {
                                {"alice", StorePermissions.All}
                            }
                        },
                        {
                            "bar",
                            new Dictionary<string, StorePermissions> {{"bob", StorePermissions.All}}
                        }
                    },
                    new Dictionary<string, Dictionary<string, StorePermissions>>()),
                new StaticSystemPermissionsProvider(
                    new Dictionary<string, SystemPermissions>
                    {
                        {"alice", SystemPermissions.ListStores},
                        {"bob", SystemPermissions.All}
                    },
                    new Dictionary<string, SystemPermissions>()),
                new CorsConfiguration()))
        {
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            StartService();
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            CloseService();
        }

        private static IBrightstarService GetClient()
        {
            return BrightstarService.GetClient("type=rest;endpoint=http://localhost:8090/brightstar");
        }

        private static IBrightstarService GetClient(string userName, string password)
        {
            return
                BrightstarService.GetClient("type=rest;endpoint=http://localhost:8090/brightstar;userName=" + userName +
                                            ";password=" + password);
        }

        [Test]
        public void TestAccessRequiresAuthentication()
        {
            var client = GetClient();
            Assert.Throws<BrightstarClientException>(() => client.ListStores());
        }

        [Test]
        public void TestPasswordValidation()
        {
            var client = GetClient("alice", "invalidpassword");
            Assert.Throws<BrightstarClientException>(() => client.ListStores());
        }

        [Test]
        public void TestAccessWithValidPassword()
        {
            var client = GetClient("alice", "password");
            client.ListStores();
        }

        [Test]
        public void TestSystemPermissionsChecked()
        {
            var client = GetClient("alice", "password");
            Assert.Throws<BrightstarClientException>(() => client.CreateStore("AlicesStore_" + DateTime.Now.Ticks));
        }

        [Test]
        public void TestSystemPermissionAllowsCreate()
        {
            var client = GetClient("bob", "bob123");
            var doesStoreExist = client.DoesStoreExist("bar");
            if (doesStoreExist)
            {
                client.DeleteStore("bar");
            }
            client.CreateStore("bar");
            Assert.IsTrue(client.DoesStoreExist("bar"));
        }

        [Test]
        public void TestBobCanCreateAndAliceCanDelete()
        {
            var bobClient = GetClient("bob", "bob123");
            var aliceClient = GetClient("alice", "password");

            // Alice can test for store existence -- TODO: surely bob should be allowed to do this too
            if (aliceClient.DoesStoreExist("foo"))
            {
                // Alice can delete a store that she has full control over
                aliceClient.DeleteStore("foo");
            }

            try
            {
                // Alice cannot create her store
                aliceClient.CreateStore("foo");
                Assert.Fail("Expected a BrightstarClientException when alice tries to create her own stor.");
            }
            catch (BrightstarClientException)
            {
                // Expected
                
            }

            // But bob can do it for her
            bobClient.CreateStore("foo");

            // Now alice can access her store
            aliceClient.ExecuteQuery("foo", "SELECT DISTINCT ?t WHERE {?x a ?t}");

            // But bob cannot
            try
            {
                bobClient.ExecuteQuery("foo", "SELECT DISTINCT ?t WHERE (?x a ?t)");
                Assert.Fail("Expected a BrightstarClientException when bob tries to query alice's store");
            }
            catch (BrightstarClientException)
            {
                // Expected
            }
        }

    }

    internal class StaticUserValidator : IUserValidator
    {
        private readonly Dictionary<string, string> _passwords; 
        public StaticUserValidator(Dictionary<string, string> passwords)
        {
            _passwords = new Dictionary<string, string>(passwords);
        }

        public IUserIdentity Validate(string username, string password)
        {
            string pwd;
            return _passwords.TryGetValue(username, out pwd) && pwd.Equals(password)
                       ? new StaticUserIdentity(username)
                       : null;
        }
    }

    internal class StaticUserIdentity : IUserIdentity
    {
        public StaticUserIdentity(string userName)
        {
            UserName = userName;
            Claims = new string[0];
        }

        public string UserName { get; private set; }
        public IEnumerable<string> Claims { get; private set; }
    }

}
#endif
