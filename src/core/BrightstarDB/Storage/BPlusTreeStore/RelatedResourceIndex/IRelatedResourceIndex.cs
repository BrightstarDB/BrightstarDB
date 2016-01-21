using System;
using System.Collections.Generic;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    interface IRelatedResourceIndex
    {
        /// <summary>
        /// Adds a related resource index entry
        /// </summary>
        /// <param name="txnId"> </param>
        /// <param name="resourceId">The resource ID of the "start" resource</param>
        /// <param name="predicateId">The resource ID of the predicate that relates the resources</param>
        /// <param name="relatedResourceId">The resource ID of the "related" resource</param>
        /// <param name="graphId">The resource ID of the graph containing the relationship</param>
        /// <param name="profiler"></param>
        void AddRelatedResource(ulong txnId, ulong resourceId, ulong predicateId, ulong relatedResourceId, int graphId, BrightstarProfiler profiler = null);

        /// <summary>
        /// Removes a related resource index entry
        /// </summary>
        /// <param name="txnId"> </param>
        /// <param name="resourceId">The resource ID of the "start" resource</param>
        /// <param name="predicateId">The resource ID of the predicate that relates the resources</param>
        /// <param name="relatedResourceId">The resource ID of the "related" resource to be removed</param>
        /// <param name="graphId">The resource ID of the graph containing the relationship</param>
        /// <param name="profiler"></param>
        void DeleteRelatedResource(ulong txnId, ulong resourceId, ulong predicateId, ulong relatedResourceId, int graphId, BrightstarProfiler profiler);


        /// <summary>
        /// Enumerates the resources related to a start resource by a specific predicate
        /// </summary>
        /// <param name="resourceId">The resource ID of the start resource</param>
        /// <param name="predicateId">The resource ID of the predicate that relates the resources. If set to Constants.NullUlong, returns relationships for all predicates</param>
        /// <param name="graphId">The resource ID of the graph containing the relationships. If set to Constants.NullUlong, returns relationships from all graphs</param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        IEnumerable<IRelatedResource> EnumerateRelatedResources(ulong resourceId, ulong predicateId = 0ul, int graphId = -1, BrightstarProfiler profiler = null);

        /// <summary>
        /// Returns true if the index contains the specified resource relationship
        /// </summary>
        /// <param name="resourceId"></param>
        /// <param name="predicateId"></param>
        /// <param name="relatedResourceId"></param>
        /// <param name="graphId"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        bool ContainsRelatedResource(ulong resourceId, ulong predicateId, ulong relatedResourceId, int graphId, BrightstarProfiler profiler);

        /// <summary>
        /// Enumerates over all of the predicate IDs managed by the related resource index
        /// </summary>
        /// <param name="profiler"></param>
        /// <returns></returns>
        IEnumerable<ulong> EnumeratePredicates(BrightstarProfiler profiler);

        /// <summary>
        /// Returns the number of relationships in the index for the specified predicate
        /// </summary>
        /// <param name="predicateId"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        ulong CountPredicateRelationships(ulong predicateId, BrightstarProfiler profiler);

        /// <summary>
        /// Returns a 2-tuple of the number of relationships in the index for the specified predicate
        /// and the number of distinct key resource ids for the specified predicate
        /// </summary>
        /// <param name="predicateId"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        Tuple<ulong, ulong> CountPredicateRelationshipsEx(ulong predicateId, BrightstarProfiler profiler);

        /// <summary>
        /// Enumerates all of the related resources stored for a single predicate
        /// </summary>
        /// <param name="predicateId"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        IEnumerable<IResourceRelationship> EnumeratePredicateRelationships(ulong predicateId, BrightstarProfiler profiler);

        /// <summary>
        /// Enumerates all relationships in the index that match the specified graph filter
        /// </summary>
        /// <param name="graphFilter"></param>
        /// <param name="profiler"></param>
        /// <returns></returns>
        IEnumerable<IResourceRelationship> EnumerateAll(Func<int, bool> graphFilter, BrightstarProfiler profiler);
    }
}
