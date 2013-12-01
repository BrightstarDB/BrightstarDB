using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Writing.Formatting;

namespace BrightstarDB
{
    /// <summary>
    /// An enumeration of the RDF formats supported by BrightstarDB 
    /// </summary>
    public class RdfFormat : FormatInfo
    {
        internal Type TripleFormatterType { get; set; }

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
        /// <returns>A <see cref="RdfFormat"/> instance</returns>
        public RdfFormat WithEncoding(Encoding encoding)
        {
            return new RdfFormat(this, encoding);
        }

        private RdfFormat(RdfFormat copyOf, Encoding withEncoding)
        {
            DefaultExtension = copyOf.DefaultExtension;
            TripleFormatterType = copyOf.TripleFormatterType;
            Encoding = withEncoding;
            MediaTypes = copyOf.MediaTypes;
            SerializedModel = copyOf.SerializedModel;
        }

        private RdfFormat(string defaultExtension, Type formatterType, SerializableModel models, params string[] mediaTypes)
        {
            DefaultExtension = defaultExtension;
            TripleFormatterType = formatterType;
            Encoding = Encoding.UTF8;
            MediaTypes = new List<string>(mediaTypes);
            SerializedModel = models;
        }

        /// <summary>
        /// Returns true if this format can be used to serialize RDF datasets consisting of multiple graphs
        /// </summary>
        public bool SupportsDatasets { get { return (SerializedModel & SerializableModel.RdfDataset) == SerializableModel.RdfDataset; } }

        /// <summary>
        /// RDF/XML format
        /// </summary>
        public static RdfFormat RdfXml = new RdfFormat("rdf", typeof(RdfXmlFormatter), SerializableModel.RdfGraph, "application/rdf+xml", "application/xml");

        /// <summary>
        /// NTriples Format
        /// </summary>
        public static RdfFormat NTriples = new RdfFormat("nt", typeof(NTriplesFormatter), SerializableModel.RdfGraph, "text/ntriples", "text/ntriples+turtle", "application/rdf-triples", "application/x-ntriples");

        /// <summary>
        /// Turtle Format
        /// </summary>
        public static RdfFormat Turtle = new RdfFormat("ttl", typeof(TurtleW3CFormatter), SerializableModel.RdfGraph, "application/x-turtle", "application/turtle");

        /// <summary>
        /// Notation 3 Format
        /// </summary>
        public static RdfFormat Notation3 = new RdfFormat("n3", typeof(Notation3Formatter), SerializableModel.RdfGraph, "text/rdf+n3");

        /// <summary>
        /// NQuads Format
        /// </summary>
        public static RdfFormat NQuads = new RdfFormat("nq", typeof(NQuadsFormatter), SerializableModel.RdfDataset, "text/x-nquads");

        /// <summary>
        /// TriG format
        /// </summary>
        public static RdfFormat TriG = new RdfFormat("trig", null, SerializableModel.RdfDataset, "application/x-trig");

        /// <summary>
        /// TriX format
        /// </summary>
        public static RdfFormat TriX = new RdfFormat("xml", null, SerializableModel.RdfDataset, "application/trix");

        /// <summary>
        /// RDF/JSON format
        /// </summary>
        public static RdfFormat Json = new RdfFormat("rj", null, SerializableModel.RdfGraph, "text/json", "application/rdf+json");

        /// <summary>
        /// All RDF serialization formats supported by BrightstarDB
        /// </summary>
        public static RdfFormat[] AllFormats =
            {
                RdfXml, NTriples, Turtle, Notation3, NQuads, TriG, TriX, Json
            };

        /// <summary>
        /// Returns the format to use for the specified media type or extension, or null if there is no match found
        /// </summary>
        /// <param name="resultsMediaTypeOrExtension"></param>
        /// <returns></returns>
        public static RdfFormat GetResultsFormat(string resultsMediaTypeOrExtension)
        {
            var parts = resultsMediaTypeOrExtension.Split(';').Select(p => p.Trim()).ToList();
#if PORTABLE
            var encodingName =
                parts.Where(p => p.ToLowerInvariant().StartsWith("charset="))
                     .Select(p => p.Substring(8))
                     .FirstOrDefault();
#else
            var encodingName =
                parts.Where(p => p.StartsWith("charset=", StringComparison.InvariantCultureIgnoreCase)).Select(
                    p => p.Substring(8)).FirstOrDefault();
#endif
            var encoding = encodingName == null ? Encoding.UTF8 : Encoding.GetEncoding(encodingName);
            var mediaType = parts[0];
            foreach (var format in AllFormats)
            {
                if (format.MediaTypes.Contains(mediaType)) return format.WithEncoding(encoding);
                if (format.DefaultExtension.Equals(mediaType)) return format.WithEncoding(encoding);
            }
            return null;
        }
    }
}
