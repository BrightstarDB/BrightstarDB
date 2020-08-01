using System.Collections.Generic;

namespace BrightstarDB.Config
{
    /// <summary>
    /// Provides configuration parameters that govern page cache warm-up
    /// </summary>
    public class PageCachePreloadConfiguration
    {
        /// <summary>
        /// Boolean flag indicating if page cache warm-up is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// The default preload ratio for stores with no explicity preload configuration
        /// </summary>
        public decimal DefaultCacheRatio { get; set; }

        /// <summary>
        /// A dictionary mapping store names to their explicit preload configuration
        /// </summary>
        public Dictionary<string, StorePreloadConfiguration> StorePreloadConfigurations { get; set; }
    }
}