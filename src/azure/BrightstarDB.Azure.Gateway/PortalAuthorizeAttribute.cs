using System.Web.Mvc;
using BrightstarDB.Azure.Management;
using Microsoft.IdentityModel.Claims;

namespace BrightstarDB.Azure.Gateway
{
    public class PortalAuthorizeAttribute : AuthorizeAttribute
    {
        private StoreAccessLevel _requiredAccessLevel;
        private readonly string _storeIdKey;

        public PortalAuthorizeAttribute(StoreAccessLevel requiredAccessLevel, string storeIdKey)
        {
            _requiredAccessLevel = requiredAccessLevel;
            _storeIdKey = storeIdKey;
        }


        public override void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            var user = filterContext.RequestContext.HttpContext.User.Identity as ClaimsIdentity;
            var storeId = filterContext.RouteData.Values[_storeIdKey] as string;
            if (user != null && storeId != null)
            {
                var userToken = AuthenticationHelper.GetAuthenticatedUserToken(user);
                var repo = AccountsRepositoryFactory.GetAccountsRepository();
                var userAccess = repo.GetUserAccessKey(userToken, storeId);
                if (userAccess != null)
                {
                    if ((userAccess.Access & _requiredAccessLevel) > 0)
                    {
                        filterContext.RequestContext.RouteData.Values.Add("_accessLevel", userAccess.Access);
                        filterContext.RequestContext.RouteData.Values.Add("_access", userAccess);
                        return;
                    }
                }
            }
            filterContext.Result = new HttpUnauthorizedResult();
        }
    }
}