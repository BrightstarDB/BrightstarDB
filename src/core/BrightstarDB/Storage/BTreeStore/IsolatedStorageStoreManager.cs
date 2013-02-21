using BrightstarDB.Storage.Persistence;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class IsolatedStorageStoreManager : AbstractStoreManager
    {
        public IsolatedStorageStoreManager(StoreConfiguration storeConfiguration) : base(storeConfiguration, new IsolatedStoragePersistanceManager())
        {
            
        }
    }
}
