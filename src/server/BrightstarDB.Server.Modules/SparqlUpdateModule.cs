using System.IO;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class SparqlUpdateModule : NancyModule
    {
        private static readonly MediaRange SparqlRequest = MediaRange.FromString("application/sparql-update");

        public SparqlUpdateModule(IBrightstarService brightstarService, IStorePermissionsProvider permissionsProvider)
        {
            this.RequiresBrightstarStorePermission(permissionsProvider, post:StorePermissions.SparqlUpdate);

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

                    var jobInfo = brightstarService.ExecuteUpdate(parameters["storeName"], requestObject.Update, true);
                    if (jobInfo.JobCompletedOk)
                    {
                        return HttpStatusCode.OK;
                    }
                    return HttpStatusCode.InternalServerError;
                };
        }
    }
}
