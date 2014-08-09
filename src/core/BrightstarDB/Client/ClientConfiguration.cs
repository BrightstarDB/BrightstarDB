using BrightstarDB.Config;

namespace BrightstarDB.Client
{
    /// <summary>
    /// A configuration object that can be optionally passed when making a connection to a BrightstarDB service
    /// </summary>
    public class ClientConfiguration
    {
        private static ClientConfiguration _default;

        /// <summary>
        /// Get or set the page-cache warmup configuration.
        /// </summary>
        /// <remarks>This configuration is only used when creating an embedded connection to a store directory</remarks>
        public PageCachePreloadConfiguration PreloadConfiguration { get; set; }

        /// <summary>
        /// Get the default client configuration to be used when the caller provides no explicit configuration
        /// </summary>
        public static ClientConfiguration Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new ClientConfiguration {PreloadConfiguration = Configuration.PreloadConfiguration};
                }
                return _default;
            }
        }
    }
}
