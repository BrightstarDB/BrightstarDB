using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides the union of permissions from two other providers
    /// </summary>
    public class CombiningStorePermissionsProvider : AbstractStorePermissionsProvider
    {
        private readonly AbstractStorePermissionsProvider _first;
        private readonly AbstractStorePermissionsProvider _second;

        public CombiningStorePermissionsProvider(AbstractStorePermissionsProvider first, AbstractStorePermissionsProvider second)
        {
            _first = first;
            _second = second;
        }

        public override StorePermissions GetStorePermissions(IUserIdentity currentUser, string storeName)
        {
            return _first.GetStorePermissions(currentUser, storeName) |
                   _second.GetStorePermissions(currentUser, storeName);
        }
    }
}
