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
        /// Adds an object to the cache
        /// </summary>
        /// <param name="key">The cache key for the object</param>
        /// <param name="o">The object to be stored. Must be serializable.</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        void Insert(string key, object o, CachePriority cachePriority);

        /// <summary>
        /// Looks for an item in the cache and returns the bytes for that item
        /// </summary>
        /// <param name="key">The cache key of the item</param>
        /// <returns>The bytes for the cached item or null if the item is not found in the cache</returns>
        byte[] Lookup(string key);

        /// <summary>
        /// Looks up an object in the cache
        /// </summary>
        /// <typeparam name="T">The type of the object to look up</typeparam>
        /// <param name="key">The cache key of the object</param>
        /// <returns>The object found or null if the object was not found or does not match the specified type.</returns>
        T Lookup<T>(string key);

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
