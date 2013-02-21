namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    interface IResourceIdCache
    {
        /// <summary>
        /// Returns the number of items currently held in the cache
        /// </summary>
        int CacheEntryCount { get; }

        /// <summary>
        /// Adds a new entry to the cache
        /// </summary>
        /// <param name="resourceHashString">The resource hash string</param>
        /// <param name="resourceId">The ID assigned to the resource</param>
        void Add(string resourceHashString, ulong resourceId);

        /// <summary>
        /// Looks up the ID for the specified resource
        /// </summary>
        /// <param name="resourceHashString">The resource hash string</param>
        /// <param name="resourceId">Receives the resource ID if a match is found</param>
        /// <returns>True if a match is found, false otherwise</returns>
        bool TryGetValue(string resourceHashString, out ulong resourceId);

        /// <summary>
        /// Clears the cache
        /// </summary>
        void Clear();
    }
}
