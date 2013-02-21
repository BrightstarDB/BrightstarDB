using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.Server
{
#if !SILVERLIGHT
    [Serializable]
#endif
    [DoNotObfuscate]
    internal class BrightstarClientDataException : BrightstarException
    {
        public BrightstarClientDataException(string msg) : base(msg)
        {
        }

        public BrightstarClientDataException(string msg, Exception inner) : base(msg, inner)
        {
        }
    }
}
