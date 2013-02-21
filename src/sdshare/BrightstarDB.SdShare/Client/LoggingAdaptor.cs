using System;
using System.Linq;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.Client
{
    public class LoggingAdaptor : BaseAdaptor
    {
        #region Implementation of ISdShareClientAdaptor

        /// <summary>
        /// Allows for specific adaptor configuration
        /// </summary>
        /// <param name="config"></param>
        public override void Initialize(XElement config)
        {
            FeedName = GetElementValue(config, "Feed");
        }

        /// <summary>
        /// Updates the underlying store with this as the new representation of the identified resource
        /// </summary>
        /// <param name="resourceUri">The resource for whom this is the representation</param>
        /// <param name="resourceDescription">The rdf xml document that describes the resource</param>
        public override void ApplyFragment(string resourceUri, XDocument resourceDescription)
        {
            Logging.LogInfo("Apply Fragment {0} {1}", resourceUri, resourceDescription.ToString());
        }

        #endregion

        protected static string GetElementValue(XElement parent, string name)
        {
            var elem = parent.Elements(name).FirstOrDefault();
            if (elem == null) throw new Exception("Missing element " + name);
            return elem.Value;
        }
    }
}
