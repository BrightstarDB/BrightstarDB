namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    interface IResourceCache
    {
        void Add(ulong resourceId, IResource resource);
        bool TryGetValue(ulong resourceId, out IResource resource);
        void Clear();
    }
}
