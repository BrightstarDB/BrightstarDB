using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Writing;
using StringWriter = VDS.RDF.Writing.StringWriter;

namespace BrightstarDB.SdShare.Client
{
    public class SparqlUpdateAdaptor : BaseAdaptor
    {
        public string SparqlEndpoint { get; set; }
        public string GraphUri { get; set; }

        #region Overrides of BaseAdaptor

        /// <summary>
        /// Allows for specific adaptor configuration
        /// </summary>
        /// <param name="config"></param>
        public override void Initialize(XElement config)
        {
            FeedName = GetElementValue(config, "Feed");
            SparqlEndpoint = GetElementValue(config, "SparqlServiceEndpoint");
            GraphUri = GetElementValue(config, "GraphUri");
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
                var g = new Graph();
                var rdfXmlParser = new RdfXmlParser();
                rdfXmlParser.Load(g, new StringReader(resourceDescription.ToString()));
                var ntWriter = new NTriplesWriter();
                var data = StringWriter.Write(g, ntWriter);

                var sparqlUpdate = "";
                sparqlUpdate += "DELETE { GRAPH <" + GraphUri + "> { <" + resourceUri + ">  ?x  ?y }}";
                sparqlUpdate += "USING <" + GraphUri + "> WHERE {<" + resourceUri + "> ?x ?y } ;";
                sparqlUpdate += "INSERT DATA { GRAPH <" + GraphUri + "> {  ";
                sparqlUpdate += data;
                sparqlUpdate += " }};";

                // execute update
                var wr = WebRequest.Create(SparqlEndpoint) as HttpWebRequest;
                wr.ContentType = "application/sparql-update";
                wr.Method = "POST";
                var ds = wr.GetRequestStream();
                var bytes = Encoding.ASCII.GetBytes(sparqlUpdate);
                ds.Write(bytes, 0, bytes.Length);
                var resp = wr.GetResponse() as HttpWebResponse;

                if (resp.StatusCode == HttpStatusCode.OK)
                {
                    Logging.LogDebug("Update successful");
                }
                else
                {
                    Logging.LogDebug("Not a 200");
                }
                resp.Close();
            } catch(Exception ex)
            {
                Logging.LogError(1, "Unable to apply fragment for resourceUri {0} {1}", ex.Message, ex.StackTrace);
            }
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
