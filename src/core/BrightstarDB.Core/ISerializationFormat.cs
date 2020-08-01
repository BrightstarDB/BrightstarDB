using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB
{
    /// <summary>
    /// Interface that describes the structure and content of a serialization format
    /// </summary>
    public interface ISerializationFormat
    {
        /// <summary>
        /// Returns a of the MIME types that can be applied to this serialization format
        /// </summary>
        List<string> MediaTypes { get; }

        /// <summary>
        /// Returns the text encoding used by the serialization format
        /// </summary>
        Encoding Encoding { get; }

        /// <summary>
        /// Returns the default filename extension for the serialization format
        /// </summary>
        String DefaultExtension { get; }

        
        /// <summary>
        /// Returns the model that 
        /// </summary>
        SerializableModel SerializedModel { get; }
    }
}
