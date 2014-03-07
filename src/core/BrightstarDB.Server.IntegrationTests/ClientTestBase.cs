#if !PORTABLE
using System;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Permissions;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;

namespace BrightstarDB.Server.IntegrationTests
{
    public class ClientTestBase
    {
        private readonly INancyBootstrapper _bootstrapper;
        private static NancyHost _serviceHost;
        private static bool _closed;
        private static readonly object HostLock = new object();

        public ClientTestBase()
            : this(new BrightstarBootstrapper(
                       BrightstarService.GetClient(),
                       new IAuthenticationProvider[] {new NullAuthenticationProvider()},
                       new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All),
                       new FallbackSystemPermissionsProvider(SystemPermissions.All, SystemPermissions.All)))
        {
        }

        public ClientTestBase(INancyBootstrapper bootstrapper)
        {
            this._bootstrapper = bootstrapper;
        }

        protected void StartService()
        {
            StartServer();
        }

        protected void CloseService()
        {
            lock (HostLock)
            {
                _serviceHost.Stop();
                _closed = true;
            }
        }

        private void StartServer()
        {
            lock (HostLock)
            {
#if SDK_TESTS
    // We assume that the test framework starts up the service for us.
#else
                if (_serviceHost == null || _closed)
                {
                    _serviceHost = new NancyHost(_bootstrapper,
                                                 new HostConfiguration {AllowChunkedEncoding = false},
                                                 new Uri("http://localhost:8090/brightstar/"));
                    _serviceHost.Start();
                }
#endif
            }
        }
    }
}
#endif