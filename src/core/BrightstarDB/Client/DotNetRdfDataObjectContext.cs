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
using System.IO;
#endif

namespace BrightstarDB.Client
{
    internal class DotNetRdfDataObjectContext : IDataObjectContext
    {
        private readonly string _configuredStoreName;
        private readonly IGraph _configuration;
        readonly ISparqlUpdateProcessor _updateProcessor;
        readonly ISparqlQueryProcessor _queryProcessor;


        public DotNetRdfDataObjectContext(ConnectionString connectionString)
        {
            try
            {
                _configuration = LoadConfiguration(connectionString.Configuration);
            }
            catch (Exception ex)
            {
                throw new BrightstarClientException(
                    String.Format("Error loading DotNetRDF configuration from {0}.", connectionString.Configuration), ex);
            }
            _configuredStoreName = connectionString.StoreName;
            if (!String.IsNullOrEmpty(connectionString.DnrStore))
            {
                var configObject = GetConfigurationObject(connectionString.DnrStore);
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

                var queryObject = GetConfigurationObject(connectionString.DnrQuery);
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

                var updateObject = GetConfigurationObject(connectionString.DnrUpdate);
                if (updateObject == null)
                {
                    throw new BrightstarClientException("The configured Update property of the connection string could not be resolved.");
                }
                if (updateObject is ISparqlUpdateProcessor)
                {
                    _updateProcessor = queryObject as ISparqlUpdateProcessor;
                }
                else if (updateObject is SparqlRemoteUpdateEndpoint)
                {
                    _updateProcessor = new RemoteUpdateProcessor(updateObject as SparqlRemoteUpdateEndpoint);
                }
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

        private object GetConfigurationObject(string id)
        {
            var configNode = _configuration.CreateUriNode(new Uri(id));
            return ConfigurationLoader.LoadObject(_configuration, configNode);
        }

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

        public bool DoesStoreExist(string storeName)
        {
            return storeName == _configuredStoreName;
        }

        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings = null,
                                            bool? optimisticLockingEnabled = null, PersistenceType? persistenceType = null,
                                            string updateGraph = null, string versionTrackingGraph = null)
        {
            throw new NotSupportedException();
        }

        public void DeleteStore(string storeName)
        {
            throw new NotSupportedException();
        }

        public bool OptimisticLockingEnabled { get { return false; } } // Currently not supported
    }

    internal class DotNetRdfConfigurationPathResolver : IPathResolver
    {
        private readonly string _configurationPath;

        public DotNetRdfConfigurationPathResolver(string configurationPath)
        {
#if PORTABLE
            _configurationPath = Path.GetDirectoryName(configurationPath);
#else
            _configurationPath = Path.GetDirectoryName(Path.GetFullPath(configurationPath));
#endif
        }
        public string ResolvePath(string path)
        {
            return Path.Combine(_configurationPath, path);
        }
    }
}