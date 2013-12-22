using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Server.Modules.Permissions;
using BrightstarDB.Storage;
using Nancy;
using Nancy.ModelBinding;
using CreateStoreRequestObject = BrightstarDB.Server.Modules.Model.CreateStoreRequestObject;
using StoreResponseModel = BrightstarDB.Server.Modules.Model.StoreResponseModel;

namespace BrightstarDB.Server.Modules
{
    public class StoresModule : NancyModule
    {
        public StoresModule(IBrightstarService brightstarService, AbstractSystemPermissionsProvider systemPermissionsProvider)
        {
            this.RequiresBrightstarSystemPermission(systemPermissionsProvider, get:SystemPermissions.ListStores, post:SystemPermissions.CreateStore);

            Get["/"] = parameters =>
                {
                    var stores = brightstarService.ListStores();
                    return
                        Negotiate.WithModel(new StoresResponseModel
                            {
                                Stores = stores.ToList()
                            });
                };

            Post["/"] = parameters =>
                {
                    var request = this.Bind<CreateStoreRequestObject>();
                    if (request == null || String.IsNullOrEmpty(request.StoreName))
                    {
                        return HttpStatusCode.BadRequest;
                    }

                    // Return 409 Conflict if attempt to create a store with a name that is currently in use
                    if (brightstarService.DoesStoreExist(request.StoreName))
                    {
                        return HttpStatusCode.Conflict;
                    }

                    // Attempt to create the store
                    try
                    {
                        PersistenceType? storePersistenceType = request.GetBrightstarPersistenceType();
                        if (storePersistenceType.HasValue)
                        {
                            brightstarService.CreateStore(request.StoreName, storePersistenceType.Value);
                        }
                        else
                        {
                            brightstarService.CreateStore(request.StoreName);
                        }
                    }
                    catch (ArgumentException)
                    {
                        return HttpStatusCode.BadRequest;   
                    }
                    return
                        Negotiate.WithModel(new StoreResponseModel(request.StoreName))
                                 .WithStatusCode(HttpStatusCode.Created);
                };
        }
    }
}
