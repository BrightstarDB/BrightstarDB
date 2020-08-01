using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore.RelatedResourceIndex
{
    internal class PredicateRelatedResourceIndex : BrightstarDB.Storage.BPlusTreeStore.BPlusTree
    {
        public PredicateRelatedResourceIndex(ulong txnId, IPageStore pageStore) : base(txnId, pageStore, RelatedResourceIndex.KeySize, 0){}
        public PredicateRelatedResourceIndex(IPageStore pageStore, ulong rootPageId) : base(pageStore, rootPageId, RelatedResourceIndex.KeySize, 0){}
    }
}