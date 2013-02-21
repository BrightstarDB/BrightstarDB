using System.Web.Mvc;
using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class StoreJobsController : Controller
    {
        //
        // GET: /StoreJobs/
        [PortalAuthorize(StoreAccessLevel.Admin | StoreAccessLevel.Write, "storeId")]
       public ActionResult Index(string storeId)
        {
           var jobs = BrightstarCluster.Instance.GetJobs(storeId);
           return View(jobs);
       }

        [PortalAuthorize(StoreAccessLevel.Admin | StoreAccessLevel.Write, "storeId")]
       public ActionResult Detail(string storeId, string jobId)
        {
            JobInfo job = BrightstarCluster.Instance.GetJobInfo(storeId, jobId);
           ViewBag.StoreId = storeId;
            return View(job);
        }
    }
}
