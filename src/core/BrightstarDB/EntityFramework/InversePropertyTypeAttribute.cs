using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Attribute used to decorate entity interface properties whose value is an inverse arc of a particular property type
    /// </summary>
    [DoNotObfuscateType, DoNotPruneType]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class InversePropertyTypeAttribute : RelativeOrAbsoluteIdentifierAttribute
    {
        /// <summary>
        /// Creates a new instance of the InversePropertyType attribute
        /// </summary>
        /// <param name="relativeOrAbsoluteUri">The relative or absolute URI that specifies the property type for the inverse arc</param>
        public InversePropertyTypeAttribute(string relativeOrAbsoluteUri)
            : base(relativeOrAbsoluteUri)
        {
        }
    }
}