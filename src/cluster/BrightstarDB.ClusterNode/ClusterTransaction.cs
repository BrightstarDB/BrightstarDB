using System;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterNode
{
    public abstract class ClusterTransaction
    {
        public Guid PrevTxnId { get; set; }
        public Guid JobId { get; set; }
        public string StoreId { get; set; }
        public abstract Message AsMessage();
    }
}