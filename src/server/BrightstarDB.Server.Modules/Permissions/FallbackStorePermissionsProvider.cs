using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides a fixed set of permissions.
    /// </summary>
    /// <remarks>This provider can be used as a fallback provider when used in conjunction with a <see cref="CombiningStorePermissionsProvider"/></remarks>
    public class FallbackStorePermissionsProvider : AbstractStorePermissionsProvider
    {
        private readonly StorePermissions _fallbackPermissions;

        public FallbackStorePermissionsProvider(StorePermissions fallbackPermissions)
        {
            _fallbackPermissions = fallbackPermissions;
        }

        public override StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            return _fallbackPermissions;
        }
    }
}