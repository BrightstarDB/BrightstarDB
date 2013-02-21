using System;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    /// <summary>
    /// Controller for the stores resource
    /// </summary>
    public class StoreRestController : Controller
    {
        //
        // GET: /

        /// <summary>
        /// GET the list of stores
        /// </summary>
        /// <param name="subscription">The ID of the subscription whose stores are to be listed</param>
        /// <returns>A list of all stores in the subscription identified by <paramref name="subscription"/>
        /// to which the user has some level of access granted.</returns>
        [AccessLevelAuthorize(StoreAccessLevel.None), ActionName("StoreList")]
        public ActionResult StoreList(string subscription)
        {
            return new JsonResult
                       {
                           Data = BrightstarCluster.Instance.GetStores(),
                           JsonRequestBehavior = JsonRequestBehavior.AllowGet
                       };
        }

        /// <summary>
        /// Create a new store for a subscription
        /// </summary>
        /// <param name="subscription">The subscription that will hold the new store</param>
        /// <param name="storeName">The name to assign to the new store</param>
        /// <returns></returns>
        [HttpPost, ActionName("StoreList"), AccessLevelAuthorize(StoreAccessLevel.Admin)]
        public ActionResult CreateStore(string subscription, string storeName)
        {
            BrightstarCluster.Instance.CreateStore(storeName);
            var url = UrlHelper.GenerateUrl("Store", null, null,
                                  new RouteValueDictionary(new {storeName}),
                                  RouteTable.Routes, ControllerContext.RequestContext, false);
            Response.AddHeader("Location", url);
            return new HttpStatusCodeResult(201, "Created");
        }

        /// <summary>
        /// HEAD request on a store returns the last modified time
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="storeId"></param>
        /// <returns></returns>
        [AcceptVerbs("HEAD"), ActionName("Store"), ValidateInput(false), AccessLevelAuthorize(StoreAccessLevel.Read)]
        public ActionResult StoreInfo(string subscription, string storeId)
        {
            try
            {
                DateTime lastModified = BrightstarCluster.Instance.GetLastModifiedDate(storeId);
                Response.Headers.Add("Last-Modified", lastModified.ToUniversalTime().ToString("s"));
                return new EmptyResult();
            }
            catch (StoreNotFoundException)
            {
                return new HttpNotFoundResult();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error handling HEAD request to {0}. Cause: {1}", Request.RawUrl, ex);
                throw;
            }
        }

        /// <summary>
        /// DELETE an existing store
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="storeId"></param>
        /// <returns></returns>
        [HttpDelete, ActionName("Store"), AccessLevelAuthorize(StoreAccessLevel.Admin)]
        public ActionResult DeleteStore(string subscription, string storeId)
        {
            BrightstarCluster.Instance.DeleteStore(storeId);
            return new EmptyResult();
        }

        //[AcceptVerbs("GET", "POST"), ActionName("Store"), ValidateInput(false), AccessLevelAuthorize(StoreAccessLevel.Read)]
        [AcceptVerbs("GET", "POST"), ActionName("Store"), ValidateInput(false), AccessLevelAuthorize(StoreAccessLevel.None)]
        public ActionResult Query(string subscription, string storeId, string query)
        {
            try
            {
                var response = BrightstarCluster.Instance.ExecuteQuery(storeId, query);
                return new ContentResult
                           {
                               Content = response,
                               ContentEncoding = System.Text.Encoding.Unicode,
                               ContentType = "application/sparql-results+xml"
                           };
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, ex.Message);
            }
        }
    }
}
