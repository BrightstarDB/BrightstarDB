using System.Collections.Generic;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// The interface for a collection of Brightstar <see cref="IDataObjectStore"/>.
    /// </summary>
    public interface IDataObjectContext
    {
        /// <summary>
        /// Opens a new <see cref="IDataObjectStore"/> with the specified name.
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="optimisticLockingEnabled">If set to true will ensure that updates to objects modified since being read will fail.</param>
        /// <param name="namespaceMappings">A map of simple string prefixes to URIs prefixes e.g. rdfs : http://www.w3c.org/rdfs</param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="defaultDataSet">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve data objects and their properties.
        /// See remarks below.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <exception cref="BrightstarClientException">Thrown if the store does not exist or cannot be accessed.</exception> 
        /// <returns>IDataObjectStore</returns>
        /// <remarks>
        /// <para>The default data set queried by context can be explicitly set by providing a value for the <paramref name="defaultDataSet"/> parameter.
        /// If an explicit data set is provided then that set of graphs, plus the update and version tracking graph will be queried by the context.
        /// If an explicit data set is not provided, but update and/or version tracking graphs are specified, then the context will query only the update and version tracking graphs.
        /// If an explicit data set is not provided and update and version tracking graphs are not specified either, then the context will query across all of the graphs in the store (and updates and version tracking
        /// information will be written to the default graph).</para>
        /// </remarks>
        IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null,
            string updateGraph = null, IEnumerable<string> defaultDataSet = null, string versionTrackingGraph = null);

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        bool DoesStoreExist(string storeName);

        /// <summary>
        /// Creates a new store.
        /// </summary>
        /// <param name="storeName">The name of the store to create</param>
        /// <param name="namespaceMappings">A collection of prefix to URI mappings to enable CURIEs to be used for resource addresses instead of full URIs</param>
        /// <param name="optimisticLockingEnabled">A boolean flag indicating if optimistic locking should be enabled</param>
        /// <param name="persistenceType">The type of persistence to use in the newly created store. If not specified, defaults to the value specified in the application configuration file or <see cref="PersistenceType.AppendOnly"/></param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <returns>The newly created data object store</returns>
        /// <remarks>When creating a new store through this API the default data set used for queries will be automatically set to the single update graph specified by the 
        /// <paramref name="updateGraph"/> parameter (or the default graph if the parameter is ommitted).</remarks>
        IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null, 
            PersistenceType? persistenceType = null,
            string updateGraph = null, string versionTrackingGraph = null);

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        void DeleteStore(string storeName);

        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        bool OptimisticLockingEnabled { get; }
    }
}
