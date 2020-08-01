namespace BrightstarDB.Config
{
    /// <summary>
    /// Provides page-cache warmup configuration for a specific store.
    /// </summary>
    public class StorePreloadConfiguration
    {
        /// <summary>
        /// The relative proportion of the page cache to be pre-filled with pages for the specific store.
        /// </summary>
        public decimal CacheRatio { get; set; }
    }
}