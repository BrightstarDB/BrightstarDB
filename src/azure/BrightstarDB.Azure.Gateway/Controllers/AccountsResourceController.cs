using System.Web.Mvc;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class AccountsResourceController : Controller
    {
        [HttpPost, ActionName("Default"), AccessLevelAuthorize(StoreAccessLevel.Superuser)]
        public ActionResult CreateAccount(string accountIdOrUserToken, string emailAddress)
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            var accountId = repo.CreateAccount(accountIdOrUserToken, emailAddress);
            return new JsonResult {Data = accountId};
        }

        [HttpGet, ActionName("Default"), AccessLevelAuthorize(StoreAccessLevel.Superuser), ]
        public ActionResult GetAccount(string accountIdOrUserToken)
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            var accountDetails = repo.GetAccountDetails(accountIdOrUserToken);
            if (accountDetails == null)
            {
                accountDetails = repo.GetAccountDetailsForUser(accountIdOrUserToken);
            }
            return new JsonResult { Data = accountDetails, JsonRequestBehavior = JsonRequestBehavior.AllowGet};
        }

        [HttpPost, ActionName("SetAdminFlag"), AccessLevelAuthorize(StoreAccessLevel.Superuser)]
        public ActionResult SetAdminFlag(string accountId, bool isAdmin)
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            var accountDetails = repo.UpdateAccount(accountId, isAdmin: isAdmin);
            return new JsonResult {Data = accountDetails};
        }

    }
}
