using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightstarDB.Storage.Statistics;

namespace BrightstarDB.Storage
{
    interface IStoreStatisticsLog
    {
        IEnumerable<StoreStatistics> GetStatistics();
        void AppendStatistics(StoreStatistics statistics);
    }
}
