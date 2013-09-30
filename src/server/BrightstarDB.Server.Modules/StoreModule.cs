using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class StoreModule:NancyModule
    {
        public StoreModule(IBrightstarService brightstarService, IStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, StorePermissions.Read);

            Get["/{storeName}"] = parameters =>
                {

                    if (brightstarService.DoesStoreExist(parameters["storeName"]))
                    {
                        if (Request.Method.ToUpperInvariant() == "HEAD")
                        {
                            return HttpStatusCode.OK;
                        }
                        return new StoreResponseObject(parameters["storeName"]);
                    }
                    return HttpStatusCode.NotFound;
                };
        }
    }
}