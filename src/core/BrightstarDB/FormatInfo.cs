using System.Collections.Generic;
using System.Text;

namespace BrightstarDB
{
    /// <summary>
    /// Base class for format information providers
    /// </summary>
    public class FormatInfo : ISerializationFormat
    {
        /// <summary>
        /// Get the user-friendly display name for this format
        /// </summary>
        public string DisplayName { get; protected set; }

        /// <summary>
        /// Get the list of media types that are recognized for this results format
        /// </summary>
        public List<string> MediaTypes { get; protected set; }

        /// <summary>
        /// The default file extension to use for this results format
        /// </summary>
        public string DefaultExtension { get; protected set; }

        /// <summary>
        /// Returns the model(s) that the format can be used to serialize
        /// </summary>
        public SerializableModel SerializedModel { get; protected set; }

        /// <summary>
        /// The encoding to be used when streaming the result
        /// </summary>
        public Encoding Encoding { get; protected set; }

        /// <summary>
        /// Returns the string representation of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
#if WINDOWS_PHONE || PORTABLE
            return MediaTypes[0] + "; charset=" + Encoding.WebName;
#else
            return MediaTypes[0] + "; charset=" + Encoding.HeaderName;
#endif
        }
    }
}