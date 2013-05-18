using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Attribute used to decorate entity interface properties whose value is an arc of a particular property type
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class PropertyTypeAttribute : RelativeOrAbsoluteIdentifierAttribute
    {
        /// <summary>
        /// Creates a new instance of the PropertyType attribute
        /// </summary>
        /// <param name="relativeOrAbsoluteUri">The relative or absolute URI that specifies the property type for the arc</param>
        public PropertyTypeAttribute(string relativeOrAbsoluteUri)
            : base(relativeOrAbsoluteUri)
        {
        }
    }
}