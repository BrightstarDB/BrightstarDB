using System;

namespace BrightstarDB
{
    /// <summary>
    /// Type of exception raised when a caller does not provide a required output format to a method/
    /// </summary>
    public class NoAcceptableFormatException : BrightstarException
    {
        /// <summary>
        /// Get the type of format specifier that was expected
        /// </summary>
        public Type RequiredFormatType { get; private set; }

        internal NoAcceptableFormatException(Type requiredFormatType, string message) : base(message)
        {
            RequiredFormatType = requiredFormatType;
        }
    }
}
