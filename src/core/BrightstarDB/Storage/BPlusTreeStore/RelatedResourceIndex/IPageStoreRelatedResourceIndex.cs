using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    interface IPageStoreRelatedResourceIndex : IRelatedResourceIndex, IPageStoreObject
    {
        void FlushCache();

        /// <summary>
        /// Preload pages of the index from storage
        /// </summary>
        /// <param name="maxPages">The maximum number of pages to preload</param>
        /// <param name="profiler">OPTIONAL: Profiling object to use to profile the preload phase</param>
        /// <returns>The number of pages actually loaded</returns>
        int Preload(int maxPages, BrightstarProfiler profiler = null);
    }
}
