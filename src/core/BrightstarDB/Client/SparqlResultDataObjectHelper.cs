using System;
using System.Collections.Generic;
using System.Xml;
using BrightstarDB.Model;
using BrightstarDB.Rdf;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Given a sparql result stream will lazily return DataObjects via the specified context.
    /// All objects returned will be stub's and not have any data loaded.
    /// </summary>
    internal class SparqlResultDataObjectHelper
    {
        private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private readonly IDataObjectStore _storeContext;
        internal SparqlResultDataObjectHelper(IDataObjectStore storeContext)
        {
            _storeContext = storeContext;
        }

        public IEnumerable<IDataObject> BindDataObjects(SparqlResult sparqlResult)
        {
            var xmlReader = sparqlResult.ResultDocument.CreateReader();
            xmlReader.MoveToContent();
            if (xmlReader.IsStartElement())
            {
                if ("RDF".Equals(xmlReader.LocalName) && RdfNamespace.Equals(xmlReader.NamespaceURI))
                {
                    return BindRdfDataObjects(xmlReader);
                }
            }
            return BindDataObjects(xmlReader);
        }

        public IEnumerable<IDataObject> BindRdfDataObjects(XmlReader xmlReader)
        {
            SkipToStartElement(xmlReader);
            while(!xmlReader.EOF)
            {
                if (xmlReader.NodeType != XmlNodeType.Element) SkipToStartElement(xmlReader);
                if (!xmlReader.EOF)
                {
                    var resource = xmlReader.GetAttribute("about", RdfNamespace);
                    if (resource != null)
                    {
                        var dataobject = BindRdfDataObject(resource, xmlReader.NamespaceURI + xmlReader.LocalName,
                                                           xmlReader);
                        if (dataobject != null) yield return dataobject;
                    }
                    else
                    {
                        xmlReader.ReadInnerXml();
                    }
                }
            }            
        } 

        private IDataObject BindRdfDataObject(string resourceAddress, string resourceType, XmlReader reader)
        {
            var resourceElementLevel = reader.Depth;
            if (SkipToStartElement(reader))
            {
                var dataObject = _storeContext.MakeDataObject(resourceAddress) as DataObject;
                var triples = new List<Triple>();
                if (!String.IsNullOrEmpty(resourceType))
                {
                    triples.Add(new Triple{Subject = resourceAddress, Predicate = DataObject.TypeDataObject.Identity, Object = resourceType});
                }
                while(!reader.EOF)
                {
                    if (reader.Depth == resourceElementLevel)
                    {
                        break;
                    }
                    if (reader.IsStartElement())
                    {
                        var pred = reader.NamespaceURI + reader.LocalName;
                        var obj = reader.GetAttribute("resource", RdfNamespace);
                        if (obj != null)
                        {
                            triples.Add(new Triple {Subject = resourceAddress, Predicate = pred, Object = obj});
                            reader.Read();
                        }
                        else if (!reader.IsEmptyElement)
                        {
                            var dt = reader.GetAttribute("datatype", RdfNamespace);
                            var lang = reader.GetAttribute("xml:lang");
                            obj = reader.ReadElementContentAsString();
                            triples.Add(new Triple
                                            {
                                                Subject = resourceAddress,
                                                Predicate = pred,
                                                IsLiteral = true,
                                                Object = obj,
                                                LangCode = lang,
                                                DataType = dt ?? RdfDatatypes.String
                                            });
                        }
                    }
                    else
                    {
                        reader.Read();
                    }
                } 
                dataObject.BindTriples(triples);
                return dataObject;
            }
            return null;
        }

        private bool SkipToStartElement(XmlReader reader)
        {
            while (!reader.EOF)
            {
                reader.Read();
                if (reader.IsStartElement()) return true;
            }
            return false;
        }

        public IEnumerable<IDataObject> BindDataObjects(XmlReader xmlReader)
        {
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element 
                    && xmlReader.Name.ToLower().Equals("uri") 
                    && xmlReader.NamespaceURI.Equals("http://www.w3.org/2005/sparql-results#"))
                {
                    string uri = null;
                    try
                    {
                        uri = xmlReader.ReadElementContentAsString();
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(BrightstarEventId.ClientDataBindError, "Error binding to SPARQL results element. {0}", ex);
                    }
                    if (!String.IsNullOrEmpty(uri))
                    {
                        yield return _storeContext.MakeDataObject(uri);
                    }
                }
            }
            xmlReader.Close();
        }

    }
}
