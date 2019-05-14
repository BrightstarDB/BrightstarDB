using System;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    interface IResourceCache : IDisposable
    {
        void Add(ulong resourceId, IResource resource);
        bool TryGetValue(ulong resourceId, out IResource resource);
        void Clear();
    }
}
