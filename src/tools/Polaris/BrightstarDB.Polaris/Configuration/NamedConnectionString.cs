using System.Xml.Serialization;

namespace BrightstarDB.Polaris.Configuration
{
    public class NamedConnectionString
    {
        /// <summary>
        /// Get or set the user-defined name for the connection string
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Get or set the connection string value
        /// </summary>
        [XmlAttribute("value")]
        public string ConnectionString { get; set; }
    }
}