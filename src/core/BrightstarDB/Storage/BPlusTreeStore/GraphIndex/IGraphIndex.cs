using System.Collections.Generic;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.GraphIndex
{
    internal interface IGraphIndex
    {
        /// <summary>
        /// Returns an enumeration over all graphs in the index
        /// </summary>
        /// <returns></returns>
        IEnumerable<GraphIndexEntry> EnumerateEntries();

        /// <summary>
        /// Return the URI for the graph with the specified ID
        /// </summary>
        /// <param name="graphId">The ID of the graph to lookup</param>
        /// <returns></returns>
        /// <remarks>Returns null if no graph exists with the specified URI or if the graph is marked as deleted</remarks>
        string GetGraphUri(int graphId);

        /// <summary>
        /// Finds or creates a new ID for the graph with the specified graph URI
        /// </summary>
        /// <param name="graphUri">The graph URI to lookup</param>
        /// <param name="profiler"></param>
        /// <returns>The ID assigned to the graph</returns>
        int AssertGraphId(string graphUri, BrightstarProfiler profiler = null);

        /// <summary>
        /// Finds the ID assigned to the graph with the specified graph URI
        /// </summary>
        /// <param name="graphUri">The graph URI to lookup</param>
        /// <param name="graphId">Receives the ID of the graph</param>
        /// <returns>True if an ID was found, false otherwise</returns>
        bool TryFindGraphId(string graphUri, out int graphId);

        /// <summary>
        /// Marks the graph with the specified graph id as deleted
        /// </summary>
        /// <param name="graphId"></param>
        void DeleteGraph(int graphId);

        /// <summary>
        /// Get boolean flag indicating if the index contains changes that need to be saved
        /// </summary>
        bool IsDirty { get; }

    }
}
