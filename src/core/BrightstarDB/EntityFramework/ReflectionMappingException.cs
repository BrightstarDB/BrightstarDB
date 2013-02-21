using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Class of exception raised when the <see cref="ReflectionMappingProvider"/> cannot process an entity interface or implementation class.
    /// </summary>
    [DoNotObfuscateType, DoNotPruneType]
    public class ReflectionMappingException : EntityFrameworkException
    {
        /// <summary>
        /// Create a new ReflectionMappingException
        /// </summary>
        /// <param name="message">The detail message for the exception</param>
        internal ReflectionMappingException(string message) : base(message)
        {
        }
    }
}