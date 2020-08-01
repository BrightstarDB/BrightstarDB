using System;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// Represents an RDF plain literal consisting of a string value and a language tag
    /// </summary>
    /// <remarks>PlainLiteral instances are immutable - once created you can access the
    /// Value and Language properties only for read. To update the value or language
    /// of a PlainLiteral property you must create a new PlainLiteral instance
    /// and assign it to that property.</remarks>
    public class PlainLiteral
    {
        private string _value;
        private string _language;

        /// <summary>
        /// Creates an empty plain literal with no value and a default language tag
        /// </summary>
        public PlainLiteral()
            : this(null, null)
        {

        }

        /// <summary>
        /// Creates a plain literal with the specified string value and a default language tag
        /// </summary>
        /// <param name="value">The string value of the literal</param>
        public PlainLiteral(string value)
            : this(value, null)
        {
        }

        /// <summary>
        /// Creates a plain literal with the specified string value and langauge tag
        /// </summary>
        /// <param name="value">The string value of the literal</param>
        /// <param name="languageTag">The language tag of the literal</param>
        /// <remarks>Language tags are normalized to lower-case</remarks>
        public PlainLiteral(string value, string languageTag)
        {
            Value = value;
            Language = languageTag;
        }

        /// <summary>
        /// Declared conversion from a string value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>A new PlainLiteral instance with no language tag </returns>
        static public implicit operator PlainLiteral(string value)
        {
            return new PlainLiteral(value);
        }

        /// <summary>
        /// Declared conversion to a string value
        /// </summary>
        /// <param name="literal"></param>
        /// <returns>The <see cref="Value"/> property of the PlainLiteral instance</returns>
        static public implicit operator string(PlainLiteral literal)
        {
            return literal.Value;
        }

        /// <summary>
        /// Get or set the string value of the literal
        /// </summary>
        public string Value { get { return _value; } private set { _value = (value ?? String.Empty); } }

        /// <summary>
        /// Get or set the language tag for the literal.
        /// </summary>
        /// <remarks>Returns <see cref="String.Empty"/> if the literal has no language tag. Use String.Empty or NULL to clear an existing language tag.</remarks>
        public string Language
        {
            get { return _language; }
            private set { _language = (value == null ? String.Empty : value.ToLowerInvariant()); }
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param><filterpriority>2</filterpriority>
        public override bool Equals(object obj)
        {
            var other = obj as PlainLiteral;
            if (other != null)
            {
                return Value.Equals(other.Value) && Language.Equals(other.Language);
            }
            return false;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return Value.GetHashCode() ^ Language.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return Value;
        }
    }
}
