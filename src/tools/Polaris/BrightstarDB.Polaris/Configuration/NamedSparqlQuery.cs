using System.Xml.Serialization;

namespace BrightstarDB.Polaris.Configuration
{
    public class NamedSparqlQuery
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Query { get; set; }
    }
}