using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrightstarDB
{
    /// <summary>
    /// An enumeration of the forms of SPARQL result supported by BrightstarDB
    /// </summary>
    public class SparqlResultsFormat : FormatInfo
    {
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
            return new SparqlResultsFormat(DisplayName, DefaultExtension, encoding, MediaTypes);
        }

        private SparqlResultsFormat(string displayName, string defaultExtension, Encoding encoding, IEnumerable<string> mediaTypes)
        {
            DisplayName = displayName;
            DefaultExtension = defaultExtension;
            Encoding = encoding;
            MediaTypes = new List<String>(mediaTypes);
        }

        private SparqlResultsFormat(string displayName, string defaultExtension, Encoding encoding, params string[] mediaTypes)
            :this(displayName, defaultExtension,encoding, (IEnumerable<string>)mediaTypes)
        {
        }

        private SparqlResultsFormat(string displayName, string defaultExtension, params string[] mediaTypes) : 
            this(displayName, defaultExtension, Encoding.UTF8, mediaTypes)
        {
        }

        /// <summary>
        /// SPARQL Query Results XML Format
        /// </summary>
        public static SparqlResultsFormat Xml = new SparqlResultsFormat("XML", "srx",
                                                                        "application/sparql-results+xml",
                                                                        "application/xml");

        /// <summary>
        /// SPARQL 1.1 Query Results JSON Format
        /// </summary>
        public static SparqlResultsFormat Json = new SparqlResultsFormat("JSON", "jrx",
                                                                         "application/sparql-results+json",
                                                                         "application/json");

        /// <summary>
        /// SPARQL 1.1 Query Results TSV Format
        /// </summary>
        public static SparqlResultsFormat Tsv = new SparqlResultsFormat("Tab-separated Values", "tsv", "text/tab-separated-values");

        /// <summary>
        /// SPARQL 1.1 Query Results CSV format
        /// </summary>
        public static SparqlResultsFormat Csv = new SparqlResultsFormat("Comma-separated Values", "csv", "text/csv");

        /// <summary>
        /// Returns an array of all the pre-defined <see cref="SparqlResultsFormat"/> instances.
        /// </summary>
        public static readonly SparqlResultsFormat[] AllFormats = new[]
                                                           {
                                                               Xml, Json, Tsv, Csv
                                                           };

        /// <summary>
        /// Returns the format to use for the specified media type or extension, or null if there is no match found
        /// </summary>
        /// <param name="resultsMediaTypeOrExtension"></param>
        /// <returns>A <see cref="SparqlResultsFormat"/> instance representing the format type and encoding specified in the string,
        /// or NULL if no match is found</returns>
        public static SparqlResultsFormat GetResultsFormat(string resultsMediaTypeOrExtension)
        {
            var parts = resultsMediaTypeOrExtension.Split(';').Select(p => p.Trim()).ToList();
#if PORTABLE
            var encodingName =
                parts.Where(p => p.ToLowerInvariant().StartsWith("charset="))
                     .Select(p => p.Substring(8))
                     .FirstOrDefault();
#else
#if NETSTANDARD16
            var encodingName =
                parts.Where(p => p.ToLowerInvariant().StartsWith("charset=", StringComparison.OrdinalIgnoreCase)).Select(
                    p => p.Substring(8)).FirstOrDefault();
#else
            var encodingName =
                parts.Where(p => p.StartsWith("charset=", StringComparison.InvariantCultureIgnoreCase)).Select(
                    p => p.Substring(8)).FirstOrDefault();
#endif
#endif
            var encoding = encodingName == null ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            var mediaType = parts[0];
            foreach(var format in AllFormats)
            {
                if (format.MediaTypes.Contains(mediaType)) return format.WithEncoding(encoding);
                if (format.DefaultExtension.Equals(mediaType)) return format.WithEncoding(encoding);
            }
            return null;
        }
    }
}
