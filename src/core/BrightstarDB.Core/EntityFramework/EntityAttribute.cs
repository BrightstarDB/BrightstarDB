using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Interface attribute that identifies the interface as being an EntityFramework entity type.
    /// The optional Identifier property specifies the URI for the entity type. This can be either 
    /// a relative or an absolute URI. Relative URIs are resolved relative to 
    /// the base URI specified by the <see cref="TypeIdentifierPrefixAttribute"/> on the assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface|AttributeTargets.Class, AllowMultiple=false)]
    public sealed class EntityAttribute : RelativeOrAbsoluteIdentifierAttribute
    {
        /// <summary>
        /// Default attribute constructor
        /// </summary>
        public EntityAttribute() : base(null) {}

        /// <summary>
        /// Creates an attribute with the entity type identifier defined
        /// </summary>
        /// <param name="entityTypeIdentifier">A URL or CURIE that specifies the type of the entity</param>
        public EntityAttribute(string entityTypeIdentifier = null) : base(entityTypeIdentifier)
        {
        }
    }
}