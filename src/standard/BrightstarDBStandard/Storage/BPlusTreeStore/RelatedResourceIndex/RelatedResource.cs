namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    internal class RelatedResource : IRelatedResource
    {
        public RelatedResource(ulong predicateId, int graphId, ulong resourceId)
        {
            ResourceId = resourceId;
            PredicateId = predicateId;
            GraphId = graphId;
        }

        #region Implementation of IRelatedResource

        public ulong ResourceId { get; private set; }

        public ulong PredicateId { get; private set; }

        public int GraphId { get; private set; }

        #endregion
    }
}
