using System;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    internal interface IResourceTable : IDisposable
    {
        /// <summary>
        /// Retrieve the resource at the specified page and segment offset
        /// </summary>
        /// <param name="pageId">The ID of the page that holds the resource to be retrieved</param>
        /// <param name="segment">The index of the segment within the page that holds the start of the resource</param>
        /// <param name="profiler"></param>
        /// <returns>The resource</returns>
        string GetResource(ulong pageId, byte segment, BrightstarProfiler profiler);

        /// <summary>
        /// Add a resource to the table
        /// </summary>
        /// <param name="transactionId">The ID of the current transaction</param>
        /// <param name="resource">The resource to be added</param>
        /// <param name="pageId">Receives the ID of the page where the resource is stored</param>
        /// <param name="segment">Receives the index of the segment where the resource is stored</param>
        /// <param name="profiler"></param>
        void Insert(ulong transactionId, string resource, out ulong pageId, out byte segment, BrightstarProfiler profiler);

        void Commit(ulong transactionId, BrightstarProfiler profiler);
    }
}