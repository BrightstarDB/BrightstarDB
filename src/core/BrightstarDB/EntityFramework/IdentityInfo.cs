using System.Reflection;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Cached identity information structure
    /// </summary>
    public class IdentityInfo
    {

        /// <summary>
        /// Creates a new cached identity information structure
        /// </summary>
        /// <param name="baseUri">The base URI for the identifier</param>
        /// <param name="identityProperty">The property that will hold the entity key</param>
        /// <param name="keyProperties">The property or properties that together are used to construct the entity key</param>
        /// <param name="keySeparator">The string separator to insert between key values in a composite key</param>
        /// <param name="keyConverter">The converter responsible for converting key property values into a single composite key string</param>
        public IdentityInfo(string baseUri, PropertyInfo identityProperty, PropertyInfo[] keyProperties, string keySeparator,
                                 IKeyConverter keyConverter)
        {
            BaseUri = baseUri;
            IdentityProperty = identityProperty;
            KeyProperties = keyProperties;
            KeySeparator = keySeparator;
            KeyConverter = keyConverter;
        }

        /// <summary>
        /// The base string for generated identity URIs
        /// </summary>
        public string BaseUri { get; private set; }

        /// <summary>
        /// The property that provides access to the generated key
        /// </summary>
        public PropertyInfo IdentityProperty { get; private set; }

        /// <summary>
        /// The properties to use to generate a key.
        /// </summary>
        /// <remarks>NULL if the identifier is not generated from entity property values</remarks>
        public PropertyInfo[] KeyProperties { get; private set; }
            
        /// <summary>
        /// The separator to insert between values when multiple properties
        /// are used to generate the key
        /// </summary>
        public string KeySeparator { get; private set; }

        /// <summary>
        /// Get the converter that is responsible for converting the key property 
        /// values of an entity into a single composite key string
        /// </summary>
        public IKeyConverter KeyConverter { get; private set; }

        /// <summary>
        /// Return a flag indicating if type-based unique constraints should be enforced
        /// </summary>
        public bool EnforceClassUniqueConstraint
        {
            get { return KeyConverter != null; }
        }
    }
}