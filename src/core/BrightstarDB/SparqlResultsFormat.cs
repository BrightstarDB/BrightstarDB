using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB
{
    /// <summary>
    /// An enumeration of the forms of SPARQL result supported by BrightstarDB
    /// </summary>
    public class SparqlResultsFormat
    {
        /// <summary>
        /// Get the list of media types that are recognized for this results format
        /// </summary>
        public List<String> MediaTypes { get; private set; }

        /// <summary>
        /// The default file extension to use for this results format
        /// </summary>
        public string DefaultExtension { get; private set; }

        /// <summary>
        /// The encoding to be used when streaming the result
        /// </summary>
        public Encoding Encoding { get; private set; }

        /// <summary>
        /// Gets an enumeration of all of the media types supported by the SPARQL results
        /// formats defined by this class.
        /// </summary>
        public static IEnumerable<string> AllMediaTypes
        {
            get { return AllFormats.SelectMany(f => f.MediaTypes); }
        }

        /// <summary>
        /// Returns a copy of this results format, with a different encoding
        /// </summary>
        /// <param name="encoding">The encoding to use</param>
        /// <returns>A SparqlResultsFormat instance</returns>
        public SparqlResultsFormat WithEncoding(Encoding encoding)
        {
            return new SparqlResultsFormat(DefaultExtension, encoding, MediaTypes);
        }

        private SparqlResultsFormat(string defaultExtension, Encoding encoding, IEnumerable<string> mediaTypes)
        {
            DefaultExtension = defaultExtension;
            Encoding = encoding;
            MediaTypes = new List<String>(mediaTypes);
        }

        private SparqlResultsFormat(string defaultExtension, Encoding encoding, params string[] mediaTypes)
            :this(defaultExtension,encoding, (IEnumerable<string>)mediaTypes)
        {
        }

        private SparqlResultsFormat(string defaultExtension, params string[] mediaTypes) : 
            this(defaultExtension, Encoding.UTF8, mediaTypes)
        {
        }

        /// <summary>
        /// SPARQL Query Results XML Format
        /// </summary>
        public static SparqlResultsFormat Xml = new SparqlResultsFormat("srx",
                                                                        "application/sparql-results+xml",
                                                                        "application/xml");

        /// <summary>
        /// SPARQL 1.1 Query Results JSON Format
        /// </summary>
        public static SparqlResultsFormat Json = new SparqlResultsFormat("jrx",
                                                                         "application/sparql-results+json",
                                                                         "application/json");

        /// <summary>
        /// SPARQL 1.1 Query Results TSV Format
        /// </summary>
        public static SparqlResultsFormat Tsv = new SparqlResultsFormat("tsv", "text/tab-separated-values");

        /// <summary>
        /// SPARQL 1.1 Query Results CSV format
        /// </summary>
        public static SparqlResultsFormat Csv = new SparqlResultsFormat("csv", "text/csv");

        private static readonly SparqlResultsFormat[] AllFormats = new[]
                                                           {
                                                               Xml, Json, Tsv, Csv
                                                           };

        /// <summary>
        /// Returns the format to use for the specified media type or extension, or null if there is no match found
        /// </summary>
        /// <param name="resultsMediaTypeOrExtension"></param>
        /// <returns></returns>
        public static SparqlResultsFormat GetResultsFormat(string resultsMediaTypeOrExtension)
        {
            var parts = resultsMediaTypeOrExtension.Split(';').Select(p => p.Trim()).ToList();
            var encodingName =
                parts.Where(p => p.StartsWith("charset=", StringComparison.InvariantCultureIgnoreCase)).Select(
                    p => p.Substring(8)).FirstOrDefault();
            var encoding = encodingName == null ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            var mediaType = parts[0];
            foreach(var format in AllFormats)
            {
                if (format.MediaTypes.Contains(mediaType)) return format.WithEncoding(encoding);
                if (format.DefaultExtension.Equals(mediaType)) return format.WithEncoding(encoding);
            }
            return null;
        }

        /// <summary>
        /// Returns the string representation of this object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
#if WINDOWS_PHONE
            return MediaTypes[0] + "; charset=" + Encoding.WebName;
#else
            return MediaTypes[0] + "; charset=" + Encoding.HeaderName;
#endif
        }
    }
}
