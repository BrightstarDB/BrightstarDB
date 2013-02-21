using System;
using SmartAssembly.Attributes;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Assembly attribute that specifies the default base URI for all PropertyType and Entity attributes
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)]
    [DoNotObfuscateType, DoNotPruneType]
    public class TypeIdentifierPrefixAttribute : Attribute
    {
        /// <summary>
        /// Get or set the base URI to use for all <see cref="PropertyTypeAttribute"/> and <see cref="EntityAttribute"/> attributes that
        /// have relative URIs
        /// </summary>
        public string BaseUri {get;set;}
        
        /// <summary>
        /// Creates a new instace of this assembly attribute
        /// </summary>
        /// <param name="baseUri">The default base URI</param>
        public TypeIdentifierPrefixAttribute(string baseUri)
        {
            BaseUri = baseUri;
        }
    }
}
