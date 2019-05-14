using System;
using BrightstarDB.Storage.Statistics;

namespace BrightstarDB.Server
{
    internal class StatsMonitor
    {
        private int _jobCounter;
        private DateTime _lastStatsUpdateTime;
        private Action _statsUpdateAction;

        public void Initialize(StoreStatistics lastStats, ulong lastCommitId, Action statsUpdateAction)
        {
            if (lastStats == null)
            {
                _jobCounter = 0;
                _lastStatsUpdateTime = DateTime.UtcNow;
            }
            else
            {
                _jobCounter = (int) (lastCommitId - lastStats.CommitNumber);
                _lastStatsUpdateTime = lastStats.CommitTime;
            }
            _statsUpdateAction = statsUpdateAction;
        }

        public void OnJobScheduled( bool incrementTransactionCount  =true)
        {
            if (_statsUpdateAction == null) return;
            if (Configuration.StatsUpdateTransactionCount == 0 && Configuration.StatsUpdateTimespan == 0) return;

            lock (this)
            {
                if (incrementTransactionCount) _jobCounter++;

                if ((Configuration.StatsUpdateTransactionCount == 0 ||
                     _jobCounter >= Configuration.StatsUpdateTransactionCount) &&
                    (Configuration.StatsUpdateTimespan == 0 ||
                     DateTime.UtcNow.Subtract(_lastStatsUpdateTime).TotalSeconds >= Configuration.StatsUpdateTimespan))
                {
                    _statsUpdateAction();
                    _lastStatsUpdateTime = DateTime.UtcNow;
                    _jobCounter = 0;
                }
            }
        }
    }
}
