using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using BrightstarDB.Config;

namespace BrightstarDB.InternalTests
{
    [TestFixture]
    public class ConfigurationSectionHandlerTests
    {
        [Test]
        public void TestTxnLoggingIsEnabledByDefault()
        {
            var configuration = ParseConfiguration("<brightstar></brightstar>");
            Assert.That(configuration, Has.Property("EnableTransactionLoggingOnNewStores").EqualTo(true));
        }

        [Test]
        public void TestTxnLoggingDisabled()
        {
            var configuration = ParseConfiguration("<brightstar><transactionLogging enabled='false'/></brightstar>");
            Assert.That(configuration, Has.Property("EnableTransactionLoggingOnNewStores").EqualTo(false));
        }

        [Test]
        public void TestCachePreloadIsNullByDefault()
        {
            var configuration = ParseConfiguration("<brightstar></brightstar>");
            Assert.That(configuration, Has.Property("PreloadConfiguration").With.Null);
        }

        [Test]
        public void TestCachePreloadEnabled()
        {
            var configuration = ParseConfiguration("<brightstar><preloadPages enabled='true'/></brightstar>");
            Assert.That(configuration.PreloadConfiguration.Enabled);
        }

        private static EmbeddedServiceConfiguration ParseConfiguration(string xml)
        {
            var handler = new BrightstarConfigurationSectionHandler();
            var configRoot = GetConfigurationRoot(xml);
            var configuration = handler.Create(null, null, configRoot);
            Assert.That(configuration, Is.Not.Null);
            Assert.That(configuration, Is.InstanceOf<EmbeddedServiceConfiguration>());
            return configuration as EmbeddedServiceConfiguration;
        }

        private static XmlElement GetConfigurationRoot(string configurationXml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(configurationXml);
            return doc.DocumentElement;
        }
    }
}
