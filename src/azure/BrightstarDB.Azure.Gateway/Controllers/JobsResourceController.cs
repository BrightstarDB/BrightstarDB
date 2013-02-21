using System;
using System.Web.Mvc;
using System.Web.Routing;
using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.Management;
using System.Diagnostics;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class JobsResourceController : Controller
    {

        [HttpPost, ActionName("Default"), ValidateInput(false), AccessLevelAuthorize(StoreAccessLevel.Write|StoreAccessLevel.Export|StoreAccessLevel.Admin)]
        public ActionResult CreateJob(string subscription, string storeId, string jobType, string jobData)
        {
            Trace.TraceInformation("CreateJob: {0}, {1}, {2}, {3}", subscription, storeId, jobType, jobData);
            try
            {
                JobType parsedJobType;
                if (!Enum.TryParse(jobType, true, out parsedJobType))
                {
                    return new HttpStatusCodeResult(400, "Unrecognized job type");
                }
                StoreAccessLevel requiredAccess = StoreAccessLevel.Write;
                switch (parsedJobType)
                {
                    case JobType.Transaction:
                    case JobType.Import:
                    case JobType.SparqlUpdate:
                        requiredAccess = StoreAccessLevel.Write;
                        break;
                    case JobType.Export:
                        requiredAccess = StoreAccessLevel.Export;
                        break;

                    case JobType.Consolidate:
                    case JobType.DeleteStore:

                        requiredAccess = StoreAccessLevel.Admin;
                        break;
                }
                var userAccess = (StoreAccessLevel)RouteData.Values["_accessLevel"];
                if ((userAccess & requiredAccess) == 0)
                {
                    Trace.TraceInformation("CreateJob: {0} {1} : Insufficient user access privileges", subscription, storeId);
                    return new HttpStatusCodeResult(403, "Insufficient privileges");
                }
                var jobId = BrightstarCluster.Instance.StartJob(storeId, parsedJobType, jobData);
                Trace.TraceInformation("Started Job {0}", jobId);
                var jobUri = UrlHelper.GenerateUrl("ApiJobsResource", null, null,
                                                   new RouteValueDictionary(new { subscription, storeName = storeId, id = jobId }),
                                                   RouteTable.Routes, ControllerContext.RequestContext, false);
                Response.Headers.Add("Location", jobUri);
                return new HttpStatusCodeResult(201);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in JobResourceController.CreateJob: " + ex);
                throw;
            }
        }

        [ActionName("Default"), AccessLevelAuthorize(StoreAccessLevel.Read)]
        public ActionResult GetJobInfo(string subscription, string storeId, string id)
        {
            Trace.TraceInformation("GetJobInfo {0} {1} {2}", subscription, storeId, id);
            try
            {
                var jobInfo = BrightstarCluster.Instance.GetJobInfo(storeId, id);
                return new JsonResult { Data = jobInfo, JsonRequestBehavior = JsonRequestBehavior.AllowGet };
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception in JobsResourceController.GetJobInfo: " + ex);
                throw;
            }
        }
    }
}
