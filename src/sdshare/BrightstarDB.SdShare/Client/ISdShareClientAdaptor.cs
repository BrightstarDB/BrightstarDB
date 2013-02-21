using System.Xml.Linq;

namespace BrightstarDB.SdShare.Client
{
    public interface ISdShareClientAdaptor
    {
        /// <summary>
        /// Allows for specific adaptor configuration
        /// </summary>
        /// <param name="config"></param>
        void Initialize(XElement config);

        /// <summary>
        /// Updates the underlying store with this as the new representation of the identified resource
        /// </summary>
        /// <param name="resourceUri">The resource for whom this is the representation</param>
        /// <param name="resourceDescription">The rdf xml document that describes the resource</param>
        void ApplyFragment(string resourceUri, XDocument resourceDescription);

        /// <summary>
        /// The name of the feed that this adaptor consumes
        /// </summary>
        string FeedName { get; set; }
    }
}
