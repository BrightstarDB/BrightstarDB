using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides a fixed set of permissions.
    /// </summary>
    /// <remarks>This provider can be used as a fallback provider when used in conjunction with a <see cref="CombiningSystemPermissionsProvider"/></remarks>
    public class FallbackSystemPermissionsProvider : AbstractSystemPermissionsProvider
    {
        private readonly SystemPermissions _fallbackPermissions;

        public FallbackSystemPermissionsProvider(SystemPermissions fallbackPermissions)
        {
            _fallbackPermissions = fallbackPermissions;
        }

        public override SystemPermissions GetPermissionsForUser(IUserIdentity user)
        {
            return _fallbackPermissions;
        }
    }
}