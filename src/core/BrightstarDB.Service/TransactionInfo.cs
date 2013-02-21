using System;
using System.Runtime.Serialization;
using BrightstarDB.Storage;

namespace BrightstarDB.Service
{
    [DataContract(Namespace = "http://brightstardb.com/schemas/servicedata/")]
    public class TransactionInfo
    {
        [DataMember]
        public string StoreName { get; set; }

        [DataMember]
        public ulong Id { get; set; }

        [DataMember]
        public DateTime StartTime { get; set; }

        [DataMember]
        public TransactionType TransactionType { get; set; }

        [DataMember]
        public TransactionStatus Status { get; set; }

        [DataMember]
        public Guid JobId { get; set; }

    }
}