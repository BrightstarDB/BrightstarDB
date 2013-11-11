using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.Responses.Negotiation;
using StoreResponseModel = BrightstarDB.Server.Modules.Model.StoreResponseModel;

namespace BrightstarDB.Server.Modules
{
    public class StoreModule:NancyModule
    {
        public StoreModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, get:StorePermissions.Read, delete:StorePermissions.Admin);

            Get["/{storeName}"] = parameters =>
                {
                    if (brightstarService.DoesStoreExist(parameters["storeName"]))
                    {
                        if (Request.Method.ToUpperInvariant() == "HEAD")
                        {
                            var storeName = parameters["storeName"];
                            IEnumerable<ICommitPointInfo> commitPoints = brightstarService.GetCommitPoints(storeName, 0,
                                                                                                           1);
                            ICommitPointInfo commit = commitPoints.FirstOrDefault();
                            return
                                Negotiate.WithHeader("Last-Modified", commit.CommitTime.ToString("r"))
                                         .WithStatusCode(HttpStatusCode.OK)
                                         .WithModel(new StoreResponseModel(parameters["storeName"]));
                        }
                        return new StoreResponseModel(parameters["storeName"]);
                    }
                    return HttpStatusCode.NotFound;
                };

            Delete["/{storeName}"] = parameters =>
                {
                    var storeName = parameters["storeName"];
                    if (brightstarService.DoesStoreExist(storeName))
                    {
                        brightstarService.DeleteStore(storeName);
                    }
                    return Negotiate.WithMediaRangeModel(MediaRange.FromString("text/html"),
                                                         new StoreDeletedModel {StoreName = storeName});
                };
        }
    }
}