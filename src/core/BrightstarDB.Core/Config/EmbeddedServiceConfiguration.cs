namespace BrightstarDB.Config
{
    /// <summary>
    /// Represents the contents of the "brightstar" configuration section in an app.config or
    /// web.config file.
    /// </summary>
    public class EmbeddedServiceConfiguration
    {
        /// <summary>
        /// Initialize a new instance of the embedded service configuration options
        /// </summary>
        /// <param name="preloadConfiguration">OPTIONAL: Initial value of the <see cref="PreloadConfiguration"/> configuration option. Defaults to null (no page cache preload)</param>
        /// <param name="enableTransactionLoggingOnNewStores">OPTIONAL: Initial value of the <see cref="EnableTransactionLoggingOnNewStores"/> flag. Defaults to true.</param>
        public EmbeddedServiceConfiguration(
            PageCachePreloadConfiguration preloadConfiguration = null,
            bool enableTransactionLoggingOnNewStores = true)
        {
            PreloadConfiguration = preloadConfiguration;
            EnableTransactionLoggingOnNewStores = enableTransactionLoggingOnNewStores;
        }

        /// <summary>
        /// Get or set the page cache preload configuration
        /// </summary>
        public PageCachePreloadConfiguration PreloadConfiguration { get; set; }

        /// <summary>
        /// Get or set the flag that indicates if new stores should be created with 
        /// transaction logging enabled by default (true) or disabled by default (false).
        /// </summary>
        public bool EnableTransactionLoggingOnNewStores { get; set; }
    }
}
