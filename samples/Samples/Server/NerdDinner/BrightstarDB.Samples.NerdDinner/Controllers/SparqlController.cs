using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BrightstarDB.Client;

namespace BrightstarDB.Samples.NerdDinner.Controllers
{
    public class SparqlController : Controller
    {
        //
        // GET: /Sparql/

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Index(string query)
        {
            if (String.IsNullOrEmpty(query))
            {
                return View("Error");
            }
            var client = BrightstarService.GetClient();
            var results = client.ExecuteQuery("NerdDinner", query);
            return new FileStreamResult(results, "application/xml; charset=utf-8");
        }

    }
}
