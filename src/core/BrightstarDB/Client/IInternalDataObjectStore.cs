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
        /// The current transaction delete patterns
        /// </summary>
        List<Triple> DeletePatterns { get; }

        /// <summary>
        /// The current transaction triples to add
        /// </summary>
        List<Triple> AddTriples { get; }

        /// <summary>
        /// Returns an enumeration of all data objects that are the subject
        /// of a triple that binds a predicate of type <paramref name="pred"/>
        /// and the object <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The object resource</param>
        /// <param name="pred">The predicate resource</param>
        /// <returns>An enumeration of all matching subject resources</returns>
        IEnumerable<IDataObject> GetInverseOf(IDataObject obj, IDataObject pred);
    }
}
