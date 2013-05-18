using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Property attribute which flags the property that is bound to the resource address
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple=false)]
    public sealed class IdentifierAttribute : Attribute
    {
        /// <summary>
        /// Get or set the base address URI used by the decorated property
        /// </summary>
        public string BaseAddress { get; set; }
        /// <summary>
        /// Used to decorate a property that provides a full URI identifier for an entity
        /// </summary>
        public IdentifierAttribute()
        {
            BaseAddress = null;
        }
        /// <summary>
        /// Used to decorate a property that provides a relative URI identifier for an entity
        /// </summary>
        /// <param name="baseAddress">The base URI that is used to resolve the relative identifier to a full URI</param>
        public IdentifierAttribute(string baseAddress)
        {
            BaseAddress = baseAddress;
        }
    }
}