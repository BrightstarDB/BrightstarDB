using System;
using Microsoft.WindowsAzure.StorageClient;

namespace BrightstarDB.Azure.Common.Logging
{
    public class QueryLogEntity : TableServiceEntity
    {
        public QueryLogEntity(string storeId,string queryString, long rowCount, double timeToResult)
        {
            PartitionKey = storeId;
            RowKey = (DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks).ToString("d19");
            RowCount = rowCount;
            TimeToResult = timeToResult;
            QueryString = queryString;
        }

        public long RowCount { get; set; }
        public double TimeToResult { get; set; }
        public string QueryString { get; set; }
    }
}
