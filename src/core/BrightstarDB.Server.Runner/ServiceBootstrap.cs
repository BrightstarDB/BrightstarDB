using System;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules;
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
