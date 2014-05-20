using System;
using System.Collections.Generic;
using BrightstarDB.EntityFramework.Query;
using BrightstarDB.Model;

namespace BrightstarDB.Client
{
    /// <summary>
    /// The interface for accessing and updating data in a Brightstar store using the 
    /// data object abstraction.
    /// </summary>
    public interface IDataObjectStore : IDisposable
    {
        /// <summary>
        /// Occurs when changes are being saved to the Brightstar store
        /// </summary>
        /// <remarks>The <see cref="SavingChanges"/> event is raised before changes are saved to the Brightstar store as a result of calling the <see cref="SaveChanges"/> method.</remarks>
        EventHandler SavingChanges { get; set;  }

        /// <summary>
        /// Creates a new data object with generated identity.
        /// </summary>
        /// <param name="prefix">Optional prefix for the data object.</param>
        /// <returns>A new IDataObject instance</returns>
        IDataObject MakeNewDataObject(string prefix = null);

        /// <summary>
        /// Creates a new data object with a unique generated URI as it's identity.
        /// </summary>
        /// <returns>A new IDataObject instance</returns>
        IDataObject MakeDataObject();

        /// <summary>
        /// Creates a local data object with the specified URI as it's identity. No state is loaded from the database until 
        /// the object set or get methods are called.
        /// </summary>
        /// <param name="identity">The URI identity for the node.</param>
        /// <returns>An IDataObject instance</returns>
        IDataObject MakeDataObject(string identity);

        /// <summary>
        /// Creates a local data object and fetches all state for it from the database.
        /// </summary>
        /// <param name="identity">The URI identity of the data object.</param>
        /// <returns>An IDataObject instance</returns>
        IDataObject GetDataObject(string identity);

        /// <summary>
        /// Creates a new IListDataObject where the members of the list are defined by the listItems parameter
        /// </summary>
        /// <param name="listItems">The set of items in the list</param>
        /// <returns>A new dataobject that is the head of the list.</returns>
        IDataObject MakeListDataObject(IEnumerable<object> listItems);

        /// <summary>
        /// Executes a SPARQL query and binds URI results to <see cref="IDataObject"/> instances.
        /// </summary>
        /// <param name="sparqlExpression">The SPARQL query to execute</param>
        /// <returns>An enumeration over the bound <see cref="IDataObject"/> instances</returns>
        /// <remarks>The SPARQL query should be written to return a variable binding. Results which do not
        /// bind to a URI are ignored.</remarks>
        IEnumerable<IDataObject> BindDataObjectsWithSparql(string sparqlExpression);

        /// <summary>
        /// Executes a SPARQL query against the underlying Brightstar store.
        /// </summary>
        /// <param name="sparqlQuery">The SPARQL query to execute</param>
        /// <returns>The query result object</returns>
        SparqlResult ExecuteSparql(string sparqlQuery);

        /// <summary>
        /// Executes a SPARQL query against the underlying Brightstar store.
        /// </summary>
        /// <param name="sparqlQueryContext">The SPARQL query to execute</param>
        /// <returns>The query result object</returns>
        SparqlResult ExecuteSparql(SparqlQueryContext sparqlQueryContext);

        /// <summary>
        /// Commits all changes. Waits for the operation to complete.
        /// </summary>
        void SaveChanges();

        /// <summary>
        /// Removes the specified data object from the tracking of the store so that it will not be part of the next SaveChanges
        /// </summary>
        /// <param name="dataObject"></param>
        void DetachDataObject(IDataObject dataObject);

        /// <summary>
        /// Updates a single object in the object context with data from the data source
        /// </summary>
        /// <param name="mode">A <see cref="RefreshMode"/> value that indicates whether property changes
        /// in the object context are overwritten with property changes from the data source</param>
        /// <param name="dataObject">The object to be refreshed</param>
        void Refresh(RefreshMode mode, IDataObject dataObject);

        /// <summary>
        /// Returns an enumeration over all data objects currently tracked by the store
        /// </summary>
        IEnumerable<IDataObject> TrackedObjects { get; }

        /// <summary>
        /// Returns a boolean flag that indicates if this store is read-only.
        /// </summary>
        /// <remarks>A read-only store will not support updates via the <see cref="SaveChanges"/> method.</remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// Returns the list of graphs to query
        /// </summary>
        /// <returns></returns>
        IList<string> GetDataset();

        /// <summary>
        /// Returns a Dataset clause for use in a SPARQL query
        /// </summary>
        /// <returns></returns>
        /// <remarks>The string returned by this method follows the DatasetClause production of the SPARQL 1.1 grammar</remarks>
        String GetDatasetClause();

        /// <summary>
        /// Add a precondition statement to the current context. All precondition
        /// statements are evaluated prior to any update being applied.
        /// </summary>
        /// <param name="matchExisting">If true, then the triple pattern must match an existing triple in the store. If false then the triple
        /// pattern must not match an existing triple in the store.</param>
        /// <param name="subject">The subject of the triple pattern</param>
        /// <param name="predicate">The predicate of the triple pattern</param>
        /// <param name="object">The object of the triple pattern</param>
        /// <param name="graph">The graph identifier of the triple pattern</param>
        /// <param name="isLiteral">True if the object is a literal value, false if it is a resource URI</param>
        /// <param name="datatype">The datatype of the literal value</param>
        /// <param name="language">The language code of the literal value</param>
        void AddPrecondition(bool matchExisting, string subject, string predicate, string @object, string graph = Constants.WildcardUri, bool isLiteral = false, string datatype = null, string language = null);
    }
}
