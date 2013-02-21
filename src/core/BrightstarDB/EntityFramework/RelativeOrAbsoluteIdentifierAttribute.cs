using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// The base class for attributes whose value is a relative or absolute URI identifier
    /// </summary>
    [DoNotObfuscateType, DoNotPruneType]
    public class RelativeOrAbsoluteIdentifierAttribute : Attribute
    {
        /// <summary>
        /// Get or set the relative or absolute URI identifier
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Creates a new instance of this attribute
        /// </summary>
        /// <param name="relativeOrAbsoluteUri">The attribute's <see cref="Identifier"/> value.</param>
        public RelativeOrAbsoluteIdentifierAttribute(string relativeOrAbsoluteUri)
        {
            Identifier = relativeOrAbsoluteUri;
        }
    }
}