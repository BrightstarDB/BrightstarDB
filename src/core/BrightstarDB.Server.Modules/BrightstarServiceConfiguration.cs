using BrightstarDB.Server.Modules.Permissions;

namespace BrightstarDB.Server.Modules
{
    public class BrightstarServiceConfiguration
    {
        public string ConnectionString { get; set; }
        public AbstractStorePermissionsProvider StorePermissionsProvider { get; set; }
        public AbstractSystemPermissionsProvider SystemPermissionsProvider { get; set; }
        public string RootPath { get; set; }
    }
}