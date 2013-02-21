namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    internal interface IResourceRelationship:IRelatedResource
    {
        /// <summary>
        /// Gets the other resource in the relationship
        /// </summary>
        ulong RelatedResource { get; }
    }
}