using System.Reflection;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Cached identity information structure
    /// </summary>
    internal class IdentityInfo
    {
        public IdentityInfo(string baseUri, PropertyInfo[] keyProperties, string keySeparator,
                                 IKeyConverter keyConverter)
        {
            BaseUri = baseUri;
            KeyProperties = keyProperties;
            KeySeparator = keySeparator;
            KeyConverter = keyConverter;
        }

        /// <summary>
        /// The base string for generated identity URIs
        /// </summary>
        public string BaseUri { get; private set; }

        /// <summary>
        /// The properties to use to generate a key
        /// </summary>
        public PropertyInfo[] KeyProperties { get; private set; }
            
        /// <summary>
        /// The separator to insert between values when multiple properties
        /// are used to generate the key
        /// </summary>
        public string KeySeparator { get; private set; }

        public IKeyConverter KeyConverter { get; private set; }
    }
}