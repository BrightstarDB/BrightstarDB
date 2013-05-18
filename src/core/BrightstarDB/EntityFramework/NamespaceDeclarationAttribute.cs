using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Assembly attribute that specifies a prefix mapping for a namespace
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class NamespaceDeclarationAttribute : Attribute
    {
        /// <summary>
        /// The prefix used to reference the namespace
        /// </summary>
        public string Prefix { get; set; }
        /// <summary>
        /// The base namespace URI that the prefix references
        /// </summary>
        public string Reference { get; set; }
        /// <summary>
        /// Creates a new namespace declaration
        /// </summary>
        /// <param name="prefix">The prefix used for the namespace</param>
        /// <param name="reference">The base namespace URI</param>
        public NamespaceDeclarationAttribute(string prefix, string reference)
        {
            Prefix = prefix;
            Reference = reference;
        }
    }
}