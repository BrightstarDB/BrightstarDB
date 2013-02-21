using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Linq;
using System.ServiceModel.Web;
using System.Web;
using BrightstarDB.OData;
using BrightstarDB.Samples.NerdDinner.Models;

namespace BrightstarDB.Samples.NerdDinner
{
    public class OData : EntityDataService<NerdDinnerContext>
    {
        // This method is called only once to initialize service-wide policies.
        public static void InitializeService(DataServiceConfiguration config)
        {
            config.SetEntitySetAccessRule("*", EntitySetRights.AllRead);
            //config.SetEntitySetAccessRule("NerdDinnerLogin", EntitySetRights.None);
            config.SetServiceOperationAccessRule("*", ServiceOperationRights.All);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
        }
    }
}
