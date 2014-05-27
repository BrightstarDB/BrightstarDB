using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    interface IPageStoreResourceIndex : IResourceIndex, IPageStoreObject
    {
        /// <summary>
        /// Preload pages from the index store into the in-memory page cache
        /// </summary>
        /// <param name="numPages"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        int Preload(int numPages, BrightstarProfiler profiler);
    }
}
