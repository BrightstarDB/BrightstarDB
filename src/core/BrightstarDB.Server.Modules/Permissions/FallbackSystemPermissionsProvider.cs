using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// A permissions provider that provides a fixed set of permissions.
    /// </summary>
    /// <remarks>This provider can be used as a fallback provider when used in conjunction with a <see cref="CombiningSystemPermissionsProvider"/></remarks>
    public class FallbackSystemPermissionsProvider : AbstractSystemPermissionsProvider
    {
        private readonly SystemPermissions _authenticatedPermissions;
        private readonly SystemPermissions _anonymousPermissions;

        /// <summary>
        /// Creates a new provider that grants a fixed set of system permissions to 
        /// authenticated users and no system permissions to anonymous users.
        /// </summary>
        /// <param name="authenticatedPermissions">The fixed set of system permissions
        /// to be granted to authenticated users</param>
        public FallbackSystemPermissionsProvider(SystemPermissions authenticatedPermissions):this(authenticatedPermissions, SystemPermissions.None)
        {
        }

        /// <summary>
        /// Creates a new provider that grants a fixed set of system permissions to
        /// authenticated users and a different fixed set of system permissions to
        /// anonymous users
        /// </summary>
        /// <param name="authenticatedPermissions">The system permissions to be granted to authenticated users</param>
        /// <param name="anonymousPermissions">The system permissions to be granted to anonymous users</param>
        public FallbackSystemPermissionsProvider(SystemPermissions authenticatedPermissions,
                                                SystemPermissions anonymousPermissions)
        {
            _authenticatedPermissions = authenticatedPermissions;
            _anonymousPermissions = anonymousPermissions;
        }

        public override SystemPermissions GetPermissionsForUser(IUserIdentity user)
        {
            return user == null ? _anonymousPermissions : _authenticatedPermissions;
        }
    }
}