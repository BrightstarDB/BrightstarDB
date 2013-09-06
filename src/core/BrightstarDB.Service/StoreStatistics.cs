using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BrightstarDB.Service
{
    [DataContract(Namespace = "http://brightstardb.com/schemas/servicedata/")]
    public class StoreStatistics
    {
        [DataMember]
        public ulong CommitId { get; set; }

        [DataMember]
        public DateTime CommitTimestamp { get; set; }

        [DataMember]
        public ulong TotalTripleCount { get; set; }

        [DataMember]
        public Dictionary<string, ulong> PredicateTripleCounts { get; set; } 
    }
}
