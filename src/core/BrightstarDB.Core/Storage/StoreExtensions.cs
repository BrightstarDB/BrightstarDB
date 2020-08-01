using System;
using System.Collections.Generic;
using System.IO;
using BrightstarDB.Model;
using BrightstarDB.Rdf;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Provides extension methods for classes that implement the IStore interfce
    /// </summary>
    internal static class StoreExtensions
    {
        /// <summary>
        /// Imports triples from the stream provided
        /// </summary>
        /// <param name="store">The store to import triples into</param>
        /// <param name="jobId">The GUID identifier for the import job</param>
        /// <param name="triples">The stream to read the triples from</param>
        /// <param name="graphUri">The URI of the graph to import the triples into, or null to import into the default graph</param>
        public static void Import(this IStore store, Guid jobId, Stream triples, Uri graphUri = null)
        {
            var tripleParser = new NTriplesParser();
            tripleParser.Parse(triples, new StoreTripleSink(store, jobId, 500000),
                               graphUri == null ? Constants.DefaultGraphUri : graphUri.ToString());
        }

        /// <summary>
        /// Exports all triples to the stream provided
        /// </summary>
        /// <param name="store">The store to export triples from </param>
        /// <param name="output">The stream to write the triples to</param>
        /// <param name="graphs">OPTIONAL: The URIs of the graph(s) to be exported. Pass NULL to export all graphs</param>
        public static void Export(this IStore store, Stream output, IEnumerable<string> graphs = null)
        {
            IEnumerable<Triple> triples = store.Match(null, null, null, graphs: graphs);
            using (var sw = new StreamWriter(output))
            {
                var ntripleWriter = new BrightstarTripleSinkAdapter(new NTriplesWriter(sw));
                foreach (Triple triple in triples)
                {
                    ntripleWriter.Triple(triple);
                }
                sw.Flush();
            }
        }
    }
}
