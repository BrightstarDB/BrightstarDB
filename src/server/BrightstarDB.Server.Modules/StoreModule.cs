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

                    if (brightstarService.DoesStoreExist(parameters["storeName"]))
                    {
                        if (Request.Method.ToUpperInvariant() == "HEAD")
                        {
                            return HttpStatusCode.OK;
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