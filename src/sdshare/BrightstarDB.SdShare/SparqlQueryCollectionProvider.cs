using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Xml.Linq;

namespace BrightstarDB.SdShare
{
    public class SparqlQueryCollectionProvider : CollectionProviderBase
    {
        private string _sparqlEndpoint;
        private string _fragmentsQuery;
        private string _snapshotQuery;
        private string _fragmentQuery;

        #region Overrides of CollectionProviderBase

        public override void Initialize(XElement configRoot)
        {
            Description = "Sparql SdShare Provider";
            Name = "Sparql SdShare Provider";
            _sparqlEndpoint = GetElementValue(configRoot, "SparqlEndpoint");
            _fragmentsQuery = GetElementValue(configRoot, "FragmentsQuery");
            _fragmentQuery = GetElementValue(configRoot, "FragmentQuery");
            _snapshotQuery = GetElementValue(configRoot, "SnapshotQuery");
            RawConfig = configRoot.ToString();
            IsEnabled = true;
        }

        public override IEnumerable<ISnapshot> GetSnapshots()
        {
            return new List<ISnapshot>() {new Snapshot() {Id = "Everything", PublishedDate = DateTime.UtcNow, Name = "All Data"}};
        }

        public override IEnumerable<IFragment> GetFragments(DateTime since, DateTime before)
        {
            // format the query
            var sinceString = since.ToString("u");
            var sparqlQuery = string.Format(_fragmentsQuery, "\"" + sinceString + "\"^^<http://www.w3.org/2001/XMLSchema#date>");
            
            // make request
            var wc = new WebClient();
            wc.Headers.Add("Content-Type", "application/sparql-query");
            var result = XDocument.Parse(wc.UploadString(_sparqlEndpoint, sparqlQuery));
            
            // process the result
            foreach (var sparqlResultRow in result.SparqlResultRows())
            {
                var resource = sparqlResultRow.GetColumnValue("resource") as Uri;
                var updated = (DateTime) sparqlResultRow.GetColumnValue("updated");
                if (resource == null || updated == null) continue;

                yield return
                    new Fragment()
                        {
                            PublishDate = updated,
                            ResourceId = resource.AbsoluteUri,
                            ResourceName = resource.AbsoluteUri,
                            ResourceUri = resource.AbsoluteUri
                        };
            }
        }

        public override Stream GetFragment(string id, string mimeType)
        {
            // build query
            var query = string.Format(_fragmentQuery, id);

            // invoke query
            var wr = WebRequest.Create(_sparqlEndpoint);
            wr.Method = "POST";
            wr.ContentType = "application/sparql-query";
            using (var writer = new StreamWriter(wr.GetRequestStream()))
            {
                writer.Write(query);
            }

            var resp = wr.GetResponse();
            return resp.GetResponseStream();
        }

        public override Stream GetSnapshot(string id, string mimeType)
        {
            // invoke query
            var wr = WebRequest.Create(_sparqlEndpoint);
            wr.Method = "POST";
            wr.ContentType = "application/sparql-query";
            using (var writer = new StreamWriter(wr.GetRequestStream()))
            {
                writer.Write(_snapshotQuery);
            }

            var resp = wr.GetResponse();
            return resp.GetResponseStream();
        }

        public override Stream GetSample(string mimeType)
        {
            throw new NotImplementedException();
        }

        #endregion

        private static string GetElementValue(XElement parent, String name)
        {
            var elem = parent.Element(name);
            if (elem == null) return null;
            return elem.Value;
        }
    }
}
