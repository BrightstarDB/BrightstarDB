using BrightstarDB.Client;
using Nancy;

namespace BrightstarDB.Server.Modules.Tests
{
    public class FakeNancyBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IBrightstarService _brightstarService;
        private readonly IStorePermissionsProvider _storePermissionsProvider;
        private readonly ISystemPermissionsProvider _systemPermissionsProvider;

        public FakeNancyBootstrapper(IBrightstarService brightstarService) : this(
            brightstarService, new PassAllStorePermissionsProvider(true))
        {
        }

        public FakeNancyBootstrapper(IBrightstarService brightstarService,
                                     IStorePermissionsProvider storePermissionsProvider)
            : this(brightstarService, storePermissionsProvider, new PassAllSystemPermissionsProvider())
        {
            
        }

        public FakeNancyBootstrapper(IBrightstarService brightstarService,
                                     IStorePermissionsProvider storePermissionsProvider,
            ISystemPermissionsProvider systemPermissionsProvider)
        {
            _brightstarService = brightstarService;
            _storePermissionsProvider = storePermissionsProvider;
            _systemPermissionsProvider = systemPermissionsProvider;
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register<IBrightstarService>(_brightstarService);
            container.Register<IStorePermissionsProvider>(_storePermissionsProvider);
            container.Register(_systemPermissionsProvider);
        }
    }
}
