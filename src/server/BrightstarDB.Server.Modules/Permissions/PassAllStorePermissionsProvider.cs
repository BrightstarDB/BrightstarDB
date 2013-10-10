using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    public class PassAllStorePermissionsProvider : AbstractStorePermissionsProvider
    {
        private readonly StorePermissions _anonymousPermissions;

        public PassAllStorePermissionsProvider(StorePermissions anonymousPermissions = StorePermissions.None)
        {
            _anonymousPermissions = anonymousPermissions;
        }

        public override bool HasStorePermission(IUserIdentity userIdentity, string storeName, StorePermissions permissionRequested)
        {
            if (userIdentity == null) return (_anonymousPermissions & permissionRequested) == permissionRequested;
            return true;
        }

        public override StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            if (currentUser == null) return _anonymousPermissions;
            return StorePermissions.All;
        }
    }
}
