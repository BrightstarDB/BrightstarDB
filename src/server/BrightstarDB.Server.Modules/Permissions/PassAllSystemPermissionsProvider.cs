using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    public class PassAllSystemPermissionsProvider : AbstractSystemPermissionsProvider
    {
        private readonly SystemPermissions _anonymousPermissions;

        public PassAllSystemPermissionsProvider(SystemPermissions anonymousUserPermissions = SystemPermissions.None)
        {
            _anonymousPermissions = anonymousUserPermissions;
        }

        public override SystemPermissions GetPermissionsForUser(IUserIdentity user)
        {
            if (user == null) return _anonymousPermissions;
            return SystemPermissions.All;
        }

    }
}