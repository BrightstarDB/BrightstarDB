using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    internal class ResourceRelationship :RelatedResource, IResourceRelationship
    {
        public ResourceRelationship(ulong resource, ulong predicate, ulong relatedResource, int graph) : base(predicate, graph, resource)
        {
            RelatedResource = relatedResource;
        }

        #region Implementation of IResourceRelationship

        /// <summary>
        /// Gets the other resource in the relationship
        /// </summary>
        public ulong RelatedResource { get; private set; }

        #endregion
    }
}
