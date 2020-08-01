using System;

namespace BrightstarDB
{
    /// <summary>
    /// An enumeration of the different type of model that have supported
    /// serializations in BrightstarDB.
    /// </summary>
    [Flags]
    public enum SerializableModel
    {
        /// <summary>
        /// A single RDF graph
        /// </summary>
        RdfGraph = 0x01,
        /// <summary>
        /// A collection of RDF graphs
        /// </summary>
        RdfDataset = 0x02,
        /// <summary>
        /// A SPARQL result set
        /// </summary>
        SparqlResultSet = 0x04,

        /// <summary>
        /// None of the recoginzed serialization models
        /// </summary>
        None = 0x0
    }
}