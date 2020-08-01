namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    internal interface IRelatedResource
    {
        ulong ResourceId { get; }
        ulong PredicateId { get; }
        int GraphId { get; }
    }
}