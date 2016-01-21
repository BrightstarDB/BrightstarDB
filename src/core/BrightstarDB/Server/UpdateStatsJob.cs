using System;
using System.Collections.Generic;
using System.Linq;
using BrightstarDB.Storage.Statistics;

namespace BrightstarDB.Server
{
    internal class UpdateStatsJob : Job
    {
        public UpdateStatsJob(Guid jobId, string label, StoreWorker storeWorker) : base(jobId, label, storeWorker)
        {
        }

        public override void Run()
        {
            try
            {
                var jobStatus = StoreWorker.GetJobStatus(JobId.ToString());
                var predicateStatistics = new Dictionary<string, PredicateStatistics>();
                ulong totalTripleCount = 0;
                var predicates = StoreWorker.ReadStore.GetPredicates().ToList();
                for (var i = 0; i < predicates.Count; i++)
                {
                    var stats = StoreWorker.ReadStore.GetPredicateStatistics(predicates[i]);
                    totalTripleCount += stats.TripleCount;
                    predicateStatistics[predicates[i]] = stats;
                    jobStatus.Information =
                        string.Format("Count completed for {0}/{1} predicates. Approximately {2:P1} percent complete",
                                      i + 1, predicates.Count, (i + 1.0)/predicates.Count);

                }
                var currentCommitPoint = StoreWorker.ReadStore.GetCommitPoints().First();
                StoreWorker.StoreStatistics.AppendStatistics(
                    new StoreStatistics(
                        currentCommitPoint.LocationOffset,
                        currentCommitPoint.CommitTime, totalTripleCount,
                        predicateStatistics));
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error reading store triple statistics. Cause: " + ex.Message;
                Logging.LogError(BrightstarEventId.StatsUpdateError, "Error reading store triple statistics for store. Cause: {0}",
                    ex);
            }
        }
    }
}
