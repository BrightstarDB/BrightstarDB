using System.Collections.Generic;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    internal interface IInternalDataObjectStore : IDataObjectStore
    {
        /// <summary>
        /// Loads the set of triples for the specified data object from the store
        /// </summary>
        /// <param name="dataObject">The entity to be bound</param>
        /// <returns>True if one or more triples were returned by the store, false otherwise.</returns>
        bool BindDataObject(DataObject dataObject);

        /// <summary>
        /// The URI identifier of the graph to be updated
        /// </summary>
        string UpdateGraphUri { get; }

        /// <summary>
        /// The URI identifiers of the graphs that contribute properties
        /// </summary>
        string[] DataSetGraphUris { get; } 

        /// <summary>
        /// The URI identifier of the graph that stores data object version numbers
        /// </summary>
        string VersionGraphUri { get; }

        /// <summary>
        /// The current transaction delete patterns
        /// </summary>
        ITripleCollection DeletePatterns { get; }

        /// <summary>
        /// The current transaction triples to add
        /// </summary>
        ITripleCollection AddTriples { get; }

        /// <summary>
        /// Returns an enumeration of all data objects that are the subject
        /// of a triple that binds a predicate of type <paramref name="pred"/>
        /// and the object <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object resource</param>
        /// <param name="pred">The predicate resource</param>
        /// <returns>An enumeration of all matching subject resources</returns>
        IEnumerable<IDataObject> GetInverseOf(IDataObject obj, IDataObject pred);


        /// <summary>
        /// Returns an enumeration of all triples that have the specified
        /// data object as the object of the triple.
        /// </summary>
        /// <param name="obj">The object resource</param>
        /// <returns>An enumeration of all triples that have <paramref name="obj"/> as the object of the triple.</returns>
        IEnumerable<Triple> GetReferencingTriples(IDataObject obj);

        /// <summary>
        /// Returns an enumeration of all triples that have the specified
        /// object URI.
        /// </summary>
        /// <param name="objUri">The object resource URI</param>
        /// <returns>An enumeration of all triples that have <paramref name="objUri"/> as the object of the triple.</returns>
        IEnumerable<Triple> GetReferencingTriples(string objUri); 

        /// <summary>
        /// Adds preconditions to validate that there is no existing resource with the URI
        /// <paramref name="identity"/> that is an instance of one or more of the specified types.
        /// </summary>
        /// <param name="identity">The identity of the resource to be validated</param>
        /// <param name="types">An enumeration of class resources URIs</param>
        void SetClassUniqueConstraints(string identity, IEnumerable<string> types);

        /// <summary>
        /// Replace all references to one resource identifier with references to a new resource identifier
        /// </summary>
        /// <param name="oldIdentity">The old resource identifier to be replaced</param>
        /// <param name="newIdentity">The new resource identifier to be used</param>
        void ReplaceIdentity(string oldIdentity, string newIdentity);
    }
}
