namespace BrightstarDB.Caching
{
    /// <summary>
    /// Interface for the policy class used to control cache evictions
    /// </summary>
    public interface ICacheEvictionPolicy
    {
        /// <summary>
        /// Runs this eviction policy against the specified cache
        /// </summary>
        /// <param name="cache">The cache to run against</param>
        /// <param name="target">The target number of bytes to evict</param>
        void Run(AbstractCache cache, long target);

        /// <summary>
        /// Initialize the eviction policy to run on the specified cache
        /// </summary>
        /// <param name="cache"></param>
        void Initialize(AbstractCache cache);

        /// <summary>
        /// Tracks cache inserts
        /// </summary>
        /// <param name="insertedKey">The inserted key</param>
        /// <param name="size">The size (in bytes) of the inserted value</param>
        /// <param name="priority">The priority assigned to the inserted cache item</param>
        void NotifyInsert(string insertedKey, long size, CachePriority priority);

        /// <summary>
        /// Tracks cache removals
        /// </summary>
        /// <param name="removedKey">The removed key</param>
        void NotifyRemove(string removedKey);

        /// <summary>
        /// Tracks cache lookups
        /// </summary>
        /// <param name="lookupKey">The key accessed</param>
        void NotifyLookup(string lookupKey);
    }
}