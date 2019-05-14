using System;
using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    interface IResourceStore : IDisposable
    {
        IResource CreateNew(ulong txnId, string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId, BrightstarProfiler profiler);
        IResource FromBTreeValue(byte[] btreeValue);
    }
}
