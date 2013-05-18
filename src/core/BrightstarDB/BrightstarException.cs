using System;

namespace BrightstarDB
{
#if !SILVERLIGHT
    /// <summary>
    /// The base class for all custom exception types raised by Brightstar
    /// </summary>
    [Serializable]
#else
    /// <summary>
    /// The base class for all custom exception types raised by Brightstar
    /// </summary>
#endif
    public abstract class BrightstarException : Exception
    {
        internal BrightstarException(string msg) :base(msg){}
        internal BrightstarException(string msg, Exception inner) : base(msg, inner){}
    }
}
