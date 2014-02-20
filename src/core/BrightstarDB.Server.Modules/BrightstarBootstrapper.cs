using System;
using System.Collections.Generic;
using System.Configuration;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Authentication;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.Conventions;
using Nancy.ViewEngines.Razor;

namespace BrightstarDB.Server.Modules
{
    public class BrightstarBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IBrightstarService _brightstarService;
        private readonly AbstractStorePermissionsProvider _storePermissionsProvider;
        private readonly AbstractSystemPermissionsProvider _systemPermissionsProvider;
        private readonly IEnumerable<IAuthenticationProvider> _authenticationProviders; 
        private readonly IRootPathProvider _rootPathProvider;

        public BrightstarBootstrapper()
        {
            var config = ConfigurationManager.GetSection("brightstarService") as BrightstarServiceConfiguration;
            if (config == null) throw new ConfigurationErrorsException(Strings.NoServiceConfiguration);

            _brightstarService = BrightstarService.GetClient(config.ConnectionString);
            _storePermissionsProvider = config.StorePermissionsProvider;
            _systemPermissionsProvider = config.SystemPermissionsProvider;
            _authenticationProviders = config.AuthenticationProviders;
        }

        /// <summary>
        /// Create a new bootstrapper from the specified configuration and root path configuration
        /// </summary>
        /// <param name="configuration">The service configuration</param>
        /// <param name="rootPath">The root path</param>
        public BrightstarBootstrapper(BrightstarServiceConfiguration configuration, string rootPath = null)
            : this(BrightstarService.GetClient(configuration.ConnectionString),
                   configuration.AuthenticationProviders,
                   configuration.StorePermissionsProvider,
                   configuration.SystemPermissionsProvider,
                   rootPath)
        {
        }

        /// <summary>
        /// Create a new bootstrapper from the specified configuration and root path configuration
        /// with an override for the IBrightstarService instance to be used
        /// </summary>
        /// <param name="service">The IBrightstarService instance to be used</param>
        /// <param name="configuration">The service configuration</param>
        /// <param name="rootPath">The root path</param>
        public BrightstarBootstrapper(IBrightstarService service,
                                      BrightstarServiceConfiguration configuration,
                                      string rootPath = null)
            : this(service,
                   configuration.AuthenticationProviders,
                   configuration.StorePermissionsProvider,
                   configuration.SystemPermissionsProvider,
                   rootPath)
        {

        }

        /// <summary>
        /// Creates a new bootstrapper that denies all anonymous access to the specified Brightstar service
        /// but grants all authenticated users full access to the service and all of its stores.
        /// </summary>
        /// <param name="brightstarService"></param>
        /// <param name="authenticationProviders">An enumeration of the authentication providers to be used by the service</param>
        public BrightstarBootstrapper(IBrightstarService brightstarService,
                                      IEnumerable<IAuthenticationProvider> authenticationProviders)
            : this(
                brightstarService,
                authenticationProviders,
                new FallbackStorePermissionsProvider(StorePermissions.All),
                new FallbackSystemPermissionsProvider(SystemPermissions.All))
        {
        }

        /// <summary>
        /// Creates a new bootstrapper with store access governed by the specified provider and default
        /// system access permissions (full control to authenticated users, no control to anonymous users)
        /// </summary>
        /// <param name="brightstarService"></param>
        /// <param name="authenticationProviders">An enumeration of the authentication providers to be used by the service</param>
        /// <param name="storePermissionsProvider"></param>
        public BrightstarBootstrapper(IBrightstarService brightstarService,
            IEnumerable<IAuthenticationProvider> authenticationProviders, AbstractStorePermissionsProvider storePermissionsProvider)
            : this(brightstarService, authenticationProviders, storePermissionsProvider, new FallbackSystemPermissionsProvider(SystemPermissions.All))
        {

        }

        /// <summary>
        /// Creates a new bootstrapper with store and system access goverened by the specified providers.
        /// </summary>
        /// <param name="brightstarService">The connection to the BrightstarDB stores</param>
        /// <param name="authenticationProviders">An enumeration of the authentication providers to be used by the service</param>
        /// <param name="storePermissionsProvider">The store permissions provider to be used by the service</param>
        /// <param name="systemPermissionsProvider">The system permissions provider to be used by the service</param>
        /// <param name="rootPath">The path to the directory containing the service Views and assets folder</param>
        public BrightstarBootstrapper(IBrightstarService brightstarService,
                                      IEnumerable<IAuthenticationProvider> authenticationProviders,
                                      AbstractStorePermissionsProvider storePermissionsProvider,
                                      AbstractSystemPermissionsProvider systemPermissionsProvider,
                                      string rootPath = null)
        {
            _brightstarService = brightstarService;
            _authenticationProviders = authenticationProviders;
            _storePermissionsProvider = storePermissionsProvider;
            _systemPermissionsProvider = systemPermissionsProvider;
            _rootPathProvider = (rootPath == null
                                     ? new DefaultRootPathProvider()
                                     : new FixedRootPathProvider(rootPath) as IRootPathProvider);
            //_rootPathProvider = new FixedRootPathProvider(rootPath);
        }

        protected override void ConfigureApplicationContainer(Nancy.TinyIoc.TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(_brightstarService);
            container.Register(_storePermissionsProvider);
            container.Register(_systemPermissionsProvider);
            foreach (var authenticationProvider in _authenticationProviders)
            {
                container.Register(authenticationProvider);
            }
            container.Register<RazorViewEngine>();
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            base.ConfigureConventions(nancyConventions);
            nancyConventions.StaticContentsConventions.Add(
                StaticContentConventionBuilder.AddDirectory("assets"));
            Nancy.Json.JsonSettings.MaxJsonLength = Int32.MaxValue;
        }

        protected override IRootPathProvider RootPathProvider
        {
            get
            {
                if (_rootPathProvider == null) return base.RootPathProvider;
                return _rootPathProvider;
            }
        }

        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
        }
    }
}
