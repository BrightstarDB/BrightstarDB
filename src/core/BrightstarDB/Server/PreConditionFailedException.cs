using System;

namespace BrightstarDB.Server
{
#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    internal class PreconditionFailedException : BrightstarInternalException
    {
        public int ExistanceFailureCount { get; private set; }
        public string ExistanceFailedTriples { get; private set; }
        public int NonExistanceFailureCount { get; private set; }
        public string NonExistanceFailedTriples { get; private set; }
        private string _msg;
        
        public PreconditionFailedException(int existancePreconditionFailureCount, string existancePreconditionFailedNTriples,
            int nonExistancePreconditionFailureCount, string nonExistancePreconditionFailedNTriples) : base(Strings.PreconditionFailedBasicMessage)
        {
            ExistanceFailureCount = existancePreconditionFailureCount;
            ExistanceFailedTriples = existancePreconditionFailedNTriples;
            NonExistanceFailureCount = nonExistancePreconditionFailureCount;
            NonExistanceFailedTriples = nonExistancePreconditionFailedNTriples;
            _msg = String.Format(Strings.PreconditionFailedFullMessage, existancePreconditionFailureCount,
                              existancePreconditionFailedNTriples, nonExistancePreconditionFailureCount,
                              nonExistancePreconditionFailedNTriples);

        }

        public override string Message
        {
            get
            {
                return _msg;
            }
        }
    }

}
