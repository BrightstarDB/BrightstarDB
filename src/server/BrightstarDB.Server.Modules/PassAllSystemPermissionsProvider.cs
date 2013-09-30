using Nancy.Security;

namespace BrightstarDB.Server.Modules
{
    public class PassAllSystemPermissionsProvider : ISystemPermissionsProvider
    {
        private readonly SystemPermissions _anonymousPermissions;

        public PassAllSystemPermissionsProvider(SystemPermissions anonymousUserPermissions = SystemPermissions.None)
        {
            _anonymousPermissions = anonymousUserPermissions;
        }

        public SystemPermissions GetPermissionsForUser(IUserIdentity user)
        {
            if (user == null) return _anonymousPermissions;
            return SystemPermissions.All;
        }

        public bool HasPermissions(IUserIdentity user, SystemPermissions requestedPermissions)
        {
            return (GetPermissionsForUser(user) & requestedPermissions) == requestedPermissions;
        }
    }
}