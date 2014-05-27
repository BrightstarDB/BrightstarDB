using System;
using System.Collections.ObjectModel;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Configuration;
using BrightstarDB.Server.Modules.Permissions;
using Nancy.Bootstrapper;

namespace BrightstarDB.Server.Runner
{
    internal static class ServiceBootstrap
    {
        public static BrightstarBootstrapper GetBootstrapper(ServiceArgs serviceArgs)
        {
            try
            {
                var configuration =
                    System.Configuration.ConfigurationManager.GetSection("brightstarService") as
                    BrightstarServiceConfiguration ?? new BrightstarServiceConfiguration();

                // Connection string specified on the command line overrides the one in the app config file.
                if (serviceArgs.ConnectionString != null) configuration.ConnectionString = serviceArgs.ConnectionString;

                var service = BrightstarService.GetClient(configuration.ConnectionString);

                // Create suitable defaults if the configuration lacks them
                if (configuration.AuthenticationProviders == null)
                {
                    configuration.AuthenticationProviders = new Collection<IAuthenticationProvider>{new NullAuthenticationProvider()};
                }
                if (configuration.StorePermissionsProvider == null)
                {
                    configuration.StorePermissionsProvider = new FallbackStorePermissionsProvider(StorePermissions.All);
                }
                if (configuration.SystemPermissionsProvider == null)
                {
                    configuration.SystemPermissionsProvider = new FallbackSystemPermissionsProvider(SystemPermissions.All);
                }

                return new BrightstarBootstrapper(service,
                                                  configuration,
                                                  serviceArgs.RootPath);
            }
            catch (Exception ex)
            {
                throw new BootstrapperException("Error initializing BrightstarDB server: " + ex.Message, ex);
            }
        }
    }
}
