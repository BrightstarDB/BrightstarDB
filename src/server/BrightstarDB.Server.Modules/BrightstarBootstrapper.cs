using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class BrightstarBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IBrightstarService _brightstarService;
        private readonly AbstractStorePermissionsProvider _storePermissionsProvider;
        private readonly AbstractSystemPermissionsProvider _systemPermissionsProvider;

        /// <summary>
        /// Creates a new bootstrapper that denies all anonymous access to the specified Brightstar service
        /// but grants all authenticated users full access to the service and all of its stores.
        /// </summary>
        /// <param name="brightstarService"></param>
        public BrightstarBootstrapper(IBrightstarService brightstarService)
            : this(
                brightstarService, 
            new PassAllStorePermissionsProvider(),
            new PassAllSystemPermissionsProvider())
        {
        }

        /// <summary>
        /// Creates a new bootstrapper with store access governed by the specified provider and default
        /// system access permissions (full control to authenticated users, no control to anonymous users)
        /// </summary>
        /// <param name="brightstarService"></param>
        /// <param name="storePermissionsProvider"></param>
        public BrightstarBootstrapper(IBrightstarService brightstarService,
                                      AbstractStorePermissionsProvider storePermissionsProvider)
            : this(brightstarService, storePermissionsProvider, new PassAllSystemPermissionsProvider())
        {

        }

        /// <summary>
        /// Creates a new bootstrapper with store and system access goverened by the specified providers.
        /// </summary>
        /// <param name="brightstarService"></param>
        /// <param name="storePermissionsProvider"></param>
        /// <param name="systemPermissionsProvider"></param>
        public BrightstarBootstrapper(IBrightstarService brightstarService,
                                      AbstractStorePermissionsProvider storePermissionsProvider,
                                      AbstractSystemPermissionsProvider systemPermissionsProvider)
        {
            _brightstarService = brightstarService;
            _storePermissionsProvider = storePermissionsProvider;
            _systemPermissionsProvider = systemPermissionsProvider;
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(_brightstarService);
            container.Register(_storePermissionsProvider);
            container.Register(_systemPermissionsProvider);
        }
    }
}
