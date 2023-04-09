using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;

namespace BrightstarDB.Config
{
    /// <summary>
    /// An app.config section handler for Brightstar embedded application configuration.
    /// </summary>
    public class BrightstarConfigurationSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Creates a configuration section handler.
        /// </summary>
        /// <returns>
        /// The created section handler object.
        /// </returns>
        /// <param name="parent">Parent object.</param><param name="configContext">Configuration context object.</param><param name="section">Section XML node.</param><filterpriority>2</filterpriority>
        public object Create(object parent, object configContext, XmlNode section)
        {
            if (!(section is XmlElement)) return null;
            var ret = new EmbeddedServiceConfiguration();
            var sectionEl = section as XmlElement;
            var preloadPages = sectionEl.GetElementsByTagName("preloadPages").Item(0) as XmlElement;
            if (preloadPages != null)
            {
                ret.PreloadConfiguration = ProcessPreloadConfiguration(preloadPages);
            }
            var txnLogging =
                sectionEl.GetElementsByTagName("transactionLogging").OfType<XmlElement>().FirstOrDefault();
            if (txnLogging != null)
            {
                ret.EnableTransactionLoggingOnNewStores = IsEnabled(txnLogging);
            }
            return ret;
        }

        private static bool IsEnabled(XmlElement moduleElement, bool defaultValue = false)
        {
            if (!moduleElement.HasAttribute("enabled"))
            {
                return defaultValue;
            }
            var enabled = moduleElement.GetAttribute("enabled");
            return enabled.ToLowerInvariant().Equals("true");
        }

        private static PageCachePreloadConfiguration ProcessPreloadConfiguration(XmlElement preloadPages)
        {
            var enabled = IsEnabled(preloadPages);
            var preloadConfiguration = new PageCachePreloadConfiguration {Enabled = enabled};
            decimal defaultRatio;
            preloadConfiguration.DefaultCacheRatio = Decimal.TryParse(preloadPages.GetAttribute("defaultCacheRatio"),
                                                                      out defaultRatio)
                                                         ? defaultRatio
                                                         : 1.0m;
            preloadConfiguration.StorePreloadConfigurations = new Dictionary<string, StorePreloadConfiguration>();
            foreach (var storeConfiguration in preloadPages.GetElementsByTagName("store").OfType<XmlElement>())
            {
                var storeName = storeConfiguration.GetAttribute("name");
                decimal cacheRatio;
                if (!String.IsNullOrEmpty(storeName) &&
                    Decimal.TryParse(storeConfiguration.GetAttribute("cacheRatio"), out cacheRatio))
                {
                    preloadConfiguration.StorePreloadConfigurations[storeName] = new StorePreloadConfiguration
                        {
                            CacheRatio = cacheRatio
                        };
                }
            }
            return preloadConfiguration;
        }

    }
}