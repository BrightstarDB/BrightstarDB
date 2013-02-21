using BrightstarDB.Azure.Management.Accounts;

namespace BrightstarDB.Azure.Management
{
    /// <summary>
    /// Class representing an access grant on a store
    /// </summary>
    public class StoreAccessKey
    {
        /// <summary>
        /// Constructor to create an instance from the persistent IStoreAccess entity
        /// </summary>
        /// <param name="access"></param>
        public StoreAccessKey(IStoreAccess access)
        {
            StoreId = access.AccessStore.Id;
            AccountId = access.Grantee.Id;
            Access = (StoreAccessLevel) access.GrantLevel;
        }

        /// <summary>
        /// The ID of the store
        /// </summary>
        public string StoreId { get; private set; }
        /// <summary>
        /// The ID of the account that is granted access
        /// </summary>
        public string AccountId { get; private set; }
        /// <summary>
        /// The level of access granted
        /// </summary>
        public StoreAccessLevel Access { get; private set; }
    }
}