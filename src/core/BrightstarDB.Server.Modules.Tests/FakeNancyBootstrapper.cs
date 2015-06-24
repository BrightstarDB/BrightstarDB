using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Configuration;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;

namespace BrightstarDB.Server.Modules.Tests
{
    public class FakeNancyBootstrapper : DefaultNancyBootstrapper
    {

        private readonly IBrightstarService _brightstarService;
        private readonly AbstractStorePermissionsProvider _storePermissionsProvider;
        private readonly AbstractSystemPermissionsProvider _systemPermissionsProvider;
        private readonly IAuthenticationProvider _authenticationProvider;

        public FakeNancyBootstrapper(IBrightstarService brightstarService) : this(
            brightstarService, new FallbackStorePermissionsProvider(StorePermissions.All, StorePermissions.All))
        {
        }

        public FakeNancyBootstrapper(IBrightstarService brightstarService,
                                     AbstractStorePermissionsProvider storePermissionsProvider)
            : this(brightstarService, storePermissionsProvider, new FallbackSystemPermissionsProvider(SystemPermissions.All))
        {
            
        }

        public FakeNancyBootstrapper(IBrightstarService brightstarService,
                                     AbstractStorePermissionsProvider storePermissionsProvider,
            AbstractSystemPermissionsProvider systemPermissionsProvider)
        {
            _brightstarService = brightstarService;
            _storePermissionsProvider = storePermissionsProvider;
            _systemPermissionsProvider = systemPermissionsProvider;
        }

        public FakeNancyBootstrapper(IBrightstarService brightstarService,
                                     IAuthenticationProvider authenticationProvider,
                                     AbstractStorePermissionsProvider storePermissionsProvider,
                                     AbstractSystemPermissionsProvider systemPermissionsProvider)
        {
            _brightstarService = brightstarService;
            _authenticationProvider = authenticationProvider;
            _systemPermissionsProvider = systemPermissionsProvider;
            _storePermissionsProvider = storePermissionsProvider;
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register<IBrightstarService>(_brightstarService);
            container.Register<AbstractStorePermissionsProvider>(_storePermissionsProvider);
            container.Register(_systemPermissionsProvider);
            if (_authenticationProvider != null) container.Register(_authenticationProvider);
        }

        protected override IRootPathProvider RootPathProvider
        {
            get
            {
                return new DefaultRootPathProvider();
            }
        }

        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            if (_authenticationProvider != null)
            {
                _authenticationProvider.Enable(pipelines);
            }
            pipelines.EnableCors(new CorsConfiguration());
        }
    }
}
