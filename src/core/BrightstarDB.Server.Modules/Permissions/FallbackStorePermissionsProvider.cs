using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides a fixed set of permissions.
    /// </summary>
    /// <remarks>This provider can be used as a fallback provider when used in conjunction with a <see cref="CombiningStorePermissionsProvider"/></remarks>
    public class FallbackStorePermissionsProvider : AbstractStorePermissionsProvider
    {
        private readonly StorePermissions _authenticatedUserPermissions;
        private readonly StorePermissions _anonymousUserPermissions;

        /// <summary>
        /// Creates a provider with a fixed set of permissions for authenticated users, 
        /// an no permissions for anonymous users
        /// </summary>
        /// <param name="authenticatedUserPermissions">The fallback store permissions to be
        /// granted to authenticated users</param>
        public FallbackStorePermissionsProvider(StorePermissions authenticatedUserPermissions)
        {
            _authenticatedUserPermissions = authenticatedUserPermissions;
            _anonymousUserPermissions = StorePermissions.None;
        }

        /// <summary>
        /// Creates a provider with one fixed set of permissions for authenticated users
        /// and a different fixed set of permissions for anonymous users
        /// </summary>
        /// <param name="authenticatedUserPermissions">The fallback store permissions to be
        /// granted to authenticated users</param>
        /// <param name="anonymousUserPermissions">The fallback store permissions to be granted
        /// to anonymous users</param>
        public FallbackStorePermissionsProvider(StorePermissions authenticatedUserPermissions,
                                                StorePermissions anonymousUserPermissions)
        {
            _authenticatedUserPermissions = authenticatedUserPermissions;
            _anonymousUserPermissions = anonymousUserPermissions;
        }

        public override StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            return currentUser == null ? _anonymousUserPermissions : _authenticatedUserPermissions;
        }
    }

}