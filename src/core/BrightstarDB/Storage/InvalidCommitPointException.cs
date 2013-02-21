using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.Storage
{
#if !SILVERLIGHT
    [Serializable]
#endif
    [DoNotObfuscate]
    internal class InvalidCommitPointException : BrightstarInternalException
    {
        public InvalidCommitPointException(string msg) : base(msg){}
        public InvalidCommitPointException(string msg, Exception inner) : base(msg, inner) {}
    }
}
