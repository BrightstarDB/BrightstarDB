using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BrightstarDB.Server.Modules.Configuration;
using BrightstarDB.Server.Modules.Permissions;
using NUnit.Framework;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class StaticPermissionsProvidersTests
    {
        private const string StorePermissionsConfiguration = @"<config><storePermissions><static>
          <store name=""foo""><user name=""alice"" permissions=""All""/><claim name=""admin"" permisions=""All""/></store>
          <store name=""bar""><user name=""alice"" permissions=""Read|Export""/><user name=""bob"" permissions=""All""/><claim name=""public"" permissions=""Read""/></store>
        </static></storePermissions></config>";

        private const string SystemPermissionsConfiguration =
            @"<config><systemPermissions><static><user name=""bob"" permissions=""All""/><claim name=""admin"" permissions=""All""/></static></systemPermissions></config>";

        public void TestLoadStorePermissionsConfiguration()
        {
            // Setup
            var doc = new XmlDocument();
            doc.LoadXml(StorePermissionsConfiguration);
            var alice = new MockUserIdentity("alice", new string[] { "public" });
            var bob = new MockUserIdentity("bob", new string[] { "admin" });
            var charles = new MockUserIdentity("charles", new string[] { "admin", "public" });
            var anon = new MockUserIdentity(null, new string[0]);

            // Execute
            var h = new BrightstarServiceConfigurationSectionHandler();
            var config = h.Create(null, null, doc.DocumentElement) as BrightstarServiceConfiguration;

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.StorePermissionsProvider, Is.Not.Null);
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(alice, "foo"), Is.EqualTo(StorePermissions.All));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(bob, "foo"), Is.EqualTo(StorePermissions.All));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(charles, "foo"), Is.EqualTo(StorePermissions.All));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(anon, "foo"), Is.EqualTo(StorePermissions.None));

            Assert.That(config.StorePermissionsProvider.GetStorePermissions(alice, "bar"), Is.EqualTo(StorePermissions.Read|StorePermissions.Export));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(bob, "bar"), Is.EqualTo(StorePermissions.All));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(charles, "bar"), Is.EqualTo(StorePermissions.Read));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(anon, "bar"), Is.EqualTo(StorePermissions.None));

            Assert.That(config.StorePermissionsProvider.GetStorePermissions(alice, "bletch"), Is.EqualTo(StorePermissions.None));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(bob, "bletch"), Is.EqualTo(StorePermissions.None));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(charles, "bletch"), Is.EqualTo(StorePermissions.None));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(anon, "bletch"), Is.EqualTo(StorePermissions.None));

        }

        [Test]
        public void TestLoadSystemPermissionsConfiguration()
        {
            // Setup
            var doc = new XmlDocument();
            doc.LoadXml(SystemPermissionsConfiguration);
            var alice = new MockUserIdentity("alice", new string[] {"public"});
            var bob = new MockUserIdentity("bob", new string[] {"admin"});
            var charles = new MockUserIdentity("charles", new string[] {"admin", "public"});
            var anon = new MockUserIdentity(null, new string[0]);

            // Execute
            var h = new BrightstarServiceConfigurationSectionHandler();
            var config = h.Create(null, null, doc.DocumentElement) as BrightstarServiceConfiguration;

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(alice), Is.EqualTo(SystemPermissions.None));
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(bob), Is.EqualTo(SystemPermissions.All));
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(charles), Is.EqualTo(SystemPermissions.All));
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(anon), Is.EqualTo(SystemPermissions.None));
        }
    }

    
}
