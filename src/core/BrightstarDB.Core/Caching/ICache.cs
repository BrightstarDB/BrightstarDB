namespace BrightstarDB.Caching
{
    /// <summary>
    /// Interface implemented by a Brightstar Cache
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Adds a new item to the cache
        /// </summary>
        /// <param name="key">The cache key for the item</param>
        /// <param name="data">The data to be stored as a byte array</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        void Insert(string key, byte[] data, CachePriority cachePriority);

        /// <summary>
        /// Looks for an item in the cache and returns the bytes for that item
        /// </summary>
        /// <param name="key">The cache key of the item</param>
        /// <returns>The bytes for the cached item or null if the item is not found in the cache</returns>
        byte[] Lookup(string key);

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">The cache key of the item to be removed</param>
        void Remove(string key);

        /// <summary>
        /// Returns true if the cache contains an entry with the specified key
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if the cache contains an entry with the specified key, false otherwise</returns>
        bool ContainsKey(string key);
    }
}
