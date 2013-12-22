using System.Collections.Generic;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using Nancy;

namespace BrightstarDB.Server.Modules
{
    public class LatestStatisticsModule : NancyModule
    {
        public LatestStatisticsModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider storePermissionsProvider)
        {
            this.RequiresBrightstarStorePermission(storePermissionsProvider, get:StorePermissions.Read);
            Get["/{storeName}/statistics/latest"] = parameters =>
                {
                    var latest = brightstarService.GetStatistics(parameters["storeName"]);
                    if (latest == null) return HttpStatusCode.NotFound;
                    return MakeResponseModel(latest);
                };
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