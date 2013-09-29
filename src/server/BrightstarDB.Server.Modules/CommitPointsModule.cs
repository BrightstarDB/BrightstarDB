using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrightstarDB.Client;
using BrightstarDB.Server.Modules.Model;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;

namespace BrightstarDB.Server.Modules
{
    public class CommitPointsModule : NancyModule
    {
        private const int DefaultPageSize = 10;

        public CommitPointsModule(IBrightstarService brightstarService)
        {
            Get["/{storeName}/commits"] = parameters =>
                {
                    int skip = Request.Query["skip"].TryParse<int>(0);
                    int take = Request.Query["take"].TryParse<int>(DefaultPageSize);
                    DateTime timestamp = Request.Query["timestamp"].TryParse<DateTime>();
                    DateTime earliest = Request.Query["earliest"].TryParse<DateTime>();
                    DateTime latest = Request.Query["latest"].TryParse<DateTime>();

                    if (timestamp != default(DateTime))
                    {
                        // Request for a single commit point
                        return MakeResponseObject(brightstarService.GetCommitPoint(parameters["storeName"], timestamp));
                    }
                    if (earliest != default(DateTime) && latest != default(DateTime))
                    {
                        IEnumerable<ICommitPointInfo> results =
                            brightstarService.GetCommitPoints(parameters["storeName"], latest, earliest, skip, take + 1);
                        var resourceUri = String.Format("commits?latest={0}&earliest={1}", latest.ToString("s"), earliest.ToString("s"));
                        return Negotiate.WithPagedList(results.Select(MakeResponseObject), skip, take, DefaultPageSize, resourceUri);
                    }
                    IEnumerable<ICommitPointInfo> commitPointInfos = brightstarService.GetCommitPoints(parameters["storeName"], skip, take + 1);
                    return Negotiate.WithPagedList(commitPointInfos.Select(MakeResponseObject), skip, take, DefaultPageSize, "commits");
                };

            Post["/{storeName}/commits"] = parameters =>
                {
                    var commitPoint = this.Bind<CommitPointResponseObject>();
                    if (commitPoint == null ||
                        String.IsNullOrEmpty(commitPoint.StoreName) ||
                        !commitPoint.StoreName.Equals(parameters["storeName"]))
                    {
                        return HttpStatusCode.BadRequest;
                    }

                    var commitPointInfo = brightstarService.GetCommitPoint(parameters["storeName"], commitPoint.Id);
                    if (commitPointInfo == null) return HttpStatusCode.BadRequest;

                    brightstarService.RevertToCommitPoint(parameters["storeName"], commitPointInfo);
                    return HttpStatusCode.OK;
                };
        }

        private static CommitPointResponseObject MakeResponseObject(ICommitPointInfo commitPointInfo)
        {
            return new CommitPointResponseObject
                {
                    Id = commitPointInfo.Id,
                    CommitTime = commitPointInfo.CommitTime,
                    JobId = commitPointInfo.JobId,
                    StoreName = commitPointInfo.StoreName
                };
        }
    }
}
