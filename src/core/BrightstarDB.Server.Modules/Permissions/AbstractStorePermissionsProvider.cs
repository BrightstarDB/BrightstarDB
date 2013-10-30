using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// Interface to be implemented by the provider of store-level security permissions
    /// </summary>
    public abstract class AbstractStorePermissionsProvider
    {
        /// <summary>
        ///  Returns true if the specified user has the required permissions for the specified sotre
        ///  </summary><param name="userIdentity">The user identiy. This will be NULL for the anonymous user</param><param name="storeName">The name of the store on which permissions are requested</param><param name="permissionRequested">The requestd permissions</param><returns>True if the user has all of the requested permissions, false otherwise</returns>
        public virtual bool HasStorePermission(IUserIdentity userIdentity, string storeName,
                                       StorePermissions permissionRequested)
        {
            return (GetStorePermissions(userIdentity, storeName) & permissionRequested) == permissionRequested;
        }

        /// <summary>
        ///  Returns the effective permissions for the specified user on the specified store
        ///  </summary><param name="currentUser">The user identity. This will be NULL for the anonymous user</param><param name="storeName">The name of the store on which permissions are requested</param><returns>A flags enum consisting of all the effective permissions for the user on the named store</returns>
        public abstract StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName);
    }
}