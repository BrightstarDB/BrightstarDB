using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base class for BrightstarDB Entity Framework exceptions
    /// </summary>
    [DoNotObfuscate, DoNotPrune]
    public class EntityFrameworkException : Exception
    {
        /// <summary>
        /// Creates a new instance of this exception with a string detail message
        /// </summary>
        /// <param name="message">The exception detail message</param>
        public EntityFrameworkException(string message) : base(message){}

        /// <summary>
        /// Creates a new instance of this exception with a string detail message 
        /// </summary>
        /// <param name="fmt">The detail message format string</param>
        /// <param name="args">The detail message string arguments</param>
        public EntityFrameworkException(string fmt, params object [] args) : base(String.Format(fmt, args))
        {
        }
    }
}
