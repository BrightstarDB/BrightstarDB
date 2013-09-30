using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    public class PassAllStorePermissionsProvider : AbstractStorePermissionsProvider
    {
        private readonly bool _allowAnonymousAccess;

        public PassAllStorePermissionsProvider(bool allowAnonymousAccess)
        {
            _allowAnonymousAccess = allowAnonymousAccess;
        }

        public override bool HasStorePermission(IUserIdentity userIdentity, string storeName, StorePermissions permissionRequested)
        {
            if (userIdentity == null) return _allowAnonymousAccess;
            return true;
        }

        public override StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            if (currentUser == null && !_allowAnonymousAccess) return StorePermissions.None;
            return StorePermissions.All;
        }
    }
}
