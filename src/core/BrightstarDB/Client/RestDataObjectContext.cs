using System;
using System.Collections.Generic;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    ///<summary>
    /// DataObjectContext that connects to BrightstarDB over a REST HTTP connection
    ///</summary>
    public class RestDataObjectContext:IDataObjectContext
    {
        private ConnectionString _connectionString;
        private readonly string _endpointUri;
        private readonly bool _optimisticLockingEnabled;

        /// <summary>
        /// Creates a new context that connects to the specific Brightstar service endpoint.
        /// </summary>
        /// <param name="connectionString">The Brightstar service connection string</param>
        public RestDataObjectContext(ConnectionString connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (connectionString.Type != ConnectionType.Rest)
            {
                throw new ArgumentException("Invalid connection type", "connectionString");
            }
            _connectionString = connectionString;
            _endpointUri = connectionString.ServiceEndpoint;
            _optimisticLockingEnabled = connectionString.OptimisticLocking;
        }

        /// <summary>
        /// Utility wrapper to return the REST client
        /// </summary>
        protected IBrightstarService Client
        {
            get { return BrightstarService.GetRestClient(_connectionString); }
        }

        #region Implementation of IDataObjectContext

        /// <summary>
        /// Opens a new <see cref="IDataObjectStore"/> with the specified name.
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="optimisticLockingEnabled">If set to true will ensure that updates to objects modified since being read will fail.</param>
        /// <param name="namespaceMappings">A map of simple string prefixes to URIs prefixes e.g. rdfs : http://www.w3c.org/rdfs</param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="defaultDataSet">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve data objects and their properties.
        /// If not defined, all graphs in the store will be queried.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <exception cref="BrightstarClientException">Thrown if the store does not exist or cannot be accessed.</exception> 
        /// <returns>IDataObjectStore</returns>
        public IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings, bool? optimisticLockingEnabled,
            string updateGraph = null, IEnumerable<string> defaultDataSet = null, string versionTrackingGraph = null)
        {
            if (!DoesStoreExist(storeName)) throw new BrightstarClientException("Store does not exist");
            if (namespaceMappings == null) namespaceMappings = new Dictionary<string, string>();

            namespaceMappings["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            namespaceMappings["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";

            if (String.IsNullOrEmpty(updateGraph)) updateGraph = Constants.DefaultGraphUri;

            return new RestDataObjectStore(_connectionString, storeName, namespaceMappings,
                optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled,
                updateGraph, defaultDataSet, versionTrackingGraph);
        }

        /// <summary>
        /// Checks for the existence of a store with the given name
        /// </summary>
        /// <param name="storeName">Name of the store to check for</param>
        /// <returns>True if the store exists, false otherwise</returns>
        public bool DoesStoreExist(string storeName)
        {
            return Client.DoesStoreExist(storeName);
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
        public IDataObjectStore CreateStore(string storeName, Dictionary<string, string> namespaceMappings, 
            bool? optimisticLockingEnabled = null, 
            PersistenceType? persistenceType = null,
            string updateGraph = null, string versionTrackingGraph = null)
        {
            Client.CreateStore(storeName,
                               persistenceType.HasValue ? persistenceType.Value : Configuration.PersistenceType);
            if (String.IsNullOrEmpty(updateGraph)) updateGraph = Constants.DefaultGraphUri;
            if (String.IsNullOrEmpty(versionTrackingGraph)) versionTrackingGraph = updateGraph;
            return new RestDataObjectStore(
                _connectionString, storeName, namespaceMappings,
                optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled,
                updateGraph, new string[] {updateGraph}, versionTrackingGraph);
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The name of the store to be deleted</param>
        public void DeleteStore(string storeName)
        {
            Client.DeleteStore(storeName);
        }

        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        public bool OptimisticLockingEnabled
        {
            get { return _optimisticLockingEnabled; }
        }

        #endregion
    }
}
