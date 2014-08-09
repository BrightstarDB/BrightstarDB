using BrightstarDB.Client;
using BrightstarDB.Storage;

namespace BrightstarDB.Portable.Tests.EntityFramework
{
    public class DataObjectStoreTestsBase : TestsBase
    {
        protected readonly IDataObjectContext _dataObjectContext;

        public DataObjectStoreTestsBase()
        {
            var connectionString = new ConnectionString("type=embedded;storesDirectory=" + TestConfiguration.StoreLocation);
            _dataObjectContext = new EmbeddedDataObjectContext(connectionString);
        }

        protected IDataObjectStore CreateStore(string storeName, PersistenceType persistenceType, bool withOptimisticLocking = false)
        {
            return _dataObjectContext.CreateStore(storeName, null, withOptimisticLocking, persistenceType);
        }

        protected IDataObjectStore OpenStore(string storeName, bool withOptimisticLocking = false)
        {
            return _dataObjectContext.OpenStore(storeName, null, withOptimisticLocking);
        }
    }
}