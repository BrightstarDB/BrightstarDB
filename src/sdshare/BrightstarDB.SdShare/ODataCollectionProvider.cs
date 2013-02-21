using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;
using VDS.RDF.Writing;

namespace BrightstarDB.SdShare
{
    public class ODataCollectionProvider : CollectionProviderBase
    {
        #region Overrides of CollectionProviderBase

        private string _odataEndpoint;
        private string _defaultSchemaNamespacePrefix;
        private string _rdfNamespacePrefix = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private string _rdfsNamespacePrefix = "http://www.w3.org/2000/01/rdf-schema#";

        public override void Initialize(XElement configRoot)
        {
            _odataEndpoint = null;
            _defaultSchemaNamespacePrefix = "http://www.brightstardb.com/odata/schema/";
            Description = "OData as SdShare RDF";
            Name = "OData";
            RawConfig = configRoot.ToString();
            IsEnabled = true;
        }

        public override IEnumerable<ISnapshot> GetSnapshots()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IFragment> GetFragments(DateTime since, DateTime before)
        {
            // get list of all odata type collections
            var xmlReader = XmlReader.Create(_odataEndpoint, new XmlReaderSettings() { CloseInput = true });
            var serviceDocument = ServiceDocument.Load(xmlReader);           
            if (serviceDocument.Workspaces.Count == 0) throw new Exception("No workspace found in service document");
            xmlReader.Close();

            var workspace = serviceDocument.Workspaces[0];
            foreach (var resourceCollectionInfo in workspace.Collections)
            {
                var queryUrl = _odataEndpoint + resourceCollectionInfo.Link;
                SyndicationFeed feed = null;
                XmlReader reader = null;
                try
                {
                    reader = XmlReader.Create(queryUrl, new XmlReaderSettings() { CloseInput = true});                    
                    feed = SyndicationFeed.Load(reader);                        
                    reader.Close();
                } catch(Exception e)
                {
                    Logging.LogError(1, "Unable to fetch odata collection data {0} {1}", queryUrl, e.Message);
                    if (reader != null) reader.Close();
                }

                if (feed != null)
                {
                    // issue a query against each collection to find all entries that have changed since the give date                    
                    foreach (var syndicationItem in feed.Items)
                    {
                        Fragment fragment = null;
                        try {
                            if (syndicationItem.LastUpdatedTime.UtcDateTime > since)
                            {
                                fragment = new Fragment()
                                                {
                                                    PublishDate = syndicationItem.LastUpdatedTime.UtcDateTime,
                                                    ResourceId = syndicationItem.Id,
                                                    ResourceName = syndicationItem.Title.Text,
                                                    ResourceUri = syndicationItem.Id
                                                };
                            }
                        } catch(Exception e)
                        {
                            Logging.LogError(1, "Unable to build fragment {0} {1}", e.Message, e.StackTrace);
                        }
                        if (fragment != null) yield return fragment;
                    }
                }
            }
            yield break;
        }

        private readonly static XNamespace DataServicesNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace MetadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private const string XmlSchemaDataTypeNamespace = "http://www.w3.org/2001/XMLSchema#";
        private const string OdataRelationshipRelTypePrefix = "http://schemas.microsoft.com/ado/2007/08/dataservices/related/";

        public override Stream GetFragment(string id, string mimeType)
        {
            try
            {
                var g = new Graph();

                // id is the url
                var odataQuery = id;
                var xmlReader = XmlReader.Create(odataQuery, new XmlReaderSettings() { CloseInput = true});
                var syndicationItem = SyndicationItem.Load(xmlReader);
                xmlReader.Close();

                if (syndicationItem != null)
                {
                    // add the title
                    g.Assert(g.CreateUriNode(new Uri(id)), g.CreateUriNode(new Uri(_rdfsNamespacePrefix + "label")), g.CreateLiteralNode(syndicationItem.Title.Text));                                     

                    // process basic properties
                    var xmlContent = syndicationItem.Content as XmlSyndicationContent;
                    if (xmlContent != null)
                    {
                        var odataEntityXml = XDocument.Load(xmlContent.GetReaderAtContent());                        

                        // get properties
                        var odataProperties =
                            odataEntityXml.Descendants().Where(elem => elem.Name.Namespace.Equals(DataServicesNamespace));

                        foreach (var odataProperty in odataProperties)
                        {
                            var propertyName = odataProperty.Name.LocalName;
                            var propertyValue = odataProperty.Value;

                            // remove later
                            propertyValue = propertyValue.Replace("&oslash;", "ø");
                            propertyValue = propertyValue.Replace("&aring;", "å");
                            propertyValue = propertyValue.Replace("&", "");

                            if (string.IsNullOrEmpty(propertyValue)) continue;                            

                            // see if there is a data type
                            if (odataProperty.Attribute(MetadataNamespace + "type") != null)
                            {
                                g.Assert(g.CreateUriNode(new Uri(id)), 
                                         g.CreateUriNode(new Uri(_defaultSchemaNamespacePrefix + propertyName)), 
                                         g.CreateLiteralNode(propertyValue, ConvertEdmToXmlSchemaDataType(odataProperty.Attribute(MetadataNamespace + "type").Value)));                                     
                            } else
                            {
                                g.Assert(g.CreateUriNode(new Uri(id)), g.CreateUriNode(new Uri(_defaultSchemaNamespacePrefix + propertyName)), g.CreateLiteralNode(propertyValue));                                
                            }
                        }
                    }                    
                }

                // add a instance-of relationship in to the graph for the entity
                foreach (var category in syndicationItem.Categories)
                {
                    var term = _defaultSchemaNamespacePrefix + category.Name.Replace('.', '/');                                        
                    g.Assert(g.CreateUriNode(new Uri(id)),
                                                g.CreateUriNode(new Uri(_rdfNamespacePrefix + "type")),
                                                g.CreateUriNode(new Uri(term)));
                }

                // process relationships
                var links = syndicationItem.Links.Where(l => l.RelationshipType.StartsWith(OdataRelationshipRelTypePrefix));
                foreach (var syndicationLink in links)
                {
                    // property name
                    var propertyName = syndicationLink.RelationshipType.Substring(OdataRelationshipRelTypePrefix.Length);

                    // go fetch the related entities
                    // todo: we might look to use expand here but as there is no wildcard its a bit of a pain right now unless we pull the schema.

                    // need to check if we need to load an entry or a feed
                    IEnumerable<SyndicationItem> items = null;
                    try
                    {
                        if (syndicationLink.MediaType.ToLower().Contains("type=entry"))
                        {
                            xmlReader = XmlReader.Create(id + "/" + propertyName, new XmlReaderSettings() { CloseInput = true});
                            items = new List<SyndicationItem>()
                                        {SyndicationItem.Load(xmlReader)};
                            xmlReader.Close();
                        }
                        else
                        {
                            xmlReader = XmlReader.Create(id + "/" + propertyName, new XmlReaderSettings() { CloseInput = true});
                            items = SyndicationFeed.Load(xmlReader).Items;
                            xmlReader.Close();
                        }
                    } catch (Exception)
                    {
                        // log and carry on
                        xmlReader.Close();
                    }

                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            // predicate value
                            g.Assert(g.CreateUriNode(new Uri(id)),
                                     g.CreateUriNode(new Uri(_defaultSchemaNamespacePrefix + propertyName)),
                                     g.CreateUriNode(new Uri(item.Id)));
                        }                                            
                    }
                }
                
                // return data
                var rdfxmlwriter = new RdfXmlWriter();
                var strw = new System.IO.StringWriter();
                rdfxmlwriter.Save(g, strw);
                var data = strw.ToString();
                data = data.Replace("utf-16", "utf-8");

                var ms = new MemoryStream();
                var sw = new StreamWriter(ms, Encoding.UTF8);
                sw.Write(data);
                sw.Flush();
                ms.Seek(0, SeekOrigin.Begin);
                return ms;

            } catch(Exception ex)
            {
                Logging.LogError(1, "Unable to fetch fragment for {0}. {1} {2}", id, ex.Message, ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// Converts from MS Edm types to Xml Schema Datatypes
        /// </summary>
        /// <param name="edmValue"></param>
        /// <returns></returns>
        private static Uri ConvertEdmToXmlSchemaDataType(string edmValue)
        {
            if (edmValue.StartsWith("Edm"))
            {
                if (edmValue.ToLower().Contains("boolean"))
                {
                    return new Uri(XmlSchemaDataTypeNamespace + "boolean");
                }
                if (edmValue.ToLower().Contains("int32"))
                {
                    return new Uri(XmlSchemaDataTypeNamespace + "int");
                }
                if (edmValue.ToLower().Contains("datetime"))
                {
                    return new Uri(XmlSchemaDataTypeNamespace + "dateTime");
                }
            }
            return new Uri(XmlSchemaDataTypeNamespace + "string");
        }

        public override Stream GetSnapshot(string id, string mimeType)
        {
            throw new NotImplementedException();
        }

        public override Stream GetSample(string mimeType)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
