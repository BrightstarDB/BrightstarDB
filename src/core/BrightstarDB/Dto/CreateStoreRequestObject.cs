using BrightstarDB.Storage;

namespace BrightstarDB.Dto
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateStoreRequestObject
    {
        /// <summary>
        /// 
        /// </summary>
        public string StoreName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int PersistenceType { get; set; }
 
        /// <summary>
        /// 
        /// </summary>
        public CreateStoreRequestObject(){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="storeName"></param>
        public CreateStoreRequestObject(string storeName):this(storeName, null){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="persistenceType"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
