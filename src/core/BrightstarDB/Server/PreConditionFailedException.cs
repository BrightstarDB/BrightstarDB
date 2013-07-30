using System;

namespace BrightstarDB.Server
{
#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    internal class PreconditionFailedException : BrightstarInternalException
    {
        public int FailureCount { get; private set; }
        public string FailedTriples { get; private set; }

        public PreconditionFailedException(int failureCount, string failedNTriples) : base("Transaction preconditions were not met.\n" + failedNTriples)
        {
            FailureCount = failureCount;
            FailedTriples = failedNTriples;
        }

    }
}
