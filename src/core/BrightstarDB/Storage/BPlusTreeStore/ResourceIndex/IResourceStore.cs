using BrightstarDB.Profiling;

namespace BrightstarDB.Storage.BPlusTreeStore.ResourceIndex
{
    interface IResourceStore
    {
        IResource CreateNew(ulong txnId, string resourceValue, bool isLiteral, ulong dataTypeId, ulong langCodeId, BrightstarProfiler profiler);
        IResource FromBTreeValue(byte[] btreeValue);
    }
}
