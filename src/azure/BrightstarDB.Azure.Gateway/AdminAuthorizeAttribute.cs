using System.Web.Mvc;
using Microsoft.IdentityModel.Claims;

namespace BrightstarDB.Azure.Gateway
{
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(System.Web.Mvc.AuthorizationContext filterContext)
        {
            var user = filterContext.RequestContext.HttpContext.User.Identity as ClaimsIdentity;
            if (user != null)
            {
                var userToken = AuthenticationHelper.GetAuthenticatedUserToken(user);
                if (userToken != null)
                {
                    var repo = AccountsRepositoryFactory.GetAccountsRepository();
                    var account = repo.GetAccountDetailsForUser(userToken);
                    if (account != null)
                    {
                        if (account.IsAdmin)
                        {
                            filterContext.RequestContext.RouteData.Values.Add("_userAccount", account);
                            return;
                        }
                    }
                }
            }
            filterContext.Result = new HttpUnauthorizedResult();
        }
    }
}