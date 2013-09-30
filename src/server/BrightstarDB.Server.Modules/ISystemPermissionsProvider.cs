using Nancy.Security;

namespace BrightstarDB.Server.Modules
{
    public interface ISystemPermissionsProvider
    {
        SystemPermissions GetPermissionsForUser(IUserIdentity user);
        bool HasPermissions(IUserIdentity user, SystemPermissions requestedPermissions);
    }
}
