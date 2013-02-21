using System.Linq;
using System.Web.Mvc;

namespace BrightstarDB.Azure.Gateway.Controllers.Admin
{
    public class AccountsListController : Controller
    {
        //
        // GET: /Admin/Accounts/
        [AdminAuthorize]
        public ActionResult Index()
        {
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            var summaries = repo.GetAccountSummaries();
            var summaryList = summaries.ToList();
            return View("Index", summaryList);
        }

    }
}
