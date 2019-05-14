using System;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    [Obsolete("No longer in use. Could possibly be removed")]
    internal interface INodeCache
    {
        void Add(INode node);
        void Remove(INode node);
        void Clear();
        bool TryGetValue(ulong nodeId, out INode node);
    }
}