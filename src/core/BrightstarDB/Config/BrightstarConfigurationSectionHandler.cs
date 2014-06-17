#if !PORTABLE && !WINDOWS_PHONE
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
            BrightstarConfiguration ret = null;
            if (section is XmlElement)
            {
                ret = new BrightstarConfiguration();
                var sectionEl = section as XmlElement;
                var preloadPages = sectionEl.GetElementsByTagName("preloadPages").Item(0) as XmlElement;
                if (preloadPages != null)
                {
                    ret.PreloadConfiguration = ProcessPreloadConfiguration(preloadPages);
                }
            }
            return ret;
        }

        private static PageCachePreloadConfiguration ProcessPreloadConfiguration(XmlElement preloadPages)
        {
            var enabled = preloadPages.GetAttribute("enabled");
            if (!enabled.ToLowerInvariant().Equals("true"))
            {
                return new PageCachePreloadConfiguration {Enabled = false};
            }
            var preloadConfiguration = new PageCachePreloadConfiguration {Enabled = true};
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
#endif