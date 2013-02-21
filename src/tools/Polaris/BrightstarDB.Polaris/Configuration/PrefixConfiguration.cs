using System.Xml.Serialization;

namespace BrightstarDB.Polaris.Configuration
{
    public class PrefixConfiguration
    {
        [XmlAttribute("prefix")]
        public string Prefix { get; set; }

        [XmlAttribute("uri")]
        public string Uri { get; set; }
    }
}