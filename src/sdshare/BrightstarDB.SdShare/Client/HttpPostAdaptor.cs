using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace BrightstarDB.SdShare.Client
{
    public class HttpPostAdaptor : BaseAdaptor
    {
        private string _endpoint;
        private string _resourceParameterName;

        #region Implementation of ISdShareClientAdaptor

        /// <summary>
        /// Allows for specific adaptor configuration
        /// </summary>
        /// <param name="config"></param>
        public override void Initialize(XElement config)
        {
            FeedName = GetElementValue(config, "Feed");
            _endpoint = GetElementValue(config, "Endpoint");
            _resourceParameterName = GetElementValue(config, "ResourceParameterName");
            if (_resourceParameterName == null) _resourceParameterName = "uri";
        }

        /// <summary>
        /// Updates the underlying store with this as the new representation of the identified resource
        /// </summary>
        /// <param name="resourceUri">The resource for whom this is the representation</param>
        /// <param name="resourceDescription">The rdf xml document that describes the resource</param>
        public override void ApplyFragment(string resourceUri, XDocument resourceDescription)
        {
            try
            {
                var wr = WebRequest.Create(_endpoint + "?" + _resourceParameterName + "=" + resourceUri);
                wr.Method = "POST";
                wr.ContentType = "application/rdf+xml";
                var reqstream = wr.GetRequestStream();
                using (var strwriter = new StreamWriter(reqstream))
                {
                   strwriter.WriteLine(resourceDescription.ToString()); 
                }                
                
                // get response
                var resp = wr.GetResponse() as HttpWebResponse;
                if (resp.StatusCode != HttpStatusCode.OK || resp.StatusCode != HttpStatusCode.Accepted)
                {
                    Logging.LogError(1, "Error in apply fragment. Remote server returned code {0}", resp.StatusCode);
                }
                resp.Close();
            } catch(Exception ex)
            {
                Logging.LogError(1, "Error in apply fragment {0}", ex.Message);
            }
        }
       
        #endregion

        protected static string GetElementValue(XElement parent, string name)
        {
            var elem = parent.Elements(name).FirstOrDefault();
            if (elem == null) return null;
            return elem.Value;
        }
    }
}
