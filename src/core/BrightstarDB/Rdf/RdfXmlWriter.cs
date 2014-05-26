using System;
using System.Xml;

namespace BrightstarDB.Rdf
{
    internal class RdfXmlWriter : ITripleSink, IDisposable
    {
        private readonly XmlWriter _writer;
        private const string RdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        

        public RdfXmlWriter(XmlWriter writer)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            _writer = writer;
            _writer.WriteStartDocument();
            _writer.WriteStartElement("rdf", "RDF", RdfNamespace);
        }

        public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode,
                           bool objIsLiteral, string dataType, string langCode, string graphUri)
        {
            _writer.WriteStartElement("Description", RdfNamespace);
            _writer.WriteAttributeString(subjectIsBNode ? "nodeID" : "about", RdfNamespace, subject);
            var predicateTuple = SplitUri(predicate);
            if (objIsLiteral)
            {
                if (String.IsNullOrEmpty(dataType))
                {
                    // Can write literal as an attribute value
                    _writer.WriteAttributeString(predicateTuple.Item2, predicateTuple.Item1, obj);
                    WriteXmlLang(langCode);
                }
                else
                {
                    // Have to write literal as an element
                    _writer.WriteStartElement(predicateTuple.Item2, predicateTuple.Item1);
                    WriteXmlLang(langCode);
                    _writer.WriteAttributeString("datatype", RdfNamespace, dataType);
                    _writer.WriteString(obj);
                }
            }
            else
            {
                _writer.WriteStartElement(predicateTuple.Item2, predicateTuple.Item1);
                _writer.WriteAttributeString(objIsBNode ? "nodeID" : "resource", RdfNamespace, obj);
            }

            _writer.WriteEndElement(); // rdf:Description
        }

        private void WriteXmlLang(string langCode)
        {
            if (!String.IsNullOrEmpty(langCode))
            {
                _writer.WriteAttributeString("xml","lang", null, langCode);
            }
        }

        public void Close()
        {
            if (_writer.WriteState == WriteState.Closed) return;
            while (_writer.WriteState == WriteState.Element)
            {
                _writer.WriteEndElement();
            }
            _writer.WriteEndDocument();
        }

        private Tuple<String, String> SplitUri(string uriString)
        {
            var ix = uriString.IndexOf('#');
            if (ix < 0) ix = uriString.LastIndexOf('/');
            return new Tuple<string, string>(uriString.Substring(0, ix+1), uriString.Substring(ix+1));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RdfXmlWriter()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }
    }
}
