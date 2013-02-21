using System;
using System.Linq;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.Client
{
    public class FeedSource
    {
        public Uri CollectionUri { get; set; }
        public int CheckPeriod { get; set; }
        public string Name { get; set; }

        public void Initialize(XElement element)
        {
            Name = GetElementValue(element, "Name");
            CheckPeriod = int.Parse(GetElementValue(element, "CheckPeriod"));
            CollectionUri = new Uri(GetElementValue(element, "Url"));
        }

        private static string GetElementValue(XContainer parent, string name)
        {
            var elem = parent.Elements(name).FirstOrDefault();
            if (elem == null) throw new Exception("Missing element " + name);
            return elem.Value;
        }
    }
}
