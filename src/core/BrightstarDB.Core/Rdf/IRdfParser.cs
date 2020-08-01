using System.IO;

namespace BrightstarDB.Rdf
{
    /// <summary>
    /// Generic interface for BrightstarDB RDF parsing
    /// </summary>
    internal interface IRdfParser
    {
        /// <summary>
        /// Parse the contents of <paramref name="data"/> as an RDF data stream
        /// </summary>
        /// <param name="data">The data stream to parse</param>
        /// <param name="sink">The target for the parsed RDF statements</param>
        /// <param name="defaultGraphUri">The default graph URI to assign to each of the parsed statements</param>
        void Parse(Stream data, ITripleSink sink, string defaultGraphUri);

        /// <summary>
        /// Parse from a text reader
        /// </summary>
        /// <param name="reader">The reader providing the data to be parsed</param>
        /// <param name="sink">The target for the parsed RDF statements</param>
        /// <param name="defaultGraphUri">The default graph URI to assign to each of the parsed statements</param>
        void Parse(TextReader reader, ITripleSink sink, string defaultGraphUri);
    }
}
