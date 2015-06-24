using System.Collections.Generic;
using BrightstarDB.Config;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Permissions;

namespace BrightstarDB.Server.Modules.Configuration
{
    public class BrightstarServiceConfiguration
    {
        public string ConnectionString { get; set; }
        public ICollection<IAuthenticationProvider> AuthenticationProviders { get; set; }
        public AbstractStorePermissionsProvider StorePermissionsProvider { get; set; }
        public AbstractSystemPermissionsProvider SystemPermissionsProvider { get; set; }
        public string RootPath { get; set; }
        public PageCachePreloadConfiguration PreloadConfiguration { get; set; }
        public CorsConfiguration CorsConfiguration { get; set; }
    }

    public class CorsConfiguration
    {
        public bool DisableCors;
        public string AllowOrigin = "*";
    }
}