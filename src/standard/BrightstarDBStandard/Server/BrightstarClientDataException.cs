using System;

namespace BrightstarDB.Server
{
#if !SILVERLIGHT && !PORTABLE && !NETCORE
    [Serializable]
#endif
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
