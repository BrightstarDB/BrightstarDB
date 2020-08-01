namespace BrightstarDB.Caching
{
    /// <summary>
    /// An implementation of the <see cref="ICache"/> interface that performs no caching.
    /// </summary>
    public class NullCache : ICache
    {
        #region Implementation of ICache

        /// <summary>
        /// Adds a new item to the cache
        /// </summary>
        /// <param name="key">The cache key for the item</param>
        /// <param name="data">The data to be stored as a byte array</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        public void Insert(string key, byte[] data, CachePriority cachePriority)
        {
            return;
        }

        /// <summary>
        /// Adds an object to the cache
        /// </summary>
        /// <param name="key">The cache key for the object</param>
        /// <param name="o">The object to be stored. Must be serializable.</param>
        /// <param name="cachePriority">The priority of the item in the cache</param>
        public void Insert(string key, object o, CachePriority cachePriority)
        {
            return;
        }

        /// <summary>
        /// Looks for an item in the cache and returns the bytes for that item
        /// </summary>
        /// <param name="key">The cache key of the item</param>
        /// <returns>The bytes for the cached item or null if the item is not found in the cache</returns>
        public byte[] Lookup(string key)
        {
            return null;
        }

        /// <summary>
        /// Looks up an object in the cache
        /// </summary>
        /// <typeparam name="T">The type of the object to look up</typeparam>
        /// <param name="key">The cache key of the object</param>
        /// <returns>The object found or null if the object was not found or does not match the specified type.</returns>
        public T Lookup<T>(string key)
        {
            return default(T);
        }

        /// <summary>
        /// Removes an item from the cache
        /// </summary>
        /// <param name="key">The cache key of the item to be removed</param>
        public void Remove(string key)
        {
            return;
        }

        /// <summary>
        /// Returns true if the cache contains an entry with the specified key
        /// </summary>
        /// <param name="key">The key to look for</param>
        /// <returns>True if the cache contains an entry with the specified key, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            return false;
        }

        #endregion
    }
}
