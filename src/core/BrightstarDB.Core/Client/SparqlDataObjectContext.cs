using System;
using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF.Query;
using VDS.RDF.Update;

namespace BrightstarDB.Client
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks><p>This implementation manages access to a single query/update endpoint pair. There is one store, with a default
    /// name of "sparql" (can be overridden in the constructor). The store can be opened multiple times in parallel if desired, 
    /// but all of the store instances share the same underlying connections to the SPARQL endpoints.</p>
    /// <p>This implementation does not support the <see cref="DeleteStore"/> and <see cref="CreateStore"/> operations. These
    /// methods will throw a <see cref="NotSupportedException"/> if they are invoked.</p>
    /// </remarks>
    public class SparqlDataObjectContext : IDataObjectContext
    {
        private const string DefaultStoreName = "sparql";
        private readonly string _storeName;

        /// <summary>
        /// Creates a new <see cref="IDataObjectContext"/> instance that uses SPARQL query and update
        /// to access RDF data.
        /// </summary>
        /// <param name="queryProcessor">The SPARQL query processor to use for read/query </param>
        /// <param name="updateProcessor">The SPARQL update processor to use for update</param>
        /// <param name="optimisticLocking">Boolean flag indicating if optimistic locking should be enabled by default for the stores
        /// accessed via this context.</param>
        /// <param name="storeName">Overrides the default store name of 'sparql' for the store managed by this context</param>
        public SparqlDataObjectContext(ISparqlQueryProcessor queryProcessor, ISparqlUpdateProcessor updateProcessor, bool optimisticLocking, string storeName = DefaultStoreName)
        {
            QueryProcessor = queryProcessor;
            UpdateProcessor = updateProcessor;
            OptimisticLockingEnabled = optimisticLocking;
            _storeName = storeName;
        }

        /// <summary>
        /// Get the DotNetRDF <see cref="ISparqlQueryProcessor"/> instance used for SPARQL query operations
        /// </summary>
        public ISparqlQueryProcessor QueryProcessor { get; private set; }

        /// <summary>
        /// Get the DotNetRDF <see cref="ISparqlUpdateProcessor"/> instance used for SPARQL update operations
        /// </summary>
        public ISparqlUpdateProcessor UpdateProcessor { get; private set; }


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
        public IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null,
                                          string updateGraph = null, IEnumerable<string> defaultDataSet = null,
                                          string versionTrackingGraph = null)
        {
            if (!storeName.Equals(_storeName)) throw new BrightstarClientException(Strings.BrightstarServiceClient_StoreDoesNotExist);
            return new SparqlDataObjectStore(QueryProcessor, UpdateProcessor, namespaceMappings,
                                             optimisticLockingEnabled.HasValue
                                                 ? optimisticLockingEnabled.Value
                                                 : OptimisticLockingEnabled,
                                             updateGraph, defaultDataSet, versionTrackingGraph);
        }

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        public bool DoesStoreExist(string storeName)
        {
            return storeName.Equals(_storeName);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="namespaceMappings"></param>
        /// <param name="optimisticLockingEnabled"></param>
        /// <param name="persistenceType"></param>
        /// <param name="updateGraph"></param>
        /// <param name="versionTrackingGraph"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">raised if this method is invoked</exception>
        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                            bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null,
                                            string updateGraph = null, string versionTrackingGraph = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="storeName"></param>
        /// <exception cref="NotSupportedException">raised if this method is invoked</exception>
        public void DeleteStore(string storeName)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        public bool OptimisticLockingEnabled { get; private set; }
    }
}