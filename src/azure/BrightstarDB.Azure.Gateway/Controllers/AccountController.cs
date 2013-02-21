using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.UI;
using BrightstarDB.Azure.Gateway.Hrd;
using BrightstarDB.Azure.Gateway.Models;
using BrightstarDB.Azure.Management;
using Microsoft.IdentityModel.Claims;
using Microsoft.IdentityModel.Protocols.WSFederation;
using Microsoft.IdentityModel.Web;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class AccountController : Controller
    {
        private HrdClient _hrdClient;

        public AccountController(HrdClient client)
        {
            _hrdClient = client;
        }

        public AccountController() : this(new HrdClient()){}

        [Authorize]
        [OutputCache(Location = OutputCacheLocation.None, NoStore = true, Duration = 0)]
        public ActionResult Index()
        {
            var identity = User.Identity as ClaimsIdentity;
            var userToken = GetAuthenticatedUserToken(identity);
            var accountDetails = AccountsRepositoryFactory.GetAccountsRepository().GetAccountDetailsForUser(userToken);
            return View(accountDetails);
        }

        [Authorize]
        public ActionResult Subscription(string subscriptionId)
        {
            var identity = User.Identity as ClaimsIdentity;
            var userToken = GetAuthenticatedUserToken(identity);
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            var account = repo.GetAccountDetailsForUser(userToken);
            var subscriptionDetail = repo.GetSubscriptionDetailsForUser(userToken, subscriptionId);
            if (subscriptionDetail == null)
            {
                return new HttpNotFoundResult();
            }
            var apiEndpoint = UrlHelper.GenerateUrl("ApiStoreListRoute", null, null,
                                                    new RouteValueDictionary(new {subscription=subscriptionDetail.Id}),
                                                    RouteTable.Routes, ControllerContext.RequestContext, false);
            var apiUrl = String.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Headers["Host"], apiEndpoint);
            return View("Subscription", new AccountAndSubscription(account, subscriptionDetail, apiUrl));
        }

        [Authorize]
        public ActionResult NewAccount()
        {
            //var identity = User.Identity as ClaimsIdentity;
            //var userToken = GetAuthenticatedUserToken(identity);
            var accountRegistration = new AccountRegistration();
            //AccountsRepositoryFactory.GetAccountsRepository().CreateAccount(userToken);
            //return RedirectToAction("Index");
            return View("Register", accountRegistration);
        }

        [Authorize]
        [HttpPost]
        public ActionResult NewAccount(AccountRegistration registration)
        {
            //ValidateModel(registration);
            if (!ModelState.IsValid)
            {
                return View("Register", registration);
            }
            var identity = User.Identity as ClaimsIdentity;
            var userToken = GetAuthenticatedUserToken(identity);
            AccountsRepositoryFactory.GetAccountsRepository().CreateAccount(userToken, registration.EmailAddress);
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult CreateTrial()
        {
            var identity = User.Identity as ClaimsIdentity;
            var userToken = GetAuthenticatedUserToken(identity);
            var accountsRepository = AccountsRepositoryFactory.GetAccountsRepository();
            var account = accountsRepository.GetAccountDetailsForUser(userToken);
            try
            {
                accountsRepository.CreateSubscription(account.AccountId, SubscriptionDetails.TrialDetails);
                return RedirectToAction("Index");
            }
            catch (AccountsRepositoryException are)
            {
                ViewBag.ErrorMessage = "Could not create trial subscription";
                if (are.ErrorCode == AccountsRepositoryException.AccountHasTrialSubscription)
                {
                    ViewBag.ErrorDetail = "This account already has a trial subscription";
                }
                return View("AccountError");
            }
        }

        [Authorize]
        public ActionResult Stores(string subscription)
        {
            var identity = User.Identity as ClaimsIdentity;
            var userToken = GetAuthenticatedUserToken(identity);
            var accountsRepository = AccountsRepositoryFactory.GetAccountsRepository();
            var stores = accountsRepository.GetStores(userToken, subscription);
            return View(stores);
        }

        [Authorize]
        public ActionResult CreateStore(string subscriptionId, string label)
        {
            try
            {
                var identity = User.Identity as ClaimsIdentity;
                var userId = GetAuthenticatedUserToken(identity);
                var accountsRepository = AccountsRepositoryFactory.GetAccountsRepository();
                var store = accountsRepository.CreateStore(userId, subscriptionId, label);
                BrightstarCluster.Instance.CreateStore(store.Id);
                return RedirectToAction("Subscription", new {subscriptionId});
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Store creation failed.";
                ViewBag.ErrorDetail = ex.Message;
                return View("AccountError");
            }
        }

        [Authorize]
        public ActionResult AccessKeys()
        {
            var userId = GetAuthenticatedUserToken(User.Identity as ClaimsIdentity);
            var accounts = AccountsRepositoryFactory.GetAccountsRepository();
            var details = accounts.GetAccountDetailsForUser(userId);
            if (details == null) return new HttpNotFoundResult();
            return new JsonResult {Data = new {details.PrimaryKey, details.SecondaryKey}};
        }

        #region Authentication

        //
        // This is the endpoint where the WS-Federation message will be posted. We are disabling the validation
        // here because we are expecting the form to have the xml WS-Federation message in it.
        //
        // POST: /Account/SignIn
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SignIn(FormCollection forms)
        {
            return RedirectToAction("Index", "Account");
        }

        //
        // GET: /Account/SignOut
        [HttpGet]
        public ActionResult SignOut()
        {
            WSFederationAuthenticationModule fam = FederatedAuthentication.WSFederationAuthenticationModule;
            try
            {
                FormsAuthentication.SignOut();
            }
            finally
            {
                FederatedAuthentication.SessionAuthenticationModule.DeleteSessionTokenCookie();
                fam.SignOut(true);
            }

            // Return to home after LogOff
            //SignOutRequestMessage signOutRequest = new SignOutRequestMessage(new Uri(fam.Issuer), fam.Realm);
            //return Redirect(signOutRequest.WriteQueryString());
            return RedirectToAction("Index", "Home");
        }

        //
        // Shows how to use the client side code to get the identity providers data
        //
        [ChildActionOnly]
        public PartialViewResult IdentityProvidersWithClientSideCode()
        {
            WSFederationAuthenticationModule fam = FederatedAuthentication.WSFederationAuthenticationModule;
            // The code below doesn't work on the emulator because the port number in Request.Url is incorrect
            // HrdRequest request = new HrdRequest(fam.Issuer, fam.Realm, context: Request.Url.AbsoluteUri);
            // This is a hack workaround because the correct URI is actually in the Realm:
            HrdRequest request = new HrdRequest(fam.Issuer, fam.Realm, context:fam.Realm);

            return PartialView("_IdentityProvidersWithClientSideCode", request);
        }

        //
        // Shows how to use server side code to get the identity providers data 
        //
        [OutputCache(Duration = 10)]
        [ChildActionOnly]
        public PartialViewResult IdentityProvidersWithServerSideCode()
        {
            WSFederationAuthenticationModule fam = FederatedAuthentication.WSFederationAuthenticationModule;
            HrdRequest request = new HrdRequest(fam.Issuer, fam.Realm, context: Request.Url.AbsoluteUri);

            IEnumerable<HrdIdentityProvider> hrdIdentityProviders = _hrdClient.GetHrdResponse(request);

            return PartialView("_IdentityProvidersWithServerSideCode", hrdIdentityProviders);
        }

        /// <summary>
        /// Gets from the form the context
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        private static string GetUrlFromContext(FormCollection form)
        {
            WSFederationMessage message = WSFederationMessage.CreateFromNameValueCollection(new Uri("http://www.notused.com"), form);
            return (message != null ? message.Context : null);
        }

        #endregion

        private string GetAuthenticatedUserToken(ClaimsIdentity identity)
        {
            var provider =
                identity.Claims.Where(
                    c =>
                    c.ClaimType.Equals(
                        "http://schemas.microsoft.com/accesscontrolservice/2010/07/claims/identityprovider")).Select(
                            c => c.Value).FirstOrDefault();
            var nameidentity =
                identity.Claims.Where(
                    c => c.ClaimType.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")).
                    Select(c => c.Value).FirstOrDefault();

            if (provider == null || nameidentity == null) return null;
            return provider.ToLowerInvariant() + ":" + nameidentity;
        }

    }
}
