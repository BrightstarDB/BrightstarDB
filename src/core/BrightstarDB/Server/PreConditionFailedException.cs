using System;
using System.Collections.Generic;
using BrightstarDB.Dto;

namespace BrightstarDB.Server
{
#if !SILVERLIGHT && !PORTABLE
    [Serializable]
#endif
    internal class PreconditionFailedException : BrightstarInternalException
    {
        public int ExistenceFailureCount { get; private set; }
        public string ExistenceFailedTriples { get; private set; }
        public int NonExistenceFailureCount { get; private set; }
        public string NonExistenceFailedTriples { get; private set; }
        private readonly string _msg;
        
        public PreconditionFailedException(int existancePreconditionFailureCount, string existancePreconditionFailedNTriples,
            int nonExistancePreconditionFailureCount, string nonExistancePreconditionFailedNTriples) : base(Strings.PreconditionFailedBasicMessage)
        {
            ExistenceFailureCount = existancePreconditionFailureCount;
            ExistenceFailedTriples = existancePreconditionFailedNTriples;
            NonExistenceFailureCount = nonExistancePreconditionFailureCount;
            NonExistenceFailedTriples = nonExistancePreconditionFailedNTriples;
            _msg = String.Format(Strings.PreconditionFailedFullMessage, existancePreconditionFailureCount, nonExistancePreconditionFailureCount);
        }

        public override string Message
        {
            get
            {
                return _msg;
            }
        }

        public ExceptionDetailObject AsExceptionDetailObject()
        {
            return new ExceptionDetailObject(this, new Dictionary<string, string>
                {
                    {"existenceFailedTriples", ExistenceFailedTriples},
                    {"nonexistenceFailedTriples", NonExistenceFailedTriples}
                });
        }
    }

}
