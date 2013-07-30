using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// A parser for RDF NTriples syntax
    /// </summary>
    public class NTriplesParser : IRdfParser
    {
        private Dictionary<string, string> _bnodes;

        /// <summary>
        /// The target graph for all statements read by this parser
        /// </summary>
        private string _graphUri;

        /// <summary>
        /// The sink to receive the parsed RDF statements
        /// </summary>
        private ITripleSink _sink;

        ///<summary>
        /// The constructed BNnode mappings.  
        ///</summary>
        public Dictionary<string, string> BNodes
        {
            get { return _bnodes; }
        }

        /// <summary>
        /// Parse the contents of <paramref name="data"/> as an N3 file
        /// </summary>
        /// <param name="data">The data stream to parse</param>
        /// <param name="sink">The target for the parsed RDF statements</param>
        /// <param name="defaultGraphUri">The default graph URI to assign to each of the parsed statements</param>
        public void Parse(Stream data, ITripleSink sink, string defaultGraphUri)
        {
            _sink = sink;
            _graphUri = defaultGraphUri;
            _bnodes = new Dictionary<string, string>();
            var sr = new StreamReader(data);
            int lineNumber = 1;
            string line = sr.ReadLine();
            try
            {
                while (line != null)
                {
                    ParseLine(lineNumber, line);
                    line = sr.ReadLine();
                    lineNumber++;
                }
            }
            catch (TripleSinkException tse)
            {
                throw tse.InnerException;
            }
            catch (RdfParserException)
            {
                // Rethrow parser exceptions without wrapping them.
                throw;
            }
            catch (Exception ex)
            {
                throw new RdfParserException("Error parsing NTriples stream", ex);
            }
        }

        /// <summary>
        /// Parse from a text reader
        /// </summary>
        /// <param name="reader">The reader providing the data to be parsed</param>
        /// <param name="sink">The target for the parsed RDF statements</param>
        /// <param name="defaultGraphUri">The default graph URI to assign to each of the parsed statements</param>
        public void Parse(TextReader reader, ITripleSink sink, string defaultGraphUri)
        {
            _sink = sink;
            _graphUri = defaultGraphUri;
            _bnodes = new Dictionary<string, string>();
            string line = reader.ReadLine();
            int lineNumber = 1;
            while (line != null)
            {
                try
                {
                    ParseLine(lineNumber, line);
                }
                    catch(TripleSinkException tse)
                    {
                        throw tse.InnerException;
                    }
                    catch(RdfParserException)
                    {
                        throw;
                    }
                catch (Exception ex)
                {
                    throw new RdfParserException(lineNumber, "Error parsing NTriples stream", ex);
                }
                line = reader.ReadLine();
                lineNumber++;
            }
        }

        private void ParseLine(int lineNumber, string line)
        {
            if (line == null)
            {
                // ignore null lines
                return;
            }

            line = line.Trim();
            if (String.Empty.Equals(line) || line.StartsWith("#"))
            {
                // ignore empty lines and comments
                return;
            }

            // get first space
            var firstSpace = line.IndexOf(' ');
            var subj = line.Substring(0, firstSpace).Trim();
            string subject;
            bool subjectIsBNode = false;

            if (subj.StartsWith("<"))
            {
                // uri
                subject = subj.Substring(1, subj.Length - 2);
            }
            else if (subj.StartsWith("_:"))
            {
                // blank node
                subject = subj.Substring(2, subj.Length - 2);
                subjectIsBNode = true;
            }
            else
            {
                throw new RdfParserException(lineNumber, "Invalid triple. Subject URI or blank node expected.");
            }

            line = line.Substring(firstSpace).Trim();

            // get predicate
            firstSpace = line.IndexOf(' ');
            string pred = line.Substring(0, firstSpace).Trim();
            string predicate;
            bool predicateIsBNode = false;

            if (pred.StartsWith("<"))
            {
                // uri
                predicate = pred.Substring(1, pred.Length - 2);
            }
            else if (pred.StartsWith("_:"))
            {
                predicate = pred.Substring(2);
                predicateIsBNode = true;
            }
            else
            {
                throw new RdfParserException("Invalid triple. Predicate URI or blank node expected.");
            }

            line = line.Substring(firstSpace).Trim();

            // object value
            if (line.StartsWith("<"))
            {
                int lastAngle = line.IndexOf(">");
                string objectUri = line.Substring(1, lastAngle - 1);

                line = line.Substring(lastAngle + 1);
                var graphUri = CheckContextForGraphUri(line);

                try
                {
                    _sink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, objectUri, false, false, null,
                                 null, graphUri);
                }
                catch (InvalidTripleException ex)
                {
                    throw new RdfParserException(lineNumber, ex.Message);
                }
                catch (Exception ex)
                {
                    throw new TripleSinkException(ex);
                }
            }
            else if (line.StartsWith("\""))
            {
                // get string in quotes
                int lastQuote = line.LastIndexOf("\"");

                string literalValue = line.Substring(1, lastQuote - 1);

                literalValue = UnEscapeLiteralValue(literalValue);

                // check for lang code or data type
                line = line.Substring(lastQuote + 1).Trim();

                if (line.StartsWith("@"))
                {
                    // langcode
                    int index = line.IndexOf(" ");
                    if (index < 0) index = line.IndexOf("\t");
                    string langCode = line.Substring(1, index - 1).Trim();

                    line = line.Substring(index + 1);
                    var graphUri = CheckContextForGraphUri(line);

                    try
                    {
                        _sink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, literalValue, false, true,
                                     RdfDatatypes.PlainLiteral, langCode, graphUri);
                    }
                    catch (Exception ex)
                    {
                        throw new TripleSinkException(ex);
                    }
                }
                else if (line.StartsWith("^^"))
                {
                    // data type
                    var index = line.IndexOf('>');
                    var dataType = line.Substring(3, index - 3);

                    line = line.Substring(index + 1);
                    var graphUri = CheckContextForGraphUri(line);

                    try
                    {
                        _sink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, literalValue, false, true,
                                     dataType, null, graphUri);
                    }
                    catch (Exception ex)
                    {
                        throw new TripleSinkException(ex);
                    }
                }
                else
                {
                    var graphUri = CheckContextForGraphUri(line);
                    try
                    {
                        _sink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, literalValue, false, true,
                                     RdfDatatypes.PlainLiteral, null, graphUri);
                    }
                    catch (Exception ex)
                    {
                        throw new TripleSinkException(ex);
                    }
                }
            }
            else if (line.StartsWith("_:"))
            {
                int end = line.IndexOf(" ");
                if (end < 0) end = line.IndexOf("\t");
                if (end < 0) end = line.IndexOf(".");

                string bnodeId = line.Substring(2, end - 2).Trim();

                line = line.Substring(end);
                var graphUri = CheckContextForGraphUri(line);

                try
                {
                    // create triple
                    _sink.Triple(subject, subjectIsBNode, predicate, predicateIsBNode, bnodeId, true, false, null, null,
                                 graphUri);
                }
                catch (Exception ex)
                {
                    throw new TripleSinkException(ex);
                }
            }
            else
            {
                throw new RdfParserException(lineNumber,
                                             "Invalid triple. Expected object URI, blank node or literal value.");
            }
        }

        /// <summary>
        ///  Checks if the remainder of the line contains a graph uri or returns the default
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string CheckContextForGraphUri(string line)
        {
            if (line.Trim().StartsWith("<"))
            {
                var startPos = line.IndexOf("<");
                var length = line.IndexOf(">") - startPos;
                return line.Substring(startPos + 1, length - 1);
            }

            return _graphUri;
        }

        private string UnEscapeLiteralValue(string value)
        {
            // special escapes
            string res = value.Replace("\\\\", "\\");
            res = res.Replace("\\\"", "\"");
            res = res.Replace("\\\'", "\'");
            res = res.Replace("\\n", "\n");
            res = res.Replace("\\t", "\t");
            res = res.Replace("\\r", "\r");

            // unicode processing
            int loc = res.IndexOf("\\u");
            while (loc >= 0)
            {
                string unicodeValues = res.Substring(loc + 2, 4);
                // check all are hex digits
                foreach (char c in unicodeValues)
                {
                    if (!IsHexDigit(c))
                    {
                        throw new FormatException("Unexpected non hex digit in unicode escaped string: " + c);
                    }
                }
                char replacementChar = ConvertToUtf16Char(unicodeValues);
                res = res.Replace("\\u" + unicodeValues, new string(replacementChar, 1));
                loc = res.IndexOf("\\u");
            }

            loc = res.IndexOf("\\U");
            while(loc >= 0)
            {
                string unicodeValues = value.Substring(loc + 2, 8);
#if PORTABLE
                if (!unicodeValues.ToCharArray().All(IsHexDigit))
                {
                    throw new FormatException("Unexpected non-hex digit in unicode escaped string: \\U" + unicodeValues);
                }
#else
                if (!unicodeValues.All(IsHexDigit))
                {
                    throw new FormatException("Unexpected non-hex digit in unicode escaped string: \\U" + unicodeValues);
                }
#endif
                string replacementChar = ConvertToUtf32Char(unicodeValues);
                res = res.Replace("\\U" + unicodeValues, replacementChar);
                loc = res.IndexOf("\\U");
            }

            return res;
        }

        private static bool IsHexDigit(char c)
        {
            if (Char.IsDigit(c))
            {
                return true;
            }
            switch (c)
            {
                case 'A':
                case 'a':
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'D':
                case 'd':
                case 'E':
                case 'f':
                case 'F':
                    return true;
                default:
                    return false;
            }
        }

        private static char ConvertToUtf16Char(String hex)
        {
            try
            {
                ushort i = Convert.ToUInt16(hex, 16);
                return Convert.ToChar(i);
            }
            catch
            {
                throw new FormatException(String.Format("Unable to convert hex value {0} to a UTF-16 character", hex));
            }
        }

        /// <summary>
        /// Converts a Hex Escape into the relevant Unicode Character
        /// </summary>
        /// <param name="hex">Hex code</param>
        /// <returns></returns>
        private static string ConvertToUtf32Char(String hex)
        {
#if SILVERLIGHT
            throw new FormatException("The Silverlight library does not support UTF-32 encoded characters.");
#elif PORTABLE
            throw new FormatException("The Portable Class Library library does not support UTF-32 encoded characters.");
#else
            try
            {
                //Convert to an Integer
                int i = Convert.ToInt32(hex, 16);
                return Char.ConvertFromUtf32(i);
            }
            catch
            {
                throw new FormatException("Unable to convert the String '" + hex + "' into a Unicode Character");
            }
#endif
        }
    }
}