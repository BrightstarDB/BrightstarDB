using System.Text;
using System.Xml.Linq;
using NetworkedPlanet.Brightstar.Client;
using NetworkedPlanet.Brightstar.LinkedDataServer.Resources;
using OpenRasta.Codecs;
using OpenRasta.Web;

namespace NetworkedPlanet.Brightstar.LinkedDataServer.Codecs
{
    [MediaType("application/xml; charset=utf-16", "xml")]
    [MediaType("application/sparql-results+xml; charset=utf-16", "xml")]
    [MediaType("application/rdf+xml; charset=utf-16", "xml")]
    public class SparqlResultCodec : IMediaTypeWriter
    {
        #region Implementation of ICodec

        public object Configuration { get; set; }

        public void WriteTo(object entity, IHttpEntity response, string[] codecParameters)
        {
            if (entity == null || !(entity is SparqlEndpoint)) return;
            var sparqlEndpoint = entity as SparqlEndpoint;
            var client = BrightstarService.GetClient();
            using (var resultStream = client.ExecuteQuery(sparqlEndpoint.Store, sparqlEndpoint.SparqlQuery))
            {
                var resultsDoc = XDocument.Load(resultStream);
                var resultsWriter = new System.Xml.XmlTextWriter(response.Stream, Encoding.Unicode);
                resultsDoc.WriteTo(resultsWriter);
                resultsWriter.Flush();
            }
        }

        #endregion
    }
}