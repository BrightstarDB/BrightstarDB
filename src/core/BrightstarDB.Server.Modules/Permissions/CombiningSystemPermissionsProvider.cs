using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides the union of permissions from two other providers
    /// </summary>
    public class CombiningSystemPermissionsProvider : AbstractSystemPermissionsProvider
    {
        private readonly AbstractSystemPermissionsProvider _first;
        private readonly AbstractSystemPermissionsProvider _second;

        public CombiningSystemPermissionsProvider(AbstractSystemPermissionsProvider first,
                                                  AbstractSystemPermissionsProvider second)
        {
            _first = first;
            _second = second;
        }

        public override SystemPermissions GetPermissionsForUser(IUserIdentity user)
        {
            return _first.GetPermissionsForUser(user) | _second.GetPermissionsForUser(user);
        }

        public override bool HasPermissions(IUserIdentity user, SystemPermissions requestedPermissions)
        {
            return _first.HasPermissions(user, requestedPermissions) ||
                   _second.HasPermissions(user, requestedPermissions);
        }
    }
}