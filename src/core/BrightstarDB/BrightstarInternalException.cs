using System;
using SmartAssembly.Attributes;

namespace BrightstarDB
{
    /// <summary>
    /// Class of exception that indicates an internal processing error within the Brightstar engine.
    /// </summary>
    /// <remarks>This exception type is reserved for exceptions relating to Brightstar internal processing.</remarks>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DoNotObfuscate]
    public class BrightstarInternalException : BrightstarException
    {
        ///<summary>
        /// Used to indicate an exception that has occurred in Brightstar
        ///</summary>
        ///<param name="msg">The exception message</param>
        internal BrightstarInternalException(string msg) : base(msg){}

        ///<summary>
        /// Used to indicate an exception that has occurred in brightstar
        ///</summary>
        ///<param name="msg">The exception message</param>
        ///<param name="inner">The exception that caused this exception to be raised</param>
        internal BrightstarInternalException(string msg, Exception inner) : base(msg, inner) {}
    }
}
