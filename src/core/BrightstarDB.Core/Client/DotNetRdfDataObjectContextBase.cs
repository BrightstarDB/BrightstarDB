using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Update;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Base class for implementations of <see cref="IDataObjectContext"/> that act on DotNetRDF
    /// IStorageProvider instances
    /// </summary>
    public abstract class DotNetRdfDataObjectContextBase : IDataObjectContext
    {
        /// <summary>
        /// Return a new <see cref="IDataObjectStore"/> instance that wraps the specified DotNetRDF IStorageProvider instance
        /// </summary>
        /// <param name="namespaceMappings">Namespace mappings to apply to the data object store</param>
        /// <param name="optimisticLockingEnabled">Boolean flag indicating if optimistic locking is to be enabled on the store</param>
        /// <param name="updateGraph">The URI of the graph to be modified by update operations</param>
        /// <param name="defaultDataSet">The URI of the graph(s) to be accessed by read and query operations</param>
        /// <param name="versionTrackingGraph">The URI of the graph to use to track entity version numbers for optimistic locking</param>
        /// <param name="storageProvider">The DotNetRDF IStorageProvider instance that manages the RDF graphs</param>
        /// <returns>A <see cref="IDataObjectStore"/> instance that wraps access to the <paramref name="storageProvider"/></returns>
        protected IDataObjectStore CreateDataObjectStore(Dictionary<string, string> namespaceMappings, bool? optimisticLockingEnabled,
                                                         string updateGraph, IEnumerable<string> defaultDataSet,
                                                         string versionTrackingGraph, IStorageProvider storageProvider)
        {
            if (!(storageProvider is IQueryableStorage))
            {
                throw new BrightstarClientException(Strings.DotNetRdf_StoreMustImplementIQueryableStorage);
            }
            var queryProcessor = new GenericQueryProcessor(storageProvider as IQueryableStorage);
            ISparqlUpdateProcessor updateProcessor = null;
            if (storageProvider is IUpdateableStorage && !storageProvider.IsReadOnly)
            {
                updateProcessor = new GenericUpdateProcessor(storageProvider);
            }

            return new SparqlDataObjectStore(queryProcessor, updateProcessor, namespaceMappings,
                                             optimisticLockingEnabled.HasValue
                                                 ? optimisticLockingEnabled.Value
                                                 : OptimisticLockingEnabled,
                                             updateGraph, defaultDataSet, versionTrackingGraph);
        }

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
        public abstract IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null,
                                                   string updateGraph = null, IEnumerable<string> defaultDataSet = null,
                                                   string versionTrackingGraph = null);

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        public abstract bool DoesStoreExist(string storeName);

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
        public abstract IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                                     bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null,
                                                     string updateGraph = null, string versionTrackingGraph = null);

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public abstract void DeleteStore(string storeName);

        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        public bool OptimisticLockingEnabled { get; protected set; }

    }
}