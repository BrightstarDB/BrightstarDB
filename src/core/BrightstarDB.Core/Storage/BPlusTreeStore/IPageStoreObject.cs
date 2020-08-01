using BrightstarDB.Profiling;
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BPlusTreeStore
{
    interface IPageStoreObject
    {
        ulong Save(ulong transactionId, BrightstarProfiler profiler);
        ulong Write(IPageStore targetStore, ulong transactionId, BrightstarProfiler profiler);
    }
}
