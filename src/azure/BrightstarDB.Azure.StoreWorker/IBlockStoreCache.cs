using BrightstarDB.Azure.Common;

namespace BrightstarDB.Azure.StoreWorker
{
    public interface IBlockStoreCache
    {
        BlockInfo Lookup(string path, long offset);
        void Insert(BlockInfo block);
    }
}