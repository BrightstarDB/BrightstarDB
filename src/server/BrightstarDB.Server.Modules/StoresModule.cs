using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Storage;
using Nancy;
using Nancy.ModelBinding;

namespace BrightstarDB.Server.Modules
{
    public class StoresModule : NancyModule
    {
        public StoresModule(IBrightstarService brightstarService, ISystemPermissionsProvider systemPermissionsProvider)
        {
            this.RequiresBrightstarSystemPermission(systemPermissionsProvider, get:SystemPermissions.ListStores, post:SystemPermissions.CreateStore);

            Get["/"] = parameters =>
                {
                    var stores = brightstarService.ListStores();
                    return stores.Select(s=>new StoreResponseObject(s));
                };

            Post["/"] = parameters =>
                {
                    var rdr = new StreamReader(this.Request.Body);
                    var body = rdr.ReadToEnd();
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
                    return new StoreResponseObject(request.StoreName);
                };
        }
    }
}
