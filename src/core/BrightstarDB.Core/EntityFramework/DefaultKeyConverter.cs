using System;
using System.Globalization;
using System.Linq;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// A basic implementation of the <see cref="IKeyConverter"/> interface.
    /// </summary>
    /// <remarks>This implementation currently only handles</remarks>
    public class DefaultKeyConverter : IKeyConverter
    {
        /// <summary>
        /// Generates the key string for the provided values
        /// </summary>
        /// <param name="keyValues">The values to be serialized into a key</param>
        /// <param name="keySeparator">The separator to insert between key values</param>
        /// <param name="forType">The type of entity the key is generated for</param>
        /// <remarks>Null values in <paramref name="keyValues"/> are ignored. Int, long and decimal values are converted
        /// to a string using the InvariantCulture format. String values are copied as is. Values of other types are converted
        /// to a string by calling their <see cref="Object.ToString()"/> method. The resulting strings are then encoded and
        /// if there are multiple non-null values they are joined using <paramref name="keySeparator"/> as the separator. The
        /// value of <paramref name="keySeparator"/> is NOT URI-encoded.</remarks>
        /// <returns>The generated key string or null if <paramref name="keyValues"/> is empty after all null values are removed.</returns>
        public virtual string GenerateKey(object[] keyValues, string keySeparator, Type forType)
        {
            keyValues = keyValues.Where(x => x != null).ToArray();
            if (keyValues.Length == 0) return null;
            return keyValues.Length == 1 ? Convert(keyValues[0]) : String.Join(keySeparator, keyValues.Select(Convert));
        }

        /// <summary>
        /// Converts a single value to its URI encoded string form
        /// </summary>
        /// <param name="v">The value to be converted</param>
        /// <returns>The URI encoded string</returns>
        public virtual string Convert(object v)
        {
            string ret;
            if (v is int)
            {
                ret=((int)v).ToString(CultureInfo.InvariantCulture);
            }
            else if (v is long)
            {
                ret=((long) v).ToString(CultureInfo.InvariantCulture);
            }
            else if (v is decimal)
            {
                ret = ((decimal) v).ToString(CultureInfo.InvariantCulture);
            }
            else if (v is string)
            {
                ret = v as string;
            }
            else if (v is IEntityObject)
            {
                ret = (v as IEntityObject).GetKey();
            }
            else ret = v.ToString();
            return Uri.EscapeUriString(ret);
        }
    }
}
