using System;
using System.Linq;
using BrightstarDB.Client;
using BrightstarDB.Dto;
using BrightstarDB.Server.Modules.Model;
using BrightstarDB.Server.Modules.Permissions;
using BrightstarDB.Storage;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;

namespace BrightstarDB.Server.Modules
{
    public class JobsModule : NancyModule
    {
        private const int DefaultPageSize = 10;
        public JobsModule(IBrightstarService brightstarService, AbstractStorePermissionsProvider permissionsProvider)
        {
            this.RequiresBrightstarStorePermissionData(permissionsProvider);

            Get["/{storeName}/jobs"] = parameters =>
                {
                    var jobsRequestObject = this.Bind<JobsRequestModel>();
                    if (jobsRequestObject == null || jobsRequestObject.StoreName == null)
                    {
                        return HttpStatusCode.BadRequest;
                    }
                    if (jobsRequestObject.Take <= 0) jobsRequestObject.Take = DefaultPageSize;
                    var jobs = brightstarService.GetJobInfo(jobsRequestObject.StoreName, jobsRequestObject.Skip,
                                                            jobsRequestObject.Take + 1);
                    return Negotiate.WithPagedList(jobsRequestObject,
                                                   jobs.Select(j=>j.MakeResponseObject(jobsRequestObject.StoreName)),
                                                   jobsRequestObject.Skip, jobsRequestObject.Take, DefaultPageSize,
                                                   "jobs");
                };

            Get["/{storeName}/jobs/{jobId}"] = parameters =>
                {
                    var request = this.Bind<JobRequestModel>();
                    if (request == null ||
                        request.StoreName == null ||
                        request.JobId == null)
                    {
                        return HttpStatusCode.BadRequest;
                    }
                    var job = brightstarService.GetJobInfo(request.StoreName, request.JobId);
                    if (job == null) return HttpStatusCode.NotFound;
                    return job.MakeResponseObject(request.StoreName);
                };

            Post["/{storeName}/jobs"] = parameters =>
                {
                    var jobRequestObject = this.Bind<JobRequestObject>();

                    // Validate
                    if (jobRequestObject == null) return HttpStatusCode.BadRequest;
                    if (String.IsNullOrWhiteSpace(jobRequestObject.JobType)) return HttpStatusCode.BadRequest;

                    var storeName = parameters["storeName"];

                    try
                    {
                        IJobInfo queuedJobInfo;
                        switch (jobRequestObject.JobType.ToLowerInvariant())
                        {
                            case "consolidate":
                                AssertPermission(StorePermissions.Admin);
                                queuedJobInfo = brightstarService.ConsolidateStore(storeName);
                                break;

                            case "createsnapshot":
                                AssertPermission(StorePermissions.Admin);
                                PersistenceType persistenceType;

                                // Validate TargetStoreName and PersistenceType parameters
                                if (!jobRequestObject.JobParameters.ContainsKey("TargetStoreName") ||
                                    String.IsNullOrWhiteSpace(jobRequestObject.JobParameters["TargetStoreName"]) ||
                                    !jobRequestObject.JobParameters.ContainsKey("PersistenceType") ||
                                    !Enum.TryParse(jobRequestObject.JobParameters["PersistenceType"],
                                                   out persistenceType))
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                // Extract optional commit point parameter
                                ICommitPointInfo commitPoint = null;
                                if (jobRequestObject.JobParameters.ContainsKey("CommitId"))
                                {
                                    ulong commitId;
                                    if (!UInt64.TryParse(jobRequestObject.JobParameters["CommitId"], out commitId))
                                    {
                                        return HttpStatusCode.BadRequest;
                                    }
                                    commitPoint = brightstarService.GetCommitPoint(storeName, commitId);
                                    if (commitPoint == null)
                                    {
                                        return HttpStatusCode.BadRequest;
                                    }
                                }

                                // Execute
                                queuedJobInfo = brightstarService.CreateSnapshot(
                                    storeName, jobRequestObject.JobParameters["TargetStoreName"],
                                    persistenceType, commitPoint);
                                break;

                            case "export":
                                AssertPermission(StorePermissions.Export);
                                if (!jobRequestObject.JobParameters.ContainsKey("FileName") ||
                                    !jobRequestObject.JobParameters.ContainsKey("GraphUri") ||
                                    String.IsNullOrWhiteSpace(jobRequestObject.JobParameters["FileName"]))
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                queuedJobInfo = brightstarService.StartExport(
                                    storeName,
                                    jobRequestObject.JobParameters["FileName"],
                                    jobRequestObject.JobParameters["GraphUri"]);
                                break;

                            case "import":
                                AssertPermission(StorePermissions.TransactionUpdate);
                                if (!jobRequestObject.JobParameters.ContainsKey("FileName") ||
                                    !jobRequestObject.JobParameters.ContainsKey("DefaultGraphUri") ||
                                    String.IsNullOrWhiteSpace(jobRequestObject.JobParameters["FileName"]))
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                queuedJobInfo = brightstarService.StartImport(
                                    storeName,
                                    jobRequestObject.JobParameters["FileName"],
                                    jobRequestObject.JobParameters["DefaultGraphUri"]);
                                break;

                            case "repeattransaction":
                                AssertPermission(StorePermissions.Admin);
                                if (!jobRequestObject.JobParameters.ContainsKey("JobId") ||
                                    String.IsNullOrWhiteSpace(jobRequestObject.JobParameters["JobId"]))
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                Guid jobId;
                                if (!Guid.TryParse(jobRequestObject.JobParameters["JobId"], out jobId))
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                var transaction = brightstarService.GetTransaction(storeName, jobId);
                                if (transaction == null)
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                queuedJobInfo = brightstarService.ReExecuteTransaction(storeName, transaction);
                                break;

                            case "sparqlupdate":
                                AssertPermission(StorePermissions.SparqlUpdate);
                                if (!jobRequestObject.JobParameters.ContainsKey("UpdateExpression") ||
                                    String.IsNullOrWhiteSpace(jobRequestObject.JobParameters["UpdateExpression"]))
                                {
                                    return HttpStatusCode.BadRequest;
                                }
                                queuedJobInfo = brightstarService.ExecuteUpdate(
                                    storeName,
                                    jobRequestObject.JobParameters["UpdateExpression"],
                                    false);
                                break;

                            case "transaction":
                                AssertPermission(StorePermissions.TransactionUpdate);
                                var preconditions = jobRequestObject.JobParameters.ContainsKey("Preconditions")
                                                        ? jobRequestObject.JobParameters["Preconditions"]
                                                        : null;
                                var deletePatterns = jobRequestObject.JobParameters.ContainsKey("Deletes")
                                                         ? jobRequestObject.JobParameters["Deletes"]
                                                         : null;
                                var insertTriples = jobRequestObject.JobParameters.ContainsKey("Inserts")
                                                        ? jobRequestObject.JobParameters["Inserts"]
                                                        : null;
                                var defaultGraphUri =
                                    jobRequestObject.JobParameters.ContainsKey("DefaultGraphUri") &&
                                    !String.IsNullOrEmpty(jobRequestObject.JobParameters["DefaultGraphUri"])
                                        ? jobRequestObject.JobParameters["DefaultGraphUri"]
                                        : null;

                                queuedJobInfo = brightstarService.ExecuteTransaction(
                                    storeName, preconditions,
                                    deletePatterns, insertTriples,
                                    defaultGraphUri,
                                    false);
                                break;

                            case "updatestats":
                                AssertPermission(StorePermissions.Admin);
                                queuedJobInfo = brightstarService.UpdateStatistics(storeName);
                                break;

                            default:
                                return HttpStatusCode.BadRequest;
                        }

                        var jobUri = (string) storeName + "/jobs/" + queuedJobInfo.JobId;
                        return Negotiate.WithModel(new JobResponseModel
                            {
                                JobId = queuedJobInfo.JobId,
                                StatusMessage = queuedJobInfo.StatusMessage,
                                JobStatus = queuedJobInfo.GetJobStatusString()
                            })
                            .WithHeader("Location", jobUri)
                            .WithStatusCode(HttpStatusCode.Created);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return HttpStatusCode.Unauthorized;
                    }

                };
        }

        

        private void AssertPermission(StorePermissions permissionRequired)
        {
            var entry = Context.ViewBag["BrightstarStorePermissions"];
            if (entry.HasValue)
            {
                if ((((StorePermissions)entry.Value) & permissionRequired) == permissionRequired)
                {
                    return;
                }
            }
            throw new UnauthorizedAccessException();
        }

        
    }
}
