using System.Configuration;
using BrightstarDB.Server.Modules.Permissions;

namespace BrightstarDB.Server.Runner
{
    public class BrightstarServiceConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("connectionString")]
        public string ConnectionString
        {
            get { return (string) this["connectionString"]; }
            set { this["connectionString"] = value; }
        }

        [ConfigurationProperty("anonStorePermissions")]
        public StorePermissions AnonymousStorePermissions
        {
            get { return (StorePermissions) this["anonStorePermissions"]; }
            set { this["anonStorePermissions"] = value; }
        }

        [ConfigurationProperty("anonSystemPermissions")]
        public SystemPermissions AnonymousSystemPermissions
        {
            get { return (SystemPermissions) this["anonSystemPermissions"]; }
            set { this["anonSystemPermissions"] = value; }
        }
    }
}
