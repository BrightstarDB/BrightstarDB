#if !PORTABLE
using System;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Permissions;
using Nancy.Hosting.Self;

namespace BrightstarDB.Server.IntegrationTests
{
    public class ClientTestBase
    {
        private static NancyHost _serviceHost;
        private static bool _closed;
        private static readonly object HostLock = new object();

        protected static void StartService()
        {
            StartServer();
        }

        protected static void CloseService()
        {
            lock (HostLock)
            {
                _serviceHost.Stop();
                _closed = true;
            }
        }

        private static void StartServer()
        {
            lock (HostLock)
            {
#if SDK_TESTS
    // We assume that the test framework starts up the service for us.
#else
                if (_serviceHost == null || _closed)
                {
                    _serviceHost = new NancyHost(new BrightstarBootstrapper(
                                                     BrightstarService.GetClient(),
                                                     new IAuthenticationProvider[] { new NullAuthenticationProvider() },
                                                     new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                                                     new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.All)),
                                                     new HostConfiguration{AllowChunkedEncoding = false},
                                                 new Uri("http://localhost:8090/brightstar/"));
                    _serviceHost.Start();
                }
#endif
            }
        }
    }
}
#endif