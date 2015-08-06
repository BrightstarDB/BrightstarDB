using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class StoreModule:NancyModule
    {
        public StoreModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, get:StorePermissions.Read, delete:StorePermissions.Admin);

            Get["/{storeName}"] = parameters =>
            {
                var storeName = parameters["storeName"];
                ViewBag.Title = storeName;

                if (!brightstarService.DoesStoreExist(storeName)) return HttpStatusCode.NotFound;
                if (Request.Method.ToUpperInvariant() == "HEAD")
                {
                    IEnumerable < ICommitPointInfo > commitPoints = brightstarService.GetCommitPoints(storeName, 0, 1);
                    var commit = commitPoints.First();
                    return
                        Negotiate.WithHeader("Last-Modified", commit.CommitTime.ToUniversalTime().ToString("r"))
                            .WithStatusCode(HttpStatusCode.OK)
                            .WithModel(new StoreResponseModel(parameters["storeName"]));
                }
                return new StoreResponseModel(parameters["storeName"]);
            };

            Delete["/{storeName}"] = parameters =>
                {
                    var storeName = parameters["storeName"];
                    ViewBag.Title = "Deleted - " + storeName;
                    if (brightstarService.DoesStoreExist(storeName))
                    {
                        brightstarService.DeleteStore(storeName);
                    }
                    return Negotiate.WithMediaRangeModel(new MediaRange("text/html"), 
                                                         new StoreDeletedModel {StoreName = storeName});
                };
        }
    }
}