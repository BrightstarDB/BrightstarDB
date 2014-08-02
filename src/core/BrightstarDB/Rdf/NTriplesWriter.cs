using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// A triple sink that writes the received triples out in NTriples format
    /// </summary>
    /// <remarks>Due to the limitations of the NTriples syntax, the Graph URI portion of the RDF statments will not be represented in the output file.</remarks>
    public class NTriplesWriter : ITripleSink
    {
        private readonly TextWriter _writer;
        private readonly Dictionary<string, string> _bnodeUriMap;

        /// <summary>
        /// Creates a new NTriples writer
        /// </summary>
        /// <param name="writer">The text writer that the NTriples representation will be written to</param>
        public NTriplesWriter(TextWriter writer)
        {
            _writer = writer;
            _bnodeUriMap = new Dictionary<string, string>();
        }

        /// <summary>
        /// Receives a BNode mapping from the generator
        /// </summary>
        /// <param name="bnodeId"></param>
        /// <param name="bnodeUri"></param>
        public void BNode(string bnodeId, string bnodeUri)
        {
            _bnodeUriMap[bnodeUri] = bnodeId;
        }

        private void AppendResource(StringBuilder line, string identifier, bool isBNode)
        {
            if (isBNode){
                line.Append("_:");
                line.Append(identifier);
            }
            else
            {
                line.Append("<");
                line.Append(Uri.EscapeUriString(identifier));
                line.Append(">");
            }
        }

        private static void AppendEscapedLiteral(StringBuilder line, IEnumerable<char> unescapedLiteral, string dataType, string languageCode)
        {
            line.Append("\"");
            //char highSurrogate = '\ud800';

            foreach (var c in unescapedLiteral)
            {
                if (c == 0x20 || c == 0x21 || c >= 0x23 && c <= 0x5B || c >= 0x5D && c <= 0x7E)
                {
                    line.Append(c);
                }
                else switch (c)
                {
                    case (char) 0x09:
                        line.Append("\\t");
                        break;
                    case (char) 0x0A:
                        line.Append("\\n");
                        break;
                    case (char) 0x0D:
                        line.Append("\\r");
                        break;
                    case (char) 0x22:
                        line.Append("\\\"");
                        break;
                    case (char) 0x5C:
                        line.Append("\\\\");
                        break;
                    default:
#if SILVERLIGHT || PORTABLE
                        if (c <= 0x8 || c == 0xB || c == 0xC || (c >= 0x0E && c <= 0x1F) ||
                                 (c > 0x7F && c <= 0xFFFF))
                        {
                            line.Append("\\u");
                            line.Append(Convert.ToInt16(c).ToString("X4"));
                        }
                        else
                        {
                            throw new FormatException("Silverlight does not support UTF-32 characters.");
                        }
#else
                        if (char.IsHighSurrogate(c))
                        {
                            highSurrogate = c;
                        }
                        else if (char.IsLowSurrogate(c))
                        {
                            line.Append("\\U");
                            line.Append(Char.ConvertToUtf32(highSurrogate, c).ToString("X8"));
                        }
                        else if (c <= 0x8 || c == 0xB || c == 0xC || (c >= 0x0E && c <= 0x1F) ||
                                 (c > 0x7F && c <= 0xFFFF))
                        {
                            line.Append("\\u");
                            line.Append(((int) c).ToString("X4"));
                        }
#endif
                        break;
                }
            }
            line.Append("\"");
            if (!String.IsNullOrEmpty(languageCode))
            {
                line.Append("@");
                line.Append(languageCode.ToLower());
            }
            else if (!String.IsNullOrEmpty(dataType) && !RdfDatatypes.PlainLiteral.Equals(dataType))
            {
                line.Append("^^");
                line.Append("<");
                line.Append(dataType);
                line.Append(">");
            }
        }

        #region Implementation of ITripleSink

        /// <summary>
        /// Handler method for an individual RDF statement
        /// </summary>
        /// <param name="subject">The statement subject resource URI</param>
        /// <param name="subjectIsBNode">True if the value of <paramref name="subject"/> is a BNode identifier</param>
        /// <param name="predicate">The predicate resource URI</param>
        /// <param name="predicateIsBNode">True if the value of <paramref name="predicate"/> is a BNode identifier.</param>
        /// <param name="obj">The object of the statement</param>
        /// <param name="objIsBNode">True if the value of <paramref name="obj"/> is a BNode identifier.</param>
        /// <param name="objIsLiteral">True if the value of <paramref name="obj"/> is a literal string</param>
        /// <param name="dataType">The datatype URI for the object literal or null if the object is not a literal</param>
        /// <param name="langCode">The language code for the object literal or null if the object is not a literal</param>
        /// <param name="graphUri">The graph URI for the statement</param>
        public void Triple(string subject, bool subjectIsBNode, string predicate, bool predicateIsBNode, string obj, bool objIsBNode, bool objIsLiteral, string dataType, string langCode, string graphUri)
        {
            var line = new StringBuilder();

            AppendResource(line, subject, subjectIsBNode);
            line.Append(" ");
            AppendResource(line, predicate, predicateIsBNode);
            line.Append(" ");
            if (objIsLiteral)
            {
#if PORTABLE
                AppendEscapedLiteral(line, obj.ToCharArray(), dataType, langCode);
#else
                AppendEscapedLiteral(line, obj, dataType, langCode);
#endif
            }
            else
            {
                AppendResource(line, obj, objIsBNode);
            }
            line.Append(" .");
            _writer.WriteLine(line.ToString());
        }

        /// <summary>
        /// Method invoked to indicate that no more triples remain to be written to the sink.
        /// </summary>
        public void Close()
        {
            // No op
        }

        #endregion
    }
}
