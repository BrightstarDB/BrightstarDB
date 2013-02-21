using System;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace BrightstarDB.Service
{
    [DataContract(Namespace = "http://brightstardb.com/schemas/servicedata/")]
    public class JobInfo
    {
        [DataMember]
        public bool JobPending { get; set; }
        [DataMember]
        public bool JobStarted { get; set; }
        [DataMember]
        public bool JobCompletedWithErrors { get; set; }
        [DataMember]
        public bool JobCompletedOk { get; set; }
        [DataMember]
        public string StatusMessage { get; set; }
        [DataMember]
        public ExceptionDetail ExceptionInfo { get; set; }
        [DataMember]
        public string JobId { get; set; }
        [DataMember]
        public DateTime JobStartedAt { get; set; }
        [DataMember]
        public DateTime JobEndedAt { get; set; }
    }
}