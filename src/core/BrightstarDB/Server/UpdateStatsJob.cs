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
                var predicateTripleCounts = new Dictionary<string, ulong>();
                ulong totalTripleCount = 0;
                List<string> predicates = StoreWorker.ReadStore.GetPredicates().ToList();
                for (int i = 0; i < predicates.Count; i++)
                {
                    var tripleCount = StoreWorker.ReadStore.GetTripleCount(predicates[i]);
                    totalTripleCount += tripleCount;
                    predicateTripleCounts[predicates[i]] = tripleCount;
                    jobStatus.Information =
                        String.Format("Count completed for {0}/{1} predicates. Approximately {2:P1} percent complete",
                                      i + 1, predicates.Count, (i + 1)*100.0/predicates.Count);

                }
                var currentCommitPoint = StoreWorker.ReadStore.GetCommitPoints().First();
                StoreWorker.StoreStatistics.AppendStatistics(
                    new StoreStatistics(
                        currentCommitPoint.LocationOffset,
                        currentCommitPoint.CommitTime, totalTripleCount,
                        predicateTripleCounts));
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
