using System;
using System.Runtime.Serialization;

namespace BrightstarDB.Service
{
    [DataContract(Namespace = "http://brightstardb.com/schemas/servicedata/")]
    public class CommitPointInfo
    {
        [DataMember]
        public string StoreName { get; set; }

        [DataMember]
        public ulong Id { get; set; }

        [DataMember]
        public DateTime CommitTime { get; set; }

        [DataMember]
        public Guid JobId { get; set; }
    }
}