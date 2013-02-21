using System;
using System.Linq;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.Client
{
    public abstract class BaseAdaptor : ISdShareClientAdaptor
    {
        #region Implementation of ISdShareClientAdaptor        
      
        /// <summary>
        /// Allows for specific adaptor configuration
        /// </summary>
        /// <param name="config"></param>
        public abstract void Initialize(XElement config);

        /// <summary>
        /// Updates the underlying store with this as the new representation of the identified resource
        /// </summary>
        /// <param name="resourceUri">The resource for whom this is the representation</param>
        /// <param name="resourceDescription">The rdf xml document that describes the resource</param>
        public abstract void ApplyFragment(string resourceUri, XDocument resourceDescription);

        #endregion

        public string FeedName { get; set; }
    }
}
