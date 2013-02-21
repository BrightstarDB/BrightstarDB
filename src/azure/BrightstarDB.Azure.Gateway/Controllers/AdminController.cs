using System.Web.Mvc;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class AdminController : Controller
    {
        //
        // GET: /Admin/
        [AdminAuthorize]
        public ActionResult Index()
        {
            return View();
        }

    }
}
