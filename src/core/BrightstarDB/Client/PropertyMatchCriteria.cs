using System;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Defines a resource property match
    /// </summary>
    public class PropertyMatchCriteria
    {
        /// <summary>
        /// Creates a new match criterion
        /// </summary>
        /// <param name="type">The property type to match</param>
        /// <param name="value">The expected value</param>
        /// <param name="langCode">OPTIONAL: The expected language code</param>
        /// <remarks>To match another resource, pass a <see cref="IDataObject"/> instance for <paramref name="value"/>,
        /// to match a literal, pass the literal value to match.</remarks>
        public PropertyMatchCriteria(IDataObject type, object value, string langCode = null)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (value == null) throw new ArgumentNullException("value");
            Type = type;
            Value = value;
            LangCode = langCode;
        }

        /// <summary>
        /// Gets the property type matched by this criterion
        /// </summary>
        public IDataObject Type { get; private set; }

        /// <summary>
        /// Gets the value matched by this criterion
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Gets the language code matched by this criterion
        /// </summary>
        /// <remarks>This property may be NULL</remarks>
        public string LangCode { get; private set; }
    }
}
