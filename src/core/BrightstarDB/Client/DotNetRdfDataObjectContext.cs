using System;
using System.Collections.Generic;
using BrightstarDB.Storage;
using VDS.RDF;
using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Storage;
using VDS.RDF.Update;
#if PORTABLE
using BrightstarDB.Portable.Adaptation;
#else

#endif

namespace BrightstarDB.Client
{
    /// <summary>
    /// An implementation of <see cref="IDataObjectContext"/> that uses a
    /// DotNetRDF ISparqlQueryProcessor for query and an ISparqlUpdateProcessor
    /// for update.
    /// </summary>
    public class DotNetRdfDataObjectContext : IDataObjectContext
    {
        private readonly string _configuredStoreName;
        readonly ISparqlUpdateProcessor _updateProcessor;
        readonly ISparqlQueryProcessor _queryProcessor;

        /// <summary>
        /// Create a new instance of <see cref="DotNetRdfDataObjectContext"/>
        /// with a specific ISparqlQueryProcessor and ISparqlUpdateProcessor instance
        /// that serves up a single named store.
        /// </summary>
        /// <param name="storeName">The name of the store that will be provided by this context</param>
        /// <param name="queryProcessor">The ISparqlQueryProcessor instance that provides SPARQL query functionality for the store.</param>
        /// <param name="updateProcessor">The ISparqlUpdateProcessor instance that provides SPARQL update functionality for the store.</param>
        /// <exception cref="ArgumentException">Raised if <paramref name="storeName"/> is NULL or an empty string</exception>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="queryProcessor"/> or <paramref name="updateProcessor"/> is NULL</exception>
        public DotNetRdfDataObjectContext(string storeName, ISparqlQueryProcessor queryProcessor, ISparqlUpdateProcessor updateProcessor)
        {
            if (String.IsNullOrEmpty(storeName)) throw new ArgumentException(Strings.StringParameterMustBeNonEmpty, "storeName");
            if (queryProcessor == null) throw new ArgumentNullException("queryProcessor");
            if (updateProcessor == null) throw new ArgumentNullException("updateProcessor");
            _configuredStoreName = storeName;
            _queryProcessor = queryProcessor;
            _updateProcessor = updateProcessor;
        }

        /// <summary>
        /// Create a new instance of <see cref="DotNetRdfDataObjectContext"/>
        /// from a BrightstarDB connection string.
        /// </summary>
        /// <param name="connectionString">The connection string to be parsed.</param>
        /// <exception cref="BrightstarClientException">Raised if a store could not be configured from the information provided in the connection string.</exception>
        /// <exception cref="ArgumentNullException">Raised if <paramref name="connectionString"/> is NULL</exception>
        public DotNetRdfDataObjectContext(ConnectionString connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            IGraph configuration;
            try
            {
                configuration = LoadConfiguration(connectionString.Configuration);
            }
            catch (Exception ex)
            {
                throw new BrightstarClientException(
                    String.Format("Error loading DotNetRDF configuration from {0}.", connectionString.Configuration), ex);
            }
            _configuredStoreName = connectionString.StoreName;
            if (!String.IsNullOrEmpty(connectionString.DnrStore))
            {
                var configObject = GetConfigurationObject(configuration, connectionString.DnrStore);
                if (configObject is IUpdateableTripleStore)
                {
                    _updateProcessor = new SimpleUpdateProcessor(configObject as IUpdateableTripleStore);
                }
                else if (configObject is IInMemoryQueryableStore)
                {
                    _updateProcessor = new LeviathanUpdateProcessor(configObject as IInMemoryQueryableStore);
                }
                else if (configObject is IStorageProvider)
                {
                    _updateProcessor = new GenericUpdateProcessor(configObject as IStorageProvider);
                }
                else
                {
                    throw new BrightstarClientException(
                        "Could not create a SPARQL Update processor for the configured store.");
                }

                if (configObject is INativelyQueryableStore)
                {
                    _queryProcessor = new SimpleQueryProcessor(configObject as INativelyQueryableStore);
                }
                else if (configObject is IInMemoryQueryableStore)
                {
                    _queryProcessor = new LeviathanQueryProcessor(configObject as IInMemoryQueryableStore);
                }
                else if (configObject is IQueryableStorage)
                {
                    _queryProcessor = new GenericQueryProcessor(configObject as IQueryableStorage);
                }
                else
                {
                    throw new BrightstarClientException(
                        "Could not create a SPARQL Query processor for the configured store.");
                }
            }
            else
            {
                if (String.IsNullOrEmpty(connectionString.DnrQuery) ||
                    String.IsNullOrEmpty(connectionString.DnrUpdate))
                {
                    throw new BrightstarClientException("DotNetRDF connection requires either a Store property or a Query and an Update property.");
                }

                var queryObject = GetConfigurationObject(configuration, connectionString.DnrQuery);
                if (queryObject == null)
                {
                    throw new BrightstarClientException("The configured Query property of the connection string could not be resolved.");
                }
                if (queryObject is ISparqlQueryProcessor)
                {
                    _queryProcessor = queryObject as ISparqlQueryProcessor;
                }
                else if (queryObject is SparqlRemoteEndpoint)
                {
                    _queryProcessor = new RemoteQueryProcessor(queryObject as SparqlRemoteEndpoint);
                }
                else
                {
                    throw new BrightstarClientException(
                        String.Format("Could not create a SPARQL Query processor from the configured Query property. Expected instance of {0} or {1}. Got an instance of {2}",
                        typeof(ISparqlQueryProcessor).FullName, typeof(SparqlRemoteEndpoint).FullName, queryObject.GetType().FullName));
                }

                var updateObject = GetConfigurationObject(configuration, connectionString.DnrUpdate);
                if (updateObject == null)
                {
                    throw new BrightstarClientException("The configured Update property of the connection string could not be resolved.");
                }
                if (updateObject is ISparqlUpdateProcessor)
                {
                    _updateProcessor = queryObject as ISparqlUpdateProcessor;
                }
#if !WINDOWS_PHONE && !PORTABLE
                else if (updateObject is SparqlRemoteUpdateEndpoint)
                {
                    _updateProcessor = new RemoteUpdateProcessor(updateObject as SparqlRemoteUpdateEndpoint);
                }
#endif
                else
                {
                    throw new BrightstarClientException(
                        String.Format("Could not create a SPARQL Update processor from the configured Update property. Expected instance of {0} or {1}. Got an instance of {2}",
                        typeof(ISparqlUpdateProcessor).FullName, typeof(SparqlRemoteUpdateEndpoint).FullName, updateObject.GetType().FullName));
                }
            }
        }


#if PORTABLE
        private static IGraph LoadConfiguration(string configurationPath)
        {
            var pm = PlatformAdapter.Resolve<IPersistenceManager>();
            ConfigurationLoader.PathResolver = new DotNetRdfConfigurationPathResolver(configurationPath);
            using (var stream = pm.GetInputStream(configurationPath))
            {
                return ConfigurationLoader.LoadConfiguration(configurationPath, new Uri(configurationPath), stream);
            }
        }
#else
        private static IGraph LoadConfiguration(string configurationPath)
        {
            ConfigurationLoader.PathResolver = new DotNetRdfConfigurationPathResolver(configurationPath);
            return ConfigurationLoader.LoadConfiguration(configurationPath);
        }
#endif

        private object GetConfigurationObject(IGraph configurationGraph, string id)
        {
            var configNode = configurationGraph.CreateUriNode(new Uri(id));
            return ConfigurationLoader.LoadObject(configurationGraph, configNode);
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
        public IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null,
                                          string updateGraph = null, IEnumerable<string> defaultDataSet = null,
                                          string versionTrackingGraph = null)
        {
            if (storeName != _configuredStoreName)
            {
                throw new BrightstarClientException("Store not found");
            }
            return new SparqlDataObjectStore(_queryProcessor, _updateProcessor, namespaceMappings,
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
            return storeName == _configuredStoreName;
        }

        /// <summary>
        /// Not supported by this class. Do not use.
        /// </summary>
        /// <param name="storeName"></param>
        /// <param name="namespaceMappings"></param>
        /// <param name="optimisticLockingEnabled"></param>
        /// <param name="persistenceType"></param>
        /// <param name="updateGraph"></param>
        /// <param name="versionTrackingGraph"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Always raised</exception>
        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                            bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null,
                                            string updateGraph = null, string versionTrackingGraph = null)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported by this class. Do not use.
        /// </summary>
        /// <param name="storeName"></param>
        /// <exception cref="NotSupportedException">Always raised</exception>
        public void DeleteStore(string storeName)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// A boolean flag indicating if the context supports optimistic locking.
        /// </summary>
        /// <remarks>Currently this type of DataObjectContext does not support optimistic locking, so this property is always false.</remarks>
        public bool OptimisticLockingEnabled { get { return false; } } // Currently not supported
    }
}