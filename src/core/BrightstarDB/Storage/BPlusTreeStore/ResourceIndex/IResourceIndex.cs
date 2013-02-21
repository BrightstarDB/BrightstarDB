using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    internal interface IResourceIndex
    {
        /// <summary>
        /// Ensures that the specified resource is in the resource index and returns its resource ID
        /// </summary>
        /// <param name="txnId">The ID of the current update transaction</param>
        /// <param name="resourceValue">The resource string value</param>
        /// <param name="isLiteral">Boolean flag indicating if the resource is a literal (true) or a URI (false)</param>
        /// <param name="dataType">The resource data-type URI</param>
        /// <param name="langCode">The resource language string</param>
        /// <param name="addToCache">Boolean flag indicating if newly indexed resources should be added to the local cache</param>
        /// <param name="profiler"></param>
        /// <returns>The resource ID for the resource</returns>
        ulong AssertResourceInIndex(ulong txnId, string resourceValue, bool isLiteral = false, string dataType = null, string langCode = null, bool addToCache = true, BrightstarProfiler profiler = null);

        /// <summary>
        /// Retrieves the resource ID for the specified resource
        /// </summary>
        /// <param name="resourceValue">The resource string value</param>
        /// <param name="isLiteral">Boolean flag indicating if the resource is a literal (true) or a URI (false)</param>
        /// <param name="dataType">The resource data-type URI</param>
        /// <param name="langCode">The resource language string</param>
        /// <param name="addToCache">Boolean flag indicating if the retrieved resource ID should be added to the local cache</param>
        /// <param name="profiler">OPTIONAL: Profiler to use to profile this call</param>
        /// <returns>The resource ID for the resource or Constants.NullUlong if there is no match</returns>
        ulong GetResourceId(string resourceValue, bool isLiteral = false, string dataType = null,
                            string langCode = null, bool addToCache = true, BrightstarProfiler profiler = null);

        /// <summary>
        /// Retrieves the resource with the specified resource ID
        /// </summary>
        /// <param name="resourceId">The resource ID to look for</param>
        /// <param name="addToCache">Boolean flag indicating if the returned resource structure should be cached for faster future lookups</param>
        /// <param name="profiler">OPTIONAL: Profiler to use to profile this call</param>
        /// <returns>The corresponding resource or null if no match is found</returns>
        IResource GetResource(ulong resourceId, bool addToCache = true, BrightstarProfiler profiler = null);
    }
}
