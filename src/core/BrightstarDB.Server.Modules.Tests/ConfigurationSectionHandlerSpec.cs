using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using BrightstarDB.Server.Modules.Permissions;
using Moq;
using NUnit.Framework;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Tests
{
    [TestFixture]
    public class ConfigurationSectionHandlerSpec
    {
        private const string SimpleConfiguration = @"
<brightstarService connectionString='type=embedded;storesDirectory=c:\brightstar\'> 
    <storePermissions>
        <fallback authenticated='All' anonymous='Read'/>
    </storePermissions>
    <systemPermissions>
        <fallback authenticated='All' anonymous='ListStores'/>
    </systemPermissions>
</brightstarService>";

        private const string FallbackDefaultsConfiguration = @"
<brightstarService connectionString='type=embedded;storesDirectory=c:\brightstar\'> 
    <storePermissions>
        <fallback authenticated='Read,Export'/>
    </storePermissions>
    <systemPermissions>
        <fallback authenticated='ListStores'/>
    </systemPermissions>
</brightstarService>";

        [Test]
        public void TestSimpleConfigurationSection()
        {
            var xml = new XmlDocument();
            xml.LoadXml(SimpleConfiguration);
            var handler = new BrightstarServiceConfigurationSectionHandler();
            var config = handler.Create(null, null, xml.DocumentElement) as BrightstarServiceConfiguration;
            var mockUser = new Mock<IUserIdentity>();

            Assert.That(config, Is.Not.Null);
            Assert.That(config.ConnectionString, Is.Not.Null);
            Assert.That(config.ConnectionString, Is.EqualTo("type=embedded;storesDirectory=c:\\brightstar\\"));
            Assert.That(config.StorePermissionsProvider, Is.Not.Null);
            Assert.That(config.StorePermissionsProvider, Is.InstanceOf<FallbackStorePermissionsProvider>());
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(null, "foo"), Is.EqualTo(StorePermissions.Read));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(mockUser.Object, "foo"), Is.EqualTo(StorePermissions.All));
            Assert.That(config.SystemPermissionsProvider, Is.Not.Null);
            Assert.That(config.SystemPermissionsProvider, Is.InstanceOf<FallbackSystemPermissionsProvider>());
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(null), Is.EqualTo(SystemPermissions.ListStores));
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(mockUser.Object), Is.EqualTo(SystemPermissions.All));
        }

        [Test]
        public void TestFallbackDefaults()
        {
            var xml = new XmlDocument();
            xml.LoadXml(FallbackDefaultsConfiguration);
            var handler = new BrightstarServiceConfigurationSectionHandler();
            var config = handler.Create(null, null, xml.DocumentElement) as BrightstarServiceConfiguration;
            var mockUser = new Mock<IUserIdentity>();

            Assert.That(config, Is.Not.Null);
            Assert.That(config.ConnectionString, Is.Not.Null);
            Assert.That(config.ConnectionString, Is.EqualTo("type=embedded;storesDirectory=c:\\brightstar\\"));
            Assert.That(config.StorePermissionsProvider, Is.Not.Null);
            Assert.That(config.StorePermissionsProvider, Is.InstanceOf<FallbackStorePermissionsProvider>());
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(null, "foo"), Is.EqualTo(StorePermissions.None));
            Assert.That(config.StorePermissionsProvider.GetStorePermissions(mockUser.Object, "foo"), Is.EqualTo(StorePermissions.Read|StorePermissions.Export));
            Assert.That(config.SystemPermissionsProvider, Is.Not.Null);
            Assert.That(config.SystemPermissionsProvider, Is.InstanceOf<FallbackSystemPermissionsProvider>());
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(null), Is.EqualTo(SystemPermissions.None));
            Assert.That(config.SystemPermissionsProvider.GetPermissionsForUser(mockUser.Object), Is.EqualTo(SystemPermissions.ListStores));
        }
    }
}
