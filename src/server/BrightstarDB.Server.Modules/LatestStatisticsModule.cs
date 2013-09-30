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
                    return MakeResponseModel(latest);
                };
        }

        private static StatisticsResponseObject MakeResponseModel(IStoreStatistics stats)
        {
            return new StatisticsResponseObject
                {
                    CommitId = stats.CommitId,
                    CommitTimestamp = stats.CommitTimestamp,
                    PredicateTripleCounts = stats.PredicateTripleCounts == null ? new Dictionary<string, ulong>() : new Dictionary<string, ulong>(stats.PredicateTripleCounts),
                    TotalTripleCount = stats.TotalTripleCount
                };
        }
    }
}