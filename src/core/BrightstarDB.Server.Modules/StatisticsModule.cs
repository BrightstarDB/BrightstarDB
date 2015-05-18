using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;
using Nancy.ModelBinding;

namespace BrightstarDB.Server.Modules
{
    public class StatisticsModule : NancyModule
    {
        private const int DefaultPageSize = 10;

        public StatisticsModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, get:StorePermissions.ViewHistory);
            Get["/{storeName}/statistics"] = parameters =>
                {
                    var request = this.Bind<StatisticsRequestObject>();
                    ViewBag.Title = request.StoreName + " - Statistics";
                    var resourceUri = "statistics" + CreateQueryString(request);

                    // Set defaults
                    if (!request.Latest.HasValue) {  request.Latest = DateTime.MaxValue; }
                    if (!request.Earliest.HasValue) { request.Earliest = DateTime.MinValue; }

                    // Execute
                    var stats = brightstarService.GetStatistics(
                        request.StoreName, request.Latest.Value, request.Earliest.Value,
                        request.Skip, DefaultPageSize + 1);

                    return Negotiate.WithPagedList(request, stats.Select(MakeResponseModel), request.Skip, DefaultPageSize,
                                                   DefaultPageSize, resourceUri);
                };
        }

        private string CreateQueryString(StatisticsRequestObject requestObject)
        {
            if (requestObject.Latest.HasValue)
            {
                if (requestObject.Earliest.HasValue)
                {
                    return String.Format("?latest={0}&earliest={1}", requestObject.Latest.Value.ToString("s"),
                                         requestObject.Earliest.Value.ToString("s"));
                }
                return String.Format("?latest={0}", requestObject.Latest.Value.ToString("s"));
            }
            if (requestObject.Earliest.HasValue)
            {
                return String.Format("?earliest={0}", requestObject.Earliest.Value.ToString("s"));
            }
            return String.Empty;
        }

        private static StatisticsResponseModel MakeResponseModel(IStoreStatistics stats)
        {
            return new StatisticsResponseModel
            {
                CommitId = stats.CommitId,
                CommitTimestamp = stats.CommitTimestamp,
                PredicateTripleCounts = stats.PredicateTripleCounts == null ? new Dictionary<string, ulong>() : new Dictionary<string, ulong>(stats.PredicateTripleCounts),
                TotalTripleCount = stats.TotalTripleCount
            };
        }
    }
}
