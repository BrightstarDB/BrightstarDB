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
                var service = serviceArgs.ConnectionString == null
                                  ? BrightstarService.GetClient()
                                  : BrightstarService.GetClient(serviceArgs.ConnectionString);
                return new BrightstarBootstrapper(service,
                                                  new PassAllStorePermissionsProvider(
                                                      serviceArgs.AnonymousStorePermissions),
                                                  new PassAllSystemPermissionsProvider(
                                                      serviceArgs.AnonymousSystemPermissions),
                                                      serviceArgs.RootPath);
            }
            catch (Exception ex)
            {
                throw new BootstrapperException("Error initializing BrightstarDB server: " + ex.Message, ex);
            }
        }
    }
}
