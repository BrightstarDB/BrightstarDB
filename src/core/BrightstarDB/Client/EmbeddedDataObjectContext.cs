#if !REST_CLIENT
using System;
using System.Collections.Generic;
using BrightstarDB.Server;
using BrightstarDB.Storage;

namespace BrightstarDB.Client
{
    /// <summary>
    /// Provides a data object abstraction over an embedded Brightstar store.
    /// </summary>
    public class EmbeddedDataObjectContext : IDataObjectContext
    {
        private readonly ServerCore _serverCore;
        private readonly bool _optimisticLockingEnabled;


        /// <summary>
        /// Get the flag that indicates if optimistic locking is enabled for
        /// stores opened or created by this context by default
        /// </summary>
        public bool OptimisticLockingEnabled { get { return _optimisticLockingEnabled; } }

        /// <summary>
        /// Create a new instance of the context that attaches to the specified directory location
        /// </summary>
        /// <param name="connectionString">The Brightstar service connection string</param>
        /// <remarks>The data context is thread-safe but doesn't support concurrent access to the same base location by multiple
        /// instances. You should ensure in your code that only one EmbeddedDataObjectContext instance is connected to any given base location
        /// at a given time.</remarks>
        public EmbeddedDataObjectContext(ConnectionString connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException("connectionString");
            if (connectionString.Type != ConnectionType.Embedded) throw new ArgumentException("Invalid connection type", "connectionString");
            _serverCore = ServerCoreManager.GetServerCore(connectionString.StoresDirectory);
            _optimisticLockingEnabled = connectionString.OptimisticLocking;
        }

        #region Implemenation of IDataObjectContext

        /// <summary>
        /// Opens a new <see cref="IDataObjectStore"/> with the specified name.
        /// </summary>
        /// <param name="storeName">The name of the store</param>
        /// <param name="optimisticLockingEnabled">Optional parameter to override the default optimistic locking setting for the context</param>
        /// <param name="namespaceMappings">A map of simple string prefixes to URIs prefixes e.g. rdfs : http://www.w3c.org/rdfs</param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="defaultDataSet">OPTIONAL: The URI identifiers of the graphs that will be queried to retrieve data objects and their properties.
        /// If not defined, all graphs in the store will be queried.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <exception cref="BrightstarClientException">Thrown if the store does not exist or cannot be accessed.</exception> 
        /// <returns>IDataObjectStore</returns>
        public IDataObjectStore OpenStore(string storeName, Dictionary<string, string> namespaceMappings = null, bool? optimisticLockingEnabled = null,
            string updateGraph = null, IEnumerable<string> defaultDataSet = null, string versionTrackingGraph = null)
        {
            if(!DoesStoreExist(storeName)) throw new BrightstarClientException("Store does not exist");

            if (namespaceMappings == null)
            {
                namespaceMappings = new Dictionary<string, string>();
            }

            namespaceMappings["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            namespaceMappings["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";

            return new EmbeddedDataObjectStore(_serverCore, storeName, namespaceMappings,
                                               optimisticLockingEnabled.HasValue
                                                   ? optimisticLockingEnabled.Value
                                                   : _optimisticLockingEnabled,
                                                   updateGraph, defaultDataSet, versionTrackingGraph);
        }

        /// <summary>
        /// Checks for the existence of a store with the given name.
        /// </summary>
        /// <param name="storeName">Name of the store </param>
        /// <returns>True if the store exists, False if not.</returns>
        public bool DoesStoreExist(string storeName)
        {
            return _serverCore.DoesStoreExist(storeName);
        }

        /// <summary>
        /// Creates a new store.
        /// </summary>
        /// <param name="storeName">Name of the store to create.</param>
        /// <param name="optimisticLockingEnabled">Optional parameter to override the default optimistic locking setting for the context</param>
        /// <param name="namespaceMappings">Namespace mappings that can be used by CURIE's.</param>
        /// <param name="persistenceType">The type of persistence to use in the newly created store. If not specified, defaults to the value specified in the application configuration file or <see cref="PersistenceType.AppendOnly"/></param>
        /// <param name="updateGraph">OPTIONAL: The URI identifier of the graph to be updated with any new triples created by operations on the store. If
        /// not defined, the default graph in the store will be updated.</param>
        /// <param name="versionTrackingGraph">OPTIONAL: The URI identifier of the graph that contains version number statements for data objects. 
        /// If not defined, the <paramref name="updateGraph"/> will be used.</param>
        /// <returns>A new store instance.</returns>
        public IDataObjectStore CreateStore(string storeName, 
            Dictionary<string, string> namespaceMappings = null, 
            bool? optimisticLockingEnabled = null, 
            PersistenceType? persistenceType = null,
            string updateGraph = null, 
            string versionTrackingGraph = null)
        {
            if (namespaceMappings == null)
            {
                namespaceMappings = new Dictionary<string, string>();
            }

            namespaceMappings["rdf"] = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
            namespaceMappings["rdfs"] = "http://www.w3.org/2000/01/rdf-schema#";

            _serverCore.CreateStore(storeName, persistenceType.HasValue ? persistenceType.Value : Configuration.PersistenceType);

            if (String.IsNullOrEmpty(updateGraph)) updateGraph = Constants.DefaultGraphUri;

            return new EmbeddedDataObjectStore(_serverCore, storeName, namespaceMappings, 
                optimisticLockingEnabled.HasValue ? optimisticLockingEnabled.Value : _optimisticLockingEnabled,
                updateGraph, new []{updateGraph}, versionTrackingGraph);
        }

        /// <summary>
        /// Delete the named store
        /// </summary>
        /// <param name="storeName">The store to delete.</param>
        public void DeleteStore(string storeName)
        {
            _serverCore.DeleteStore(storeName);
        }

        #endregion

    }
}
#endif