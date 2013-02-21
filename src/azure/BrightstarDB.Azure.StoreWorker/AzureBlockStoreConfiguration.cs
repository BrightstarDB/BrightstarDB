namespace BrightstarDB.Azure.StoreWorker
{
    /// <summary>
    /// Represents the collection of configuration values that can be applied to the block store system
    /// </summary>
    public class AzureBlockStoreConfiguration
    {
        /// <summary>
        /// Get / set the size of the in-memory cache
        /// </summary>
        public int MemoryCacheInMB { get; set; }

        /// <summary>
        /// Get / set the id of the local storage directory for the disk cache
        /// </summary>
        public string LocalStorageKey { get; set; }

        /// <summary>
        /// Get / set the Azure storage connection string
        /// </summary>
        public string ConnectionString { get; set; }

        public bool Disconnected { get; set; }
    }
}