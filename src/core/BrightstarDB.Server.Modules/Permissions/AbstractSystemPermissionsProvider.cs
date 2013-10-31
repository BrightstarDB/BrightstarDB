using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    public abstract class AbstractSystemPermissionsProvider
    {
        public abstract SystemPermissions GetPermissionsForUser(IUserIdentity user);

        public virtual bool HasPermissions(IUserIdentity user, SystemPermissions requestedPermissions)
        {
            return (GetPermissionsForUser(user) & requestedPermissions) == requestedPermissions;
        }
    }
}
