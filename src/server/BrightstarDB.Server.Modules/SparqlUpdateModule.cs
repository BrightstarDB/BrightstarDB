using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class SparqlUpdateModule : NancyModule
    {
        private static readonly MediaRange SparqlRequest = MediaRange.FromString("application/sparql-update");

        public SparqlUpdateModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider permissionsProvider)
        {
            this.RequiresBrightstarStorePermission(permissionsProvider, get:StorePermissions.SparqlUpdate, post:StorePermissions.SparqlUpdate);

            Get["/{storeName}/update"] = parameters =>
                {
                    var requestObject = this.Bind<SparqlUpdateRequestObject>();
                    return
                        Negotiate.WithAllowedMediaRange(MediaRange.FromString("text/html"))
                                 .WithModel(requestObject)
                                 .WithView("SparqlUpdate");
                };

            Post["/{storeName}/update"] = parameters =>
                {
                    var requestObject = this.Bind<SparqlUpdateRequestObject>();
                    if (SparqlRequest.Matches(Request.Headers.ContentType))
                    {
                        using (var reader = new StreamReader(Request.Body))
                        {
                            requestObject.Update = reader.ReadToEnd();
                        }
                    }

                    if (!parameters["storeName"].HasValue) return HttpStatusCode.BadRequest;
                    if (string.IsNullOrWhiteSpace(requestObject.Update)) return HttpStatusCode.BadRequest;

                    IJobInfo jobInfo = brightstarService.ExecuteUpdate(parameters["storeName"], requestObject.Update, true);
                    if (jobInfo.JobCompletedOk)
                    {
                        return jobInfo.MakeResponseObject(requestObject.StoreName);
                    }
                    return
                        Negotiate.WithStatusCode(HttpStatusCode.BadRequest)
                                 .WithModel(jobInfo.MakeResponseObject(requestObject.StoreName));
                };
        }

    }
}
