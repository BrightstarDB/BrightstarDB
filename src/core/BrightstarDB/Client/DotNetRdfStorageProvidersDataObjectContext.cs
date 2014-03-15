using System;
using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// A <see cref="IDataObjectContext"/> that uses one or more underlying <see cref="IStorageProvider"/>
    /// instances to manage RDF data.
    /// </summary>
    /// <remarks>The collection of stores managed by this class are fixed when the class is initialized.
    /// The <see cref="CreateStore"/> and <see cref="DeleteStore"/> methods will raise a 
    /// <see cref="NotSupportedException"/> if invoked.</remarks>
    public class DotNetRdfStorageProvidersDataObjectContext : DotNetRdfDataObjectContextBase
    {
        private readonly IGraph _configurationGraph;
        private readonly Dictionary<string, IStorageProvider> _storageProviders; 

        /// <summary>
        /// Creates a new data object context that provides access to one or more
        /// instances of the IStorageProvider interface configured in a DotNetRDF
        /// configuration graph
        /// </summary>
        /// <param name="configurationGraph">The RDF graph providing the storage provider configuration(s)</param>
        /// <param name="optimisticLockingEnabled">Boolean flag indicating whether optimistic locking should be enabled by default for the stores provided by this context</param>
        public DotNetRdfStorageProvidersDataObjectContext(IGraph configurationGraph, bool optimisticLockingEnabled = false)
        {
            _configurationGraph = configurationGraph;
            _storageProviders = new Dictionary<string, IStorageProvider>();
            OptimisticLockingEnabled = optimisticLockingEnabled;
        }

        /// <summary>
        /// Creates a new data object context that provides access to a single
        /// DotNetRDF storage provider
        /// </summary>
        /// <param name="storeName">The name to use for the BrightstarDB store created from the storage provider</param>
        /// <param name="storageProvider">The storage provider to use to create the BrightstarDB store</param>
        /// <param name="optimisticLockingEnabled">Boolean flag indicating whether optimistic locking should be enabled by default for the stores provided by this context</param>
        public DotNetRdfStorageProvidersDataObjectContext(string storeName, IStorageProvider storageProvider, bool optimisticLockingEnabled = false)
        {
            _configurationGraph = null;
            _storageProviders = new Dictionary<string, IStorageProvider> {{storeName, storageProvider}};
            OptimisticLockingEnabled = optimisticLockingEnabled;
        }

        /// <summary>
        /// Creates a new data object context that provides access to multiple DotNetRDF
        /// storage providers
        /// </summary>
        /// <param name="storageProviders">A dictionary mapping the name to use for the BrightstarDB store to the storage
        /// provider that the BrightstarDB store will access</param>
        /// <param name="optimisticLockingEnabled">Boolean flag indicating whether optimistic locking should be enabled by default for the stores provided by this context</param>
        public DotNetRdfStorageProvidersDataObjectContext(IDictionary<string, IStorageProvider> storageProviders, bool optimisticLockingEnabled = false)
        {
            _configurationGraph = null;
            _storageProviders = new Dictionary<string, IStorageProvider>(storageProviders);
            OptimisticLockingEnabled = optimisticLockingEnabled;
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
        public override IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null,
                                          string updateGraph = null, IEnumerable<string> defaultDataSet = null,
                                          string versionTrackingGraph = null)
        {
            var storageProvider = GetStorageProvider(storeName);
            if (storageProvider == null)
            {
                throw new BrightstarClientException(String.Format(Strings.BrightstarServiceClient_StoreDoesNotExist, storeName));
            }
            return CreateDataObjectStore(namespaceMappings, optimisticLockingEnabled, updateGraph, defaultDataSet, versionTrackingGraph, storageProvider);
        }


        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        public override bool DoesStoreExist(string storeName)
        {
            try
            {
                if (_storageProviders.ContainsKey(storeName)) return true;
                return GetStorageProvider(storeName) != null;
            }
            catch (BrightstarClientException)
            {
                return false;
            }
        }

        private IStorageProvider GetStorageProvider(string storeName)
        {
            if (_storageProviders.ContainsKey(storeName)) return _storageProviders[storeName];
            if (_configurationGraph != null)
            {
                var obj = DotNetRdfConfigurationHelper.GetConfigurationObject(_configurationGraph, storeName);
                if (obj == null)
                {
                    throw new BrightstarClientException(String.Format(Strings.BrightstarServiceClient_StoreDoesNotExist, storeName));
                }
                if (!(obj is IStorageProvider))
                {
                    throw new BrightstarClientException(Strings.DotNetRdf_NotAStorageProvider);
                }
                _storageProviders[storeName] = obj as IStorageProvider;
                return obj as IStorageProvider;
            }
            return null;
        }

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
        public override IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                            bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null,
                                            string updateGraph = null, string versionTrackingGraph = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Deletes all data from the specified store
        /// </summary>
        /// <param name="storeName"></param>
        /// <remarks>This implementation of DeleteStore differs slightly from the basic BrightstarDB implementation. Rather than
        /// removing the store completely, this method simply clears all RDF data in the store. It can only be executed against
        /// DotNetRDF storage providers that support listing of graphs and graph deletion. For stores that either do not support
        /// these operaitons or which are marked as readonly, this method will raise a <see cref="NotSupportedException"/></remarks>
        public override void DeleteStore(string storeName)
        {
            IStorageProvider storageProvider = GetStorageProvider(storeName);
            if (storageProvider == null) throw new BrightstarClientException(Strings.BrightstarServiceClient_StoreDoesNotExist);
            if (storageProvider.IsReadOnly ||
                !storageProvider.ListGraphsSupported ||
                !storageProvider.DeleteSupported)
            {
                throw new NotSupportedException(Strings.DotNetRdf_StoreDoesNotSupportDelete);
            }
            foreach (var g in storageProvider.ListGraphs())
            {
                storageProvider.DeleteGraph(g);
            }
        }

    }
}