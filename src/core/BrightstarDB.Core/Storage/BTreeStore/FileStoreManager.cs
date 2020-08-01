#if !PORTABLE
using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class FileStoreManager : AbstractStoreManager
    {
        public FileStoreManager(StoreConfiguration storeConfiguration) : base(storeConfiguration, new FilePersistenceManager())
        {
        }
    }
}
#endif