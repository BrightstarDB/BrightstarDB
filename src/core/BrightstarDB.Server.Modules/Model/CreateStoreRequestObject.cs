using BrightstarDB.Storage;

namespace BrightstarDB.Server.Modules.Model
{
    public class CreateStoreRequestObject
    {
        public string StoreName { get; set; }
        public int PersistenceType { get; set; }
 
        public CreateStoreRequestObject(){}
        public CreateStoreRequestObject(string storeName):this(storeName, null){}
        public CreateStoreRequestObject(string storeName, PersistenceType? persistenceType)
        {
            StoreName = storeName;
            if (persistenceType.HasValue)
            {
                PersistenceType = (int) persistenceType;
            }
            else
            {
                PersistenceType = -1;
            }
        }

        public PersistenceType? GetBrightstarPersistenceType()
        {
            switch (PersistenceType)
            {
                case 0:
                    return Storage.PersistenceType.AppendOnly;
                case 1:
                    return Storage.PersistenceType.Rewrite;
                default:
                    return null;
            }
        }
    }
}
