using System;
using System.IO;
using System.Web.Mvc;
using BrightstarDB.Azure.Common;
using BrightstarDB.Azure.Gateway.Models;
using BrightstarDB.Azure.Management;

namespace BrightstarDB.Azure.Gateway.Controllers
{
    public class StoresController : Controller
    {
        //
        // GET: /Stores/

        public ActionResult Index()
        {
            var stores = BrightstarCluster.Instance.GetStores();
            return View(stores);
        }

        [HttpPost, ActionName("Index")]
        public ActionResult NewStore(string storeName)
        {
            BrightstarCluster.Instance.CreateStore(storeName);
            var stores = BrightstarCluster.Instance.GetStores();
            return View(stores);
        }

        [PortalAuthorize(StoreAccessLevel.Admin, "id")]
        public ActionResult Manage(string id)
        {
            ViewBag.StoreId = id;
            ViewBag.AccessLevel = (StoreAccessLevel)RouteData.Values["_accessLevel"];
            ViewBag.AccountId = ((StoreAccessKey) RouteData.Values["_access"]).AccountId;
            var repo = AccountsRepositoryFactory.GetAccountsRepository();
            var storeDetail = repo.GetStoreDetail(id);
            return View(storeDetail);
        }

        [PortalAuthorize(StoreAccessLevel.Write, "id")]
        public ActionResult Transaction(string id)
        {
            ViewBag.StoreId = id;
            ViewBag.AccessLevel = (StoreAccessLevel)RouteData.Values["_accessLevel"];
            return View();
        }

        [HttpPost, ActionName("Transaction"), ValidateInput(false), PortalAuthorize(StoreAccessLevel.Write, "id")]
        public ActionResult NewTransactionJob(string id, string deleteTriples, string insertTriples)
        {
            var preconditions = String.Empty;
            var jobId = BrightstarCluster.Instance.StartUpdateTransaction(id, preconditions, deleteTriples, insertTriples);
            return RedirectToAction("Detail", "StoreJobs", new {storeId = id, jobId = jobId});
        }

        public ActionResult Query(string id)
        {
            var queryModel = new QueryModel {Query = "SELECT DISTINCT ?t WHERE { ?x a ?t }", StoreId=id};
            return View(queryModel);
        }

        [HttpPost, ActionName("Query"), ValidateInput(false)]
        public ActionResult ExecuteQuery(string id, string query)
        {
            var queryModel = new QueryModel {Query = query, StoreId=id};
            try
            {
                queryModel.Results = BrightstarCluster.Instance.ExecuteQuery(id, query);
            }
            catch (Exception ex)
            {
                queryModel.Results = "Error executing query: " + ex;
            }
            return View(queryModel);
        }

        public ActionResult MasterFile(string id)
        {
            var buff = BrightstarCluster.Instance.GetMasterFile(id);

            return new FileStreamResult(new MemoryStream(buff), "binary/octet-stream");
        }

        public ActionResult DataFile(string id)
        {
            var buff = BrightstarCluster.Instance.GetDataFile(id);
            return new FileStreamResult(new MemoryStream(buff), "binary/octet-stream");
        }

        [PortalAuthorize(StoreAccessLevel.Admin, "id")]
        public ActionResult Reset(string id)
        {
            BrightstarCluster.Instance.DeleteStore(id);
            BrightstarCluster.Instance.CreateStore(id);
            ViewBag.StoreId = id;
            ViewBag.AccessLevel = (StoreAccessLevel)RouteData.Values["_accessLevel"];
            return RedirectToAction("Manage");
        }

        [PortalAuthorize(StoreAccessLevel.Admin, "id")]
        public ActionResult Delete(string id)
        {
            AccountsRepositoryFactory.GetAccountsRepository().DeleteStore(id);
            BrightstarCluster.Instance.DeleteStore(id);
            return RedirectToAction("Index", "Account");
        }

        [PortalAuthorize(StoreAccessLevel.Write, "id")]
        public ActionResult Import(string id, ImportSourceModel importSource)
        {
            var importData = new Client.BlobImportSource
                                 {
                                     BlobUri = importSource.SourceAddress,
                                     ConnectionString = importSource.StorageConnectionString,
                                     IsGZiped = importSource.UseGZip
                                 };
            var jobId = BrightstarCluster.Instance.StartJob(id, JobType.Import, importData.ToJsonString());
            return RedirectToAction("Detail", "StoreJobs", new {storeId = id, jobId});
        }

        [PortalAuthorize(StoreAccessLevel.Admin, "id")]
        public ActionResult Export(string id, ExportSinkModel exportSink)
        {
            var exportData = new Client.BlobImportSource
                                 {
                                     BlobUri = exportSink.ContainerAddress + "/" + exportSink.BlobName,
                                     ConnectionString = exportSink.StorageConnectionString,
                                     IsGZiped = exportSink.UseGZip
                                 };
            var jobId = BrightstarCluster.Instance.StartJob(id, JobType.Export, exportData.ToJsonString());
            return RedirectToAction("Detail", "StoreJobs", new {storeId = id, jobId});
        }

        [PortalAuthorize(StoreAccessLevel.Admin | StoreAccessLevel.Write, "id")]
        public ActionResult Jobs(string id)
        {
            ViewBag.StoreId = id;
            ViewBag.AcccessLevel = (StoreAccessLevel)RouteData.Values["_accessLevel"];
            var jobs = BrightstarCluster.Instance.GetJobs(id);
            return View(jobs);
        }

    }
}
