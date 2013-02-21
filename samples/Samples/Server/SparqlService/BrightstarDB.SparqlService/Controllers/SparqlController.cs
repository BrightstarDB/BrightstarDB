using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using BrightstarDB.Client;

namespace BrightstarDB.SparqlService.Controllers
{
    /// <summary>
    /// The SPARQL controller accepts queries or updates and dispatches them to the Brightstar Service and 
    /// then returns the result to the calling request.
    /// </summary>
    public class SparqlController : Controller
    {
        // [Authorize(Roles = "SparqlReader")] Uncomment, and configure security with specific roles to lock down read access
        [ValidateInput(false)]
        public ActionResult Query(string storename, string query)
        {
            // Determine what results format to return to the client
            string bestMimeType = MimeParse.MimeParse.BestMatch(SparqlResultsFormat.AllMediaTypes.ToList(),
                                                                Request.Headers["Accept"]);
            var callback = Request["callback"];
            if (!String.IsNullOrEmpty(callback) && Request.Headers["Accept"].Contains("*/*"))
            {
                // Workaround for jQuery clients that will send Accept */* for a JSONP request
                bestMimeType = SparqlResultsFormat.Json.MediaTypes[0];
            }
            SparqlResultsFormat resultsFormat = null;
            if (!String.IsNullOrEmpty(bestMimeType))
            {
                resultsFormat = SparqlResultsFormat.GetResultsFormat(bestMimeType);
            }
            if (resultsFormat == null)
            {
                throw new HttpException(406, "Not Acceptable");
            }

            if (query == null && Request.HttpMethod.ToLower().Equals("post") && Request.ContentType.Equals("application/sparql-query"))
            {
                try
                {
                    string q;
                    using (var streamReader = new StreamReader(Request.InputStream))
                    {
                        q = streamReader.ReadToEnd();
                        if (string.IsNullOrEmpty(q))
                        {
                            throw new HttpException(400, "No query in request body");
                        }
                    }

                    // Execute the query
                    var client = BrightstarService.GetClient();
                    var results = client.ExecuteQuery(storename, q, DateTime.MinValue, resultsFormat);
                    return new FileStreamResult(results, resultsFormat.ToString());
                }
                catch (Exception ex)
                {
                    throw new HttpException(500, "Unable to execute query " + ex.Message, ex);
                }
            }

            if (query != null)
            {
                try 
                {
                    var client = BrightstarService.GetClient();
                    if (resultsFormat.DefaultExtension.Equals(SparqlResultsFormat.Json.DefaultExtension) && !String.IsNullOrEmpty(callback))
                    {
                        // Handle JSONP request
                        var results = client.ExecuteQuery(storename, query, DateTime.MinValue, resultsFormat);
                        using(var stringReader = new StreamReader(results))
                        {
                            var rawJson = stringReader.ReadToEnd();
                            return new ContentResult
                                       {
                                           ContentType = bestMimeType,
                                           Content = callback + "(" + rawJson + ");",
                                           ContentEncoding = Encoding.UTF8
                                       };
                        }
                    }
                    else
                    {
                        var results = client.ExecuteQuery(storename, query, DateTime.MinValue, resultsFormat);
                        return new FileStreamResult(results, resultsFormat.ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new HttpException(500, "Unable to execute query " + ex.Message, ex);
                }
            }


            MimeParse.MimeParse.BestMatch(new []
                                    {
                                        "application/xbel+xml",
                                        "text/xml"
                                    }, "text/*;q=0.5,*; q=0.1");// 'text/xml'
            throw new HttpException(400, "Missing parameters or incorrect request format");
        }

        // [Authorize(Roles = "SparqlWriter")] Uncomment, and configure security with specific roles to lock down write access
        [ValidateInput(false)]
        public ActionResult Update(string storename)
        {
            if (Request.HttpMethod.ToLower().Equals("post") && Request.ContentType.Equals("application/sparql-update"))
            {
                try
                {
                    string update;
                    using (var streamReader = new StreamReader(Request.InputStream))
                    {
                        update = streamReader.ReadToEnd();
                        if (string.IsNullOrEmpty(update))
                        {
                            throw new HttpException(400, "No update expression in request body");
                        }
                    }

                    var client = BrightstarService.GetClient();
                    var jobInfo = client.ExecuteUpdate(storename, update);

                    if (!jobInfo.JobCompletedOk)
                    {
                        throw new HttpException(500, "Unable to complete update job " + jobInfo.StatusMessage);
                    }

                    return new HttpStatusCodeResult(200);

                } catch(Exception ex)
                {
                    throw new HttpException(500, "Unable to complete update job " + ex.Message, ex);
                }
            }

            throw new HttpException(400, "Missing parameters or incorrect request format");
        }

    }
}
