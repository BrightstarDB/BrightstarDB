using Nancy.Security;

namespace BrightstarDB.Server.Modules
{
    public class PassAllStorePermissionsProvider : IStorePermissionsProvider
    {
        private readonly bool _allowAnonymousAccess;

        public PassAllStorePermissionsProvider(bool allowAnonymousAccess)
        {
            _allowAnonymousAccess = allowAnonymousAccess;
        }

        public bool HasStorePermission(IUserIdentity userIdentity, string storeName, StorePermissions permissionRequested)
        {
            if (userIdentity == null) return _allowAnonymousAccess;
            return true;
        }

        public StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            if (currentUser == null && !_allowAnonymousAccess) return StorePermissions.None;
            return StorePermissions.All;
        }
    }
}
