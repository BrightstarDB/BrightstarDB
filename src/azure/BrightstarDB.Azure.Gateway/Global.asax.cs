using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Web;
using Microsoft.IdentityModel.Web.Configuration;

namespace BrightstarDB.Azure.Gateway
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        
        void OnServiceConfigurationCreated(object sender, ServiceConfigurationCreatedEventArgs e)
        {
            // Use the <serviceCertificate> to protect the cookies that are sent to the client
            List<CookieTransform> sessionTransforms =
                new List<CookieTransform>(
                    new CookieTransform[]
                        {
                            new DeflateCookieTransform(),
                            new RsaEncryptionCookieTransform(e.ServiceConfiguration.ServiceCertificate),
                            new RsaSignatureCookieTransform(e.ServiceConfiguration.ServiceCertificate)
                        });
            var sessionHandler = new SessionSecurityTokenHandler(sessionTransforms.AsReadOnly());
            e.ServiceConfiguration.SecurityTokenHandlers.AddOrReplace(sessionHandler);
        }
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Store REST API
            routes.MapRoute(
                "ApiStoreListRoute",
                "services/api/{subscription}",
                new {controller = "StoreRest", action = "StoreList"});

            routes.MapRoute(
                "ApiStoreRoute",
                "services/api/{subscription}/{storeId}",
                new { controller = "StoreRest", action = "Store"});


            routes.MapRoute(
                "ApiJobsResource",
                "services/api/{subscription}/{storeId}/jobs/{id}",
                new { controller = "JobsResource", action = "Default", id = UrlParameter.Optional });

            // Admin REST API
            routes.MapRoute(
                "ApiAccountRouteWithAction",
                "services/account/{accountId}/{action}",
                new { controller = "AccountsResource" });

            routes.MapRoute(
                "ApiAccountRoute",
                "services/account/{accountIdOrUserToken}",
                new {controller = "AccountsResource", action = "Default"}
                );

            routes.MapRoute(
                "ApiSubscriptionRoute",
                "services/account/{accountId}/subscriptions/{subscriptionId}",
                new {controller = "SubscriptionsResource", action = "Default", subscriptionId = UrlParameter.Optional});

            // Browser Stores management route
            routes.MapRoute("StoreListRoute",
                            "Stores",
                            new {controller = "Stores", action = "Index"});
            routes.MapRoute("StoreActionRoute",
                            "Stores/{id}/{action}",
                            new {controller = "Stores", action = "Manage"});

            // Browser job management route
            routes.MapRoute(
                "StoreJobsRoute",
                "Stores/{storeId}/Jobs/{jobId}",
                new {controller = "StoreJobs", action = "Detail"});

            // Admin routes
            routes.MapRoute(
                "AdminHomeRoute",
                "Admin",
                new {controller = "Admin", action = "Index"});
            routes.MapRoute(
                "AdminAccountsList",
                "Admin/Accounts",
                new {controller = "AccountsList", action = "Index"}
                );
            routes.MapRoute(
                "AdminAccountDetail",
                "Admin/Accounts/{id}/{action}",
                new {controller = "AccountAdmin", action = "Index"});

            // Default route
            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
            
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            AccountsRepositoryFactory.Initialize( new WebAccountsCache(Context.Cache));
            Error += new EventHandler(MvcApplication_Error);
            FederatedAuthentication.ServiceConfigurationCreated += OnServiceConfigurationCreated;
        }

// ReSharper disable InconsistentNaming
// Naming required to expose method to WSFederationAuthentication
        public void WSFederationAuthentication_RedirectingToIdentityProvider(object sender, RedirectingToIdentityProviderEventArgs e)
// ReSharper restore InconsistentNaming
        {
            Trace.TraceInformation("WSFederationAuthentication_RedirectingToIdentityProvider: RequestUrl=" + e.SignInRequestMessage.RequestUrl);
             if (e.SignInRequestMessage.RequestUrl.Contains("localhost") ||
                e.SignInRequestMessage.RequestUrl.Contains("127.0.0.1"))
            {
                e.SignInRequestMessage.HomeRealm = "https://localhost:444/";
            }
            else if (e.SignInRequestMessage.RequestUrl.Contains("service-staging"))
            {
                e.SignInRequestMessage.HomeRealm = "https://service-staging.brightstardb.com/";
            }
            
        }
        
        void MvcApplication_Error(object sender, EventArgs e)
        {
            Trace.TraceError("Unhandled exception in Gateway application: " + Server.GetLastError());
        }

        
    }
}