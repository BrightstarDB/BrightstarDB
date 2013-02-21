using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Attribute applied to an entity property, that specifies the name of the 
    /// inverse property. The inverse property will be located by examining the
    /// property return type of the decorated property.
    /// </summary>
    [DoNotObfuscateType, DoNotPruneType]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class InversePropertyAttribute : Attribute
    {
        /// <summary>
        /// Get or set the name of the property that this property is an inverse of
        /// </summary>
        public string InversePropertyName { get; set; }

        /// <summary>
        /// Used to decorate a property that is the inverse of another property 
        /// </summary>
        /// <param name="inversePropertyName">The name of the property that this property is the inverse of</param>
        /// <remarks>The property named by <paramref name="inversePropertyName"/> must be the name of a property
        /// on the type returned by the decorated property (or on the type of the items in the returned
        /// collection if the decorated property type is a collection type)</remarks>
        public InversePropertyAttribute(string inversePropertyName)
        {
            InversePropertyName = inversePropertyName;
        }
    }
}