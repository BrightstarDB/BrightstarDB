using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Exception raised when an entity for a specific .NET type cannot be found
    /// </summary>
    public class MappingNotFoundException : EntityFrameworkException
    {
        /// <summary>
        /// Get the type for which the mapping was not found
        /// </summary>
        public Type UnmappedType { get; private set; }

        /// <summary>
        /// Creates a new instance of the exception class
        /// </summary>
        /// <param name="unmappedType">The type for which an entity mapping was not found</param>
        public MappingNotFoundException(Type unmappedType) :
            base(String.Format("No URI mapping found for class {0}", unmappedType ))
        {
            UnmappedType = unmappedType;
        }
    }
}
