using System.Web.Mvc;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class SubscriptionsResourceController : Controller
    {
        // POST /services/account/{accountId}/subscriptions
        [HttpPost, AccessLevelAuthorize(StoreAccessLevel.Superuser), ActionName("Default")]
        public ActionResult Create(string accountId, SubscriptionDetails details)
        {
            var accounts = GetAccountsRepository();
            var newSubscription = accounts.CreateSubscription(accountId, details);
            return new JsonResult {Data = newSubscription};
        }

        // POST /services/account/{accountId}/subscriptions/{subscriptionId}
        [HttpPost, AccessLevelAuthorize(StoreAccessLevel.Superuser), ActionName("Default")]
        public ActionResult Update(string accountId, string subscriptionId, SubscriptionDetails details)
        {
            var accounts = GetAccountsRepository();
            accounts.UpdateSubscription(subscriptionId, details);
            return new EmptyResult();
        }

        // GET /services/account/{accountId}/subscriptions
        [HttpGet, AccessLevelAuthorize(StoreAccessLevel.Superuser), ActionName("Default")]
        public ActionResult GetSubscriptions(string accountId)
        {
            var accounts = GetAccountsRepository();
            var account = accounts.GetAccountDetails(accountId);
            if (account == null) return new HttpNotFoundResult();
            return new JsonResult {Data = account.Subscriptions};
        }

        private IAccountsRepository GetAccountsRepository()
        {
            return AccountsRepositoryFactory.GetAccountsRepository();
        }

    }
}
