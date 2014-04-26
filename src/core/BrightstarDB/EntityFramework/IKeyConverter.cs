using System;

namespace BrightstarDB.EntityFramework
{
    /// <summary>
    /// Interface to be implemented by classes that provide the service of converting a colleciton
    /// of key values into a string for use in entity identifiers.
    /// </summary>
    public interface IKeyConverter
    {
        /// <summary>
        /// When this method is implemented in a class, it should return a URI-path encoded string
        /// that encodes the values in <paramref name="keyValues"/>.
        /// </summary>
        /// <param name="keyValues">The composite key values to be encoded</param>
        /// <param name="keySeparator">The configured separator string to insert between key values</param>
        /// <param name="forType">The type of entity that the key is being encoded for</param>
        /// <remarks> The <paramref name="keySeparator"/>
        /// and <paramref name="forType"/> parameters are provided for information. The converter
        /// SHOULD honour the <paramref name="keySeparator"/> parameter when inserting separators between
        /// key values.</remarks>
        /// <returns>A URI-path encoded string</returns>
        string GenerateKey(object[] keyValues, string keySeparator, Type forType);
    }
}
