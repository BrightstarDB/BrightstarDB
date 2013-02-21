using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Controllers.Admin
{
    public class AccountAdminController : Controller
    {
        //
        // GET: /AccountAdmin/

        [AdminAuthorize]
        public ActionResult Index(string id)
        {
            var accountDetail = AccountsRepositoryFactory.GetAccountsRepository().GetAccountDetails(id);
            return View(accountDetail);
        }

        [AdminAuthorize]
        public ActionResult AddSubscription(string id)
        {
            var subscriptionDetails = SubscriptionDetails.TrialDetails;
            return View(subscriptionDetails);
        }

        [AdminAuthorize, HttpPost]
        public ActionResult AddSubscription(string id, SubscriptionDetails details)
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            repo.CreateSubscription(id, details);
            return RedirectToAction("Index");
        }

        [AdminAuthorize]
        public ActionResult Deactivate(string id, string subscriptionId)
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            repo.DeactivateSubscription(id, subscriptionId);
            return RedirectToAction("Index");
        }

        [AdminAuthorize]
        public ActionResult Activate(string id, string subscriptionId)
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            repo.ActivateSubscription(id, subscriptionId);
            return RedirectToAction("Index");
        }
    }
}
